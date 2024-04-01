using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Disc0ver
{
    [RequireComponent(typeof(MovementComponent))]
    public class CharacterController : MonoBehaviour
    {
        public Vector3 inputVec;
        public Vector3 acceleration;

        [SerializeField, Range(1f, 10f)]
        private float maxVelocity = 1f;
        [SerializeField, Range(1f, 10f)]
        private float maxAcceleration = 7f;

        [SerializeField, Range(10f, 180f)] private float angleVelocity = 30f;

        private float brakingFrictionFactor = 2f;

        private bool _jump;

        public bool Jump => _jump;
        public float jumpForceTimeRemain = 0f;
        public float terminalFallingVelocity = 10f; 

        private void Awake()
        {
            var movementComponent = GetComponent<MovementComponent>();
            movementComponent.controller = this;
        }

        private void Start()
        {
            CameraController.Instance.FollowCharacter(this);
        }

        private void HandleInput()
        {
            inputVec.x = Input.GetAxis("Horizontal");
            inputVec.z = Input.GetAxis("Vertical");

            var cameraForward = CameraController.Instance.Forward();
            // var flag = Vector3.Dot(inputVec, cameraForward) < 0 ? -1 : 1;
            inputVec = new Vector3(inputVec.z * cameraForward.x + inputVec.x * cameraForward.z, 0,
                -inputVec.x * cameraForward.x + inputVec.z * cameraForward.z);
        }

        private void Update()
        {
            HandleInput();
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
            var accForward = acceleration;
            accForward.y = 0;

            if (inputVec.magnitude < 1e-4)
            {
                return transientRotation;
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
    }
}

