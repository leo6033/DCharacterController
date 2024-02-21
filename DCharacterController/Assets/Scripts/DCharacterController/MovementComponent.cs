using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Disc0ver
{
    public class MovementComponent : MonoBehaviour
    {
        #region Settings

        public Rigidbody rigidbody;

        [Header("Ground Setting")] 
        [Tooltip("可站立的地面 layer")]
        public LayerMask stableGroundLayer;

        [Tooltip("最大可站立坡度")]
        [Range(0f, 89f)]
        public float maxStableAngle = 60f;


        [Header("Step Setting")] 
        [Tooltip("最大步高")]
        public float maxStepHeight = 0.5f;

        [Header("Capsule Setting")]
        public CapsuleCollider capsule;
        [SerializeField]
        private float capsuleHeight;
        [SerializeField]
        private float capsuleRadius;
        [SerializeField]
        private float capsuleYOffset;

        #endregion

        #region Properties
        
        private Vector3 velocity = Vector3.zero;
        private CharacterController _controller;

        private Vector3 _initialSimulationPosition;
        private Vector3 _transientPosition;
        private Quaternion _initialSimulationRotation;
        private Quaternion _transientRotation;

        private Vector3 _movePositionTarget;
        private bool _movePositionDirty;
        private Quaternion _moveRotationTarget;
        private bool _moveRotationDirty;

        [NonSerialized] 
        public Vector3 initialTickPosition;

        [NonSerialized] 
        public Quaternion initialTickRotation;

        [NonSerialized] 
        public LayerMask collidableLayers;

        [NonSerialized] 
        public CharacterController controller;
        
        #endregion

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            _initialSimulationPosition = position;
            _transientPosition = position;

            initialTickPosition = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
            _initialSimulationRotation = rotation;
            _transientRotation = rotation;

            initialTickRotation = rotation;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _initialSimulationPosition = position;
            _transientPosition = position;
            _initialSimulationRotation = rotation;
            _transientRotation = rotation;

            initialTickPosition = position;
            initialTickRotation = rotation;
        }

        public void ResizeCapsule(float radius, float height, float yOffset)
        {
            height = Mathf.Max(height, radius * 2 + 0.01f);

            capsule.radius = radius;
            capsule.height = height;
            capsule.center = new Vector3(0, yOffset, 0);
            
        }

        private void Awake()
        {
            _transientPosition = transform.position;
            _transientRotation = transform.rotation;
            
            collidableLayers = 0;
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(this.gameObject.layer, i))
                {
                    collidableLayers |= (1 << i);
                }
            }
            
            ResizeCapsule(capsuleRadius, capsuleHeight, capsuleYOffset);
        }

        private void Update()
        {
            PreSimulationUpdate();
            Simulate(Time.deltaTime);
            
            SetPositionAndRotation(_transientPosition, _transientRotation);
        }

        private void PreSimulationUpdate()
        {
            initialTickPosition = _transientPosition;
            initialTickRotation = _transientRotation;
            transform.SetPositionAndRotation(initialTickPosition, initialTickRotation);
        }

        private void Simulate(float deltaTime)
        {
            controller.CalcVelocity(ref velocity, deltaTime, 8f, false, 20f);

            _transientPosition += velocity * deltaTime;
        }
    }
}


