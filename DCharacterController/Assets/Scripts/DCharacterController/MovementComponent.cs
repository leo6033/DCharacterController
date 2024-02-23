using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Disc0ver
{
    public enum MoveMode
    {
        MoveNone = 0,
        MoveWalking = 1,
        MoveNavWalking = 2,
        MoveFalling = 3,
        
    }
    
    /// <summary>
    /// Contains all the information for the motor's grounding status
    /// </summary>
    public struct CharacterGroundingReport
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public Vector3 GroundNormal;
        public Vector3 InnerGroundNormal;
        public Vector3 OuterGroundNormal;

        public Collider GroundCollider;
        public Vector3 GroundPoint;

        public void CopyFrom(CharacterTransientGroundingReport transientGroundingReport)
        {
            FoundAnyGround = transientGroundingReport.FoundAnyGround;
            IsStableOnGround = transientGroundingReport.IsStableOnGround;
            SnappingPrevented = transientGroundingReport.SnappingPrevented;
            GroundNormal = transientGroundingReport.GroundNormal;
            InnerGroundNormal = transientGroundingReport.InnerGroundNormal;
            OuterGroundNormal = transientGroundingReport.OuterGroundNormal;

            GroundCollider = null;
            GroundPoint = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Contains the simulation-relevant information for the motor's grounding status
    /// </summary>
    public struct CharacterTransientGroundingReport
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public Vector3 GroundNormal;
        public Vector3 InnerGroundNormal;
        public Vector3 OuterGroundNormal;

        public void CopyFrom(CharacterGroundingReport groundingReport)
        {
            FoundAnyGround = groundingReport.FoundAnyGround;
            IsStableOnGround = groundingReport.IsStableOnGround;
            SnappingPrevented = groundingReport.SnappingPrevented;
            GroundNormal = groundingReport.GroundNormal;
            InnerGroundNormal = groundingReport.InnerGroundNormal;
            OuterGroundNormal = groundingReport.OuterGroundNormal;
        }
    }
    
    public struct HitStabilityReport
    {
        public bool IsStable;

        public bool FoundInnerNormal;
        public Vector3 InnerNormal;
        public bool FoundOuterNormal;
        public Vector3 OuterNormal;

        public bool ValidStepDetected;
        public Collider SteppedCollider;

        public bool LedgeDetected;
        public bool IsOnEmptySideOfLedge;
        public float DistanceFromLedge;
        public bool IsMovingTowardsEmptySideOfLedge;
        public Vector3 LedgeGroundNormal;
        public Vector3 LedgeRightDirection;
        public Vector3 LedgeFacingDirection;
    }

    public struct SweepHitReport
    {
        public Collider hitCollider;
        public RaycastHit hitInfo;
    }
    
    public struct OverlapResult
    {
        public Vector3 Normal;
        public Collider Collider;

        public OverlapResult(Vector3 normal, Collider collider)
        {
            Normal = normal;
            Collider = collider;
        }
    }

    public struct OverlapInfo
    {
        public OverlapResult[] overlaps;
        public int OverlapCount => _overlapCount;
        private int _overlapCount;

        public OverlapInfo(int maxRigidbodyOverlapsCount)
        {
            overlaps = new OverlapResult[maxRigidbodyOverlapsCount];
            _overlapCount = 0;
        }

        public void AddInfo(Vector3 normal, Collider collider)
        {
            if (_overlapCount < overlaps.Length)
            {
                overlaps[_overlapCount] = new OverlapResult(normal, collider);
                _overlapCount++;
            }
        }

        public void Reset()
        {
            _overlapCount = 0;
        }
    }

    public class DCharacterControllerConst
    {
        public static readonly float MIN_TICK_TIME = 1e-6f;
        public static readonly float BrakingSubStepTime = 1.0f / 33.0f;
        public static readonly float CollisionOffset = 0.01f;
    }

    
    
    public class MovementComponent : MonoBehaviour
    {
        #region Consts

        public const int MaxCollisionBudget = 16;
        public const int MaxHitBudget = 16;

        #endregion
        
        #region Settings

        public Rigidbody rigidbody;

        [Header("Component Setting")]
        [Range(0.0166f, 0.5f)]
        public float maxSimulationTimeStep = 0.02f;

        [Range(1, 25)] 
        public int maxSimulationIterations = 2;

        public int maxDecollisionIterations = 1;

        public int maxMovementSweepIterations = 5;

        public int maxRigidbodyOverlapsCount = 16;
        
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
        public CapsuleConfig capsuleConfig;

        #endregion

        #region Properties

        private Capsule _capsule;
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

        private Collider[] _internalProbedColliders = new Collider[MaxCollisionBudget];
        private OverlapInfo _overlapInfo;

        [NonSerialized] 
        public Vector3 initialTickPosition;

        [NonSerialized] 
        public Quaternion initialTickRotation;

        [NonSerialized] 
        public LayerMask collidableLayers;

        [NonSerialized] 
        public CharacterController controller;

        [NonSerialized] 
        public CharacterGroundingReport groundingStatus = new CharacterGroundingReport();

        [NonSerialized]
        public CharacterTransientGroundingReport lastGroundingStatus = new CharacterTransientGroundingReport();
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

            _capsule = new Capsule();
            _capsule.Init(this);
            _overlapInfo = new OverlapInfo(maxRigidbodyOverlapsCount);

        }

        private void Update()
        {
            Simulate(Time.deltaTime);
            
            SetPositionAndRotation(_transientPosition, _transientRotation);
        }

        private void PreSimulationUpdate()
        {
            initialTickPosition = _transientPosition;
            initialTickRotation = _transientRotation;
            transform.SetPositionAndRotation(initialTickPosition, initialTickRotation);
            
            lastGroundingStatus.CopyFrom(groundingStatus);
            groundingStatus = new CharacterGroundingReport();
            groundingStatus.GroundNormal = transform.up;
        }

        private void Simulate(float deltaTime)
        {
            PreSimulationUpdate();

            controller.CalcVelocity(ref velocity, deltaTime, 8f, false, 20f);
        }
        
        public void OnDrawGizmos()
        {
            if (_capsule == null)
                return;
            var hit = new HitStabilityReport();
            var resolutionMovement = PenetrationAdjustment(_transientPosition, _transientRotation, _internalProbedColliders, ref hit, ref _overlapInfo);

            for (int i = 0; i < _overlapInfo.OverlapCount; ++i)
            {
                var collider = _overlapInfo.overlaps[i].Collider;

                if (collider == _capsule.capsule)
                    continue; // skip ourself

                Vector3 otherPosition = collider.gameObject.transform.position;
                Quaternion otherRotation = collider.gameObject.transform.rotation;

                Vector3 direction;
                float distance;

                bool overlapped = Physics.ComputePenetration(
                    _capsule.capsule, transform.position, transform.rotation,
                    collider, otherPosition, otherRotation,
                    out direction, out distance
                );

                // draw a line showing the depenetration direction if overlapped
                if (overlapped)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(otherPosition, direction * distance);
                }
            }
        }

        private void ColliderSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance,
            out RaycastHit suitableHit)
        {
            suitableHit = new RaycastHit();
            
        }

        private void PerformMovement()
        {
            
        }

        /// <summary>
        /// 移动核心接口，后续所有的运动类型都调用这个
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="rotation"></param>
        /// <param name="isSweep"></param>
        /// <param name="hit"></param>
        private void SafeMoveUpdatedComponent(Vector3 delta, Quaternion rotation, bool isSweep, ref SweepHitReport hit)
        {
            PenetrationAdjustment(_transientPosition, _transientRotation, _internalProbedColliders, ref _overlapInfo);
            MoveUpdatedComponent(delta, rotation, isSweep, ref hit);
        }

        protected virtual void MoveUpdatedComponent(Vector3 delta, Quaternion rotation, bool isSweep, ref SweepHitReport hit)
        {
            var targetPosition = _transientPosition + delta;
            if (!isSweep)
            {
                _transientPosition = targetPosition;
                _transientRotation = rotation;
            }
            else
            {
                var remainingMovementDirection = delta.normalized;
                var remainMovementMagnitude = delta.magnitude;
                var originalVelocityDirection = remainingMovementDirection;
                int sweepMade = 0;
                bool hitSomethingThisSweep = true;
                var tmpMovedPosition = _transientPosition;
                bool previousHitIsStable = false;

                while (remainMovementMagnitude > 0f && sweepMade <= maxMovementSweepIterations)
                {
                    bool foundClosestHit = false;
                    Vector3 closestSweepHitPoint = default;
                    Vector3 closestSweepHitNormal = default;
                    float closestSweepHitDistance = 0f;
                    Collider closestSweepHitCollider = null;

                    
                }
            }
        }

        private Vector3 PenetrationAdjustment(Vector3 position, Quaternion rotation, Collider[] internalProbedColliders, ref OverlapInfo overlapInfo)
        {
            var offset = _capsule.PenetrationAdjustment(position, rotation, internalProbedColliders, ref overlapInfo);
            _transientPosition += offset;
            return offset;
        }

        private void Walking(float deltaTime, int iterations)
        {
            if (deltaTime < DCharacterControllerConst.MIN_TICK_TIME)
                return;
            var remainingTime = deltaTime;
            while (remainingTime >= DCharacterControllerConst.MIN_TICK_TIME)
            {
                var timeTick = GetSimulationTimeStep(remainingTime, iterations);
                remainingTime -= timeTick;
                
                controller.CalcVelocity(ref velocity, timeTick, 8f, false, 20f);

                var delta = velocity * timeTick;
                if (delta.magnitude < 1e-8)
                {
                    remainingTime = 0f;
                }
                else
                {
                    // SafeMoveUpdatedComponent();
                }
            }
        }

        private void StepUp()
        {
            
        }

        private void SlideAlongSurface()
        {
            
        }
        

        private float GetSimulationTimeStep(float remainingTime, int iterations)
        {
            if (remainingTime > maxSimulationTimeStep)
            {
                if (iterations < maxSimulationIterations)
                {
                    remainingTime = Mathf.Min(maxSimulationTimeStep, remainingTime * 0.5f);
                }
                else
                {
                    // If this is the last iteration, just use all the remaining time. This is usually better than cutting things short, as the simulation won't move far enough otherwise.
                    // Print a throttled warning.               
                }
            }

            return Mathf.Max(DCharacterControllerConst.MIN_TICK_TIME, remainingTime);
        }

        public bool IsStableOnNormal(Vector3 direction)
        {
            return Vector3.Angle(transform.up, direction) <= maxStableAngle;
        }

        public Vector3 GetObstructionNormal(Vector3 hitNormal, bool stableOnHit)
        {
            var obstructionNormal = hitNormal;
            if (groundingStatus.IsStableOnGround && IsGround() && IsGround() && !stableOnHit)
            {
                Vector3 obstructionLeftAlongGround = Vector3.Cross(groundingStatus.GroundNormal, obstructionNormal).normalized;
                obstructionNormal = Vector3.Cross(obstructionLeftAlongGround, transform.up).normalized;
            }

            // Catch cases where cross product between parallel normals returned 0
            if (obstructionNormal.sqrMagnitude == 0f)
            {
                obstructionNormal = hitNormal;
            }

            return obstructionNormal;
        }

        public bool IsGround()
        {
            return true;
        }
    }
}


