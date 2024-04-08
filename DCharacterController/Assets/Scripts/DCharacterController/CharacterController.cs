using System;
using System.Collections;
using System.Collections.Generic;
using Disc0ver.FSM;
using UnityEditor.UI;
using UnityEngine;

namespace Disc0ver
{
    [RequireComponent(typeof(MovementComponent))]
    [RequireComponent(typeof(DccAnimComponent))]
    public class CharacterController : MonoBehaviour
    {
        public Vector3 inputVec;
        public Vector3 acceleration;
        public GameObject modelGo;

        [SerializeField, Range(1f, 10f)]
        private float maxVelocity = 1f;
        [SerializeField, Range(1f, 10f)]
        private float maxAcceleration = 7f;
        [SerializeField, Range(1f, 20f)] 
        private float maxJumpSpeed = 15f;
        [SerializeField, Range(0.3f, 0.5f)] 
        public float maxJumpDownTime = 0.3f;

        [SerializeField, Range(10f, 180f)] private float angleVelocity = 30f;

        private float brakingFrictionFactor = 2f;

        private bool _jump;
        private bool _notifyApex = false;
        private bool _applyGravityWhileJump = true;

        public bool Jump => _jump;
        public bool ApplyGravityWhileJump => _applyGravityWhileJump;
        public float jumpForceTimeRemain = 0f;
        public float terminalFallingVelocity = 10f;
        public bool NotifyApex => _notifyApex;

        public MovementComponent MovementComponent => _movementComponent;
        private MovementComponent _movementComponent;
        public StateMachine stateMachine;
        public DccAnimClips dccAnimClips;

        private Vector3 _lastRootPosition;

        private void Awake()
        {
            // Application.targetFrameRate = 60;
            _movementComponent = GetComponent<MovementComponent>();
            _movementComponent.controller = this;
            stateMachine = new StateMachine(this);
        }

        private void Start()
        {
            CameraController.Instance.FollowCharacter(this);
        }

        private void HandleInput(float deltaTime)
        {
            inputVec.x = Input.GetAxis("Horizontal");
            inputVec.z = Input.GetAxis("Vertical");

            var cameraForward = CameraController.Instance.Forward();
            // var flag = Vector3.Dot(inputVec, cameraForward) < 0 ? -1 : 1;
            inputVec = new Vector3(inputVec.z * cameraForward.x + inputVec.x * cameraForward.z, 0,
                -inputVec.x * cameraForward.x + inputVec.z * cameraForward.z);

        }

        public void DoJump(float percent)
        {
            _notifyApex = true;
            jumpForceTimeRemain = 1f;
            _movementComponent.DoJump(Mathf.Max(0.5f, percent) * maxJumpSpeed);
            stateMachine.ChangeToState(StateType.Jump);
        }

        public void StartMove()
        {
            _movementComponent.SetMovementMode(MoveMode.MoveWalking);
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.deltaTime;
            _lastRootPosition = transform.position;
            stateMachine.Update(deltaTime);

            HandleInput(deltaTime);
            _movementComponent.Tick(deltaTime);
        }

        private void LateUpdate()
        {
            CameraController.Instance.Tick();
        }

        public void CalcVelocity(ref Vector3 currentVelocity, float deltaTime, float friction, bool fluid, float breakingDeceleration)
        {
            acceleration = Vector3.ClampMagnitude(inputVec, 1) * maxAcceleration;
            if (acceleration.magnitude < 0.001f)
            {
                var oldVelocity = currentVelocity;
                ApplyVelocityBreaking(ref currentVelocity, deltaTime, friction, breakingDeceleration);
            }
            else
            {
                if (stateMachine.animancerComponent.Animator.applyRootMotion)
                {
                    Debug.Log($"rootPosition {stateMachine.animancerComponent.Animator.rootPosition}");
                    // currentVelocity = (stateMachine.animancerComponent.Animator.rootPosition - _lastRootPosition) / deltaTime;
                    currentVelocity = stateMachine.animancerComponent.Animator.velocity;
                    return;
                }
                
                friction = Mathf.Max(0f, friction);
                currentVelocity = currentVelocity -
                                  (currentVelocity - acceleration.normalized * currentVelocity.magnitude) *
                                  Mathf.Min(friction * deltaTime, 1f);

                currentVelocity = currentVelocity + acceleration * deltaTime;
                currentVelocity = Vector3.ClampMagnitude(currentVelocity, maxVelocity);
            }
            
        }
        
        public Quaternion GetDeltaRotation(Quaternion transientRotation, float deltaTime)
        {
            var accForward = inputVec;
            accForward.y = 0;

            if (inputVec.magnitude < 1e-4)
            {
                return transientRotation;
            }

            if (stateMachine.animancerComponent.Animator.applyRootMotion)
            {
                var angle = stateMachine.animancerComponent.Animator.bodyRotation.eulerAngles;
                angle.x = 0;
                angle.z = 0;
                return stateMachine.animancerComponent.Animator.rootRotation;
            }
            
            return Quaternion.RotateTowards(transientRotation, Quaternion.LookRotation(accForward, Vector3.up), angleVelocity * deltaTime);
        }

        private void ApplyVelocityBreaking(ref Vector3 currentVelocity, float deltaTime, float friction, float breakingDeceleration)
        {
            var frictionFactor = Mathf.Max(0, brakingFrictionFactor);
            friction = Mathf.Max(0f, friction * frictionFactor);
            breakingDeceleration = Mathf.Max(0, breakingDeceleration);

            if (friction == 0 || breakingDeceleration == 0)
            {
                return;
            }

            var oldVelocity = currentVelocity;
            var maxTimeStep = Mathf.Clamp(DCharacterControllerConst.BrakingSubStepTime, 1.0f / 75.0f, 1.0f / 20.0f);
            var revAccel = -breakingDeceleration * currentVelocity.normalized;
            var remainingTime = deltaTime;
            while (remainingTime >= DCharacterControllerConst.MinTickTime)
            {
                var dt = (remainingTime > maxTimeStep) ? Mathf.Min(maxTimeStep, remainingTime * 0.5f) : remainingTime;
                remainingTime -= dt;

                currentVelocity = currentVelocity + ((-friction) * currentVelocity + revAccel) * dt;

                if (Vector3.Dot(currentVelocity, oldVelocity) < 0)
                {
                    currentVelocity = Vector3.zero;
                    return;
                }
            }
            
            if(currentVelocity.magnitude < 0.01f)
                currentVelocity = Vector3.zero;
        }

        public void NotifyJumpApex()
        {
            _notifyApex = false;
        }

        public void OnLand()
        {
            Debug.Log("[Controller] land");
            if(inputVec.magnitude > 1e-4)
                stateMachine.ChangeToState(StateType.Move);
            else
                stateMachine.ChangeToState(StateType.Idle);
        }

        public void Falling()
        {
            stateMachine.ChangeToState(StateType.Jump);
        }
    }
}

