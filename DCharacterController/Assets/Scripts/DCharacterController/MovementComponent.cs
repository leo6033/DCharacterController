using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;

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

    public struct HitResult
    {
        public bool isBlockingHit;
        public bool isStartPenetrating;
        public float time;
        public Vector3 location;
        public RaycastHit hitInfo;
        public Vector3 traceStart;
        public Vector3 traceEnd;

        public HitResult(float _time = 1f)
        {
            time = _time;
            hitInfo = default;
            isBlockingHit = false;
            isStartPenetrating = false;
            location = Vector3.zero;
            traceStart = Vector3.zero;
            traceEnd = Vector3.zero;
        }

        public void Reset(float _time = 1f)
        {
            time = _time;
            hitInfo = default;
            isBlockingHit = false;
            isStartPenetrating = false;
            location = Vector3.zero;
            traceStart = Vector3.zero;
            traceEnd = Vector3.zero;
        }

        public void Update(Vector3 start, Vector3 target, RaycastHit hit, bool blockingHit, bool startPenetrating)
        {
            traceStart = start;
            traceEnd = target;
            hitInfo = hit;
            isBlockingHit = blockingHit;
            isStartPenetrating = startPenetrating;
            time = hitInfo.distance / (target - start).magnitude;
            location = start + (target - start) * time;
        }

        public bool IsValidBlockingHit => isBlockingHit && !isStartPenetrating;
    }

    [Serializable]
    public struct FindGroundResult
    {
        public bool isBlockingHit;
        public bool isWalkableFloor;
        public bool isLineTrace;
        /// <summary>
        /// the distance to the floor, computed from the trace
        /// </summary>
        public float floorDistance;
        /// <summary>
        /// The distance to the floor, computed from the trace. Only valid if isLineTrace is true.
        /// </summary>
        public float lineDistance;

        public HitResult hitResult;

        public FindGroundResult(bool isBlockingHit = false, bool isWalkableFloor = false, bool isLineTrace = false,
            float floorDistance = 0f, float lineDistance = 0f)
        {
            this.isBlockingHit = isBlockingHit;
            this.isWalkableFloor = isWalkableFloor;
            this.isLineTrace = isLineTrace;
            this.floorDistance = floorDistance;
            this.lineDistance = lineDistance;
            hitResult = default;
        }

        public float GetDistanceToFloor()
        {
            return isLineTrace ? lineDistance : floorDistance;
        }

        public void SetFromSweep(HitResult sweepResult, float distance, bool walkable)
        {
            hitResult = sweepResult;
            isBlockingHit = sweepResult.IsValidBlockingHit;
            isWalkableFloor = walkable;
            isLineTrace = false;
            floorDistance = distance;
            lineDistance = 0f;
        }

        public void SetFromLineTrace(HitResult result, float sweepFloorDist, float distance, bool walkable)
        {
            if (hitResult.isBlockingHit && result.isBlockingHit)
            {
                var oldHit = hitResult;
                hitResult = result;
                hitResult.time = oldHit.time;
                hitResult.hitInfo = oldHit.hitInfo;
                hitResult.location = oldHit.location;
                hitResult.traceStart = oldHit.traceStart;
                hitResult.traceEnd = oldHit.traceEnd;
            
                isLineTrace = true;
                isWalkableFloor = walkable;
                lineDistance = distance;
                floorDistance = sweepFloorDist;
            }
        }

        public void Clear()
        {
            isBlockingHit = false;
            isWalkableFloor = false;
            isLineTrace = false;
            floorDistance = 0f;
            lineDistance = 0f;
            hitResult.Reset();
        }
    }

    public struct StepDownResult
    {
        /// <summary>
        /// true if the floor was computed as a result of then step down
        /// </summary>
        public bool computedFloor;
        /// <summary>
        /// the result of the floor test if the floor was updated
        /// </summary>
        public FindGroundResult GroundResult;

        public StepDownResult(bool computedFloor = false)
        {
            this.computedFloor = computedFloor;
            GroundResult = new FindGroundResult();
        }
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

    public struct MovementCache
    {
        public Vector3 position;
        public Quaternion rotation;
        public FindGroundResult oldGround;
        private MovementComponent _movementComponent;

        public MovementCache(MovementComponent movementComponent)
        {
            position = movementComponent.TransientPosition;
            rotation = movementComponent.TransientRotation;
            oldGround = movementComponent.CurrentGround;
            _movementComponent = movementComponent;
        }

        public void RevertMove()
        {
            _movementComponent.RevertMove(this);
        }
    }

    public class DCharacterControllerConst
    {
        public static readonly float MinTickTime = 1e-6f;
        public static readonly float VerticalSlopeNormalY = 0.001f;
        public static readonly float BrakingSubStepTime = 1.0f / 33.0f;
        public static readonly float CollisionOffset = 0.01f;
        public static readonly float GroundSweepBackOffset = 0.1f;
        public static readonly float SweepBackOffset = 0.002f;
    }

    
    
    public class MovementComponent : MonoBehaviour
    {
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

        public int maxHitsBudget = 16;
        
        public int maxCollisionBudget = 16;

        public float minFloorDistance = 1.5f;

        public float maxFloorDistance = 2.4f;

        public float SweepEdgeRejectDistance = 0.1f;

        public float PerchRadiusThreshold = 0.15f;

        public float perchAdditionalHeight = 0.05f;

        public float fallingLateralFriction = 0.1f;

        public float gravity = 5f;

        public bool canWalkOffLedges = true;
        
        [Header("Ground Setting")] 
        [Tooltip("可站立的地面 layer")]
        public LayerMask stableGroundLayer;

        [Tooltip("最大可站立坡度")]
        [Range(0f, 89f)]
        public float maxStableAngle = 60f;

        [Tooltip("保持地面移动速度")]
        public bool maintainHorizontalGroundVelocity = false;

        [Header("Step Setting")] 
        [Tooltip("最大步高")]
        public float maxStepHeight = 0.5f;
        
        [Header("Capsule Setting")]
        public CapsuleConfig capsuleConfig;

        #endregion

        #region Properties

        private Capsule _capsule;
        [SerializeField]
        private Vector3 velocity = Vector3.zero;
        private CharacterController _controller;

        private Vector3 _initialSimulationPosition;
        private Vector3 _transientPosition;
        public Vector3 TransientPosition => _transientPosition;
        private Quaternion _initialSimulationRotation;
        private Quaternion _transientRotation;
        public Quaternion TransientRotation => _transientRotation;

        private Vector3 _movePositionTarget;
        private bool _movePositionDirty;
        private Quaternion _moveRotationTarget;
        private bool _moveRotationDirty;

        private RaycastHit[] _internalCharacterHits;
        private Collider[] _internalProbedColliders;
        private OverlapInfo _overlapInfo;
        private MoveMode _movementMode;

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

        // [NonSerialized] 
        public FindGroundResult CurrentGround;
        #endregion

//         #region Editor
//
// #if UNITY_EDITOR
//         private Collider[] _editorProbedColliders = new Collider[MaxCollisionBudget];
//         private OverlapInfo _editorOverlapInfo;
// #endif
//         
//
//         #endregion
        
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
            _capsule.Init(this, true);
            _overlapInfo = new OverlapInfo(maxRigidbodyOverlapsCount);
            _internalCharacterHits = new RaycastHit[maxHitsBudget];
            _internalProbedColliders = new Collider[maxCollisionBudget];
            _movementMode = MoveMode.MoveNone;
        }

        private void FixedUpdate()
        {
            Simulate(Time.fixedDeltaTime);
            
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

            // controller.CalcVelocity(ref velocity, deltaTime, 8f, false, 20f);
            // var delta = velocity * deltaTime;
            // _transientPosition += delta;
            
            PerformMovement(deltaTime);

            Rotate(deltaTime);
        }
        
        // public void OnDrawGizmos()
        // {
        //     if (_capsule == null)
        //         return;
        //     
        //     // var resolutionMovement = PenetrationAdjustment(_transientPosition, _transientRotation, _internalProbedColliders, ref _overlapInfo);
        //
        //     for (int i = 0; i < _overlapInfo.OverlapCount; ++i)
        //     {
        //         var collider = _overlapInfo.overlaps[i].Collider;
        //     
        //         if (collider == _capsule.capsule)
        //             continue; // skip ourself
        //     
        //         Vector3 otherPosition = collider.gameObject.transform.position;
        //         Quaternion otherRotation = collider.gameObject.transform.rotation;
        //     
        //         Vector3 direction;
        //         float distance;
        //     
        //         bool overlapped = Physics.ComputePenetration(
        //             _capsule.capsule, transform.position, transform.rotation,
        //             collider, otherPosition, otherRotation,
        //             out direction, out distance
        //         );
        //     
        //         // draw a line showing the depenetration direction if overlapped
        //         if (overlapped)
        //         {
        //             Gizmos.color = Color.red;
        //             Gizmos.DrawRay(otherPosition, direction * distance);
        //         }
        //     }
        //     
        //     // int nbOverlaps = _capsule.CollisionOverlap(_transientPosition, _transientRotation, _internalProbedColliders, 0.01f);
        //     // for (int i = 0; i < nbOverlaps; i++)
        //     // {
        //     //     Debug.Log(_internalProbedColliders[i].name);
        //     //     Gizmos.color = Color.blue;
        //     //     var point = _internalProbedColliders[i].ClosestPointOnBounds(_capsule.capsule.center);
        //     //     Gizmos.DrawRay(point, (_capsule.capsule.center - point).normalized * 3);
        //     // }
        //
        //     // var hitNumber = _capsule.CollisionSweep(transform.position, transform.position + Vector3.forward * 10, _transientRotation,
        //     //     _internalCharacterHits);
        //     // for (int i = 0; i < hitNumber; i++)
        //     // {
        //     //     Gizmos.color = Color.blue;
        //     //     Gizmos.DrawRay(_internalCharacterHits[i].point, _internalCharacterHits[i].normal * 3);
        //     // }
        //     //
        //     // hitNumber = _capsule.CollisionSweep(transform.position, transform.position + Vector3.down * 10, _transientRotation,
        //     //     _internalCharacterHits);
        //     // for (int i = 0; i < hitNumber; i++)
        //     // {
        //     //     Gizmos.color = Color.blue;
        //     //     Gizmos.DrawRay(_internalCharacterHits[i].point, _internalCharacterHits[i].normal * 3);
        //     // }
        //     
        // }

        private void StartNewPhysics(float deltaTime, int iterations)
        {
            if ((deltaTime < DCharacterControllerConst.MinTickTime) || (iterations >= maxSimulationIterations) || !HasValidData())
            {
                return;
            }

            switch (_movementMode)
            {
                case MoveMode.MoveNone:
                    break;
                case MoveMode.MoveWalking:
                    Walking(deltaTime, iterations);
                    break;
                case MoveMode.MoveFalling:
                    Falling(deltaTime, iterations);
                    break;
            }
        }

        private bool HasValidData()
        {
            return true;
        }
        
        private void SetMovementMode(MoveMode newMovementMode)
        {
            if (newMovementMode == _movementMode)
                return;
            
            Debug.Log($"movement move change {newMovementMode.ToString()}");
            var preMovementMode = _movementMode;
            _movementMode = newMovementMode;

            OnMovementModeChanged(preMovementMode, _movementMode);
        }

        private void OnMovementModeChanged(MoveMode preMovementMode, MoveMode newMovementMode)
        {
            if (newMovementMode == MoveMode.MoveWalking)
            {
                velocity.y = 0;
                FindGround(_transientPosition, ref CurrentGround, false, null);
                AdjustFloorHeight();
            }
            else
            {
                CurrentGround.Clear();
                
                if (newMovementMode == MoveMode.MoveFalling)
                {
                    // controller.Falling();
                }

                if (newMovementMode == MoveMode.MoveNone)
                {
                    
                }
            }
            
            
        }
        
        private void PerformMovement(float deltaTime)
        {
            if (_movementMode == MoveMode.MoveNone)
            {
                if (controller.inputVec.magnitude > 0)
                {
                    _movementMode = MoveMode.MoveWalking;
                }
            }

            if (_movementMode == MoveMode.MoveWalking)
            {
                Walking(deltaTime, 0);
            }
            else if (_movementMode == MoveMode.MoveFalling)
            {
                Falling(deltaTime, 0);
            }
            
            Rotate(deltaTime);
        }

        private void Rotate(float deltaTime)
        {
            var rotate = controller.GetDeltaRotation(_transientRotation, deltaTime);

            MoveUpdatedComponent(Vector3.zero, rotate, false, ref CurrentGround.hitResult);
        }

        

        /// <summary>
        /// 移动核心接口，后续所有的运动类型都调用这个
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="rotation"></param>
        /// <param name="isSweep"></param>
        /// <param name="hit"></param>
        private void SafeMoveUpdatedComponent(Vector3 delta, Quaternion rotation, bool isSweep, ref HitResult hit)
        {
            PenetrationAdjustment1(_transientPosition, _transientRotation, _internalProbedColliders, ref _overlapInfo);

            
            MoveUpdatedComponent(delta, rotation, isSweep, ref hit);
            // if (hit.isStartPenetrating)
            // {
            //     
            //     MoveUpdatedComponent(delta, rotation, isSweep, ref hit);
            // }
            
        }

        protected virtual void MoveUpdatedComponent(Vector3 delta, Quaternion rotation, bool isSweep, ref HitResult hit)
        {
            var startPosition = _transientPosition;
            var targetPosition = startPosition + delta;
            // var isPreBlockingHit = hit.isBlockingHit;
            hit.isBlockingHit = false;
            hit.time = 1f;

            var minMovementDistance = isSweep ? Mathf.Pow(4f * 0.0001f, 2) : 0f;

            if (delta.magnitude <= minMovementDistance)
            {
                hit.time = 0f;
                if (rotation.Equals(_transientRotation))
                {
                    return;
                }
            }
            
            if (!isSweep)
            {
                _transientPosition = targetPosition;
                _transientRotation = rotation;
            }
            else
            {
                var hitNumber = _capsule.CollisionSweep(startPosition, targetPosition, _transientRotation,
                    _internalCharacterHits);
                var blockingHitIndex = -1;
                var blockingHitNormalDotDelta = float.MaxValue;
                for (int i = 0; i < hitNumber; i++)
                {
                    var blockingHit = _internalCharacterHits[i];
                    // if(isPreBlockingHit && hit.hitInfo.collider == blockingHit.collider)
                        // continue;
                    // 初始存在碰撞时，优先选择与运动方向相反的
                    if (Physics.ComputePenetration(_capsule.capsule, startPosition,
                            _transientRotation,
                            blockingHit.collider, blockingHit.transform.position, blockingHit.transform.rotation,
                            out var penetrationDirection, out var penetrationDistance))
                    {
                        var normalDotDelta = Vector3.Dot(penetrationDirection, delta);
                        if (normalDotDelta < blockingHitNormalDotDelta)
                        {
                            blockingHitIndex = i;
                            blockingHitNormalDotDelta = normalDotDelta;
                            hit.isStartPenetrating = true;
                        }
                    }
                    else if(blockingHitIndex == -1)
                    {
                        blockingHitIndex = i;
                        break;
                    }
                }

                if (blockingHitIndex >= 0)
                {
                    var blockingHit = _internalCharacterHits[blockingHitIndex];
                    hit.isBlockingHit = true;
                    hit.hitInfo = blockingHit;
                    hit.time = Mathf.Max(hit.hitInfo.distance - DCharacterControllerConst.SweepBackOffset, 0) / delta.magnitude;
                    hit.traceStart = startPosition;
                    hit.traceEnd = targetPosition;

                    var moveDistance = hit.time * delta;
                    if (moveDistance.sqrMagnitude <= minMovementDistance)
                    {
                        hit.time = 0;

                    }
                    
                    targetPosition = startPosition + hit.time * delta;
                    hit.location = targetPosition;
                    Debug.DrawRay(hit.hitInfo.point, hit.hitInfo.normal, Color.red);
                    if (hit.isStartPenetrating)
                    {
                        Debug.Log($"isStartPenetrating, collider {hit.hitInfo.collider.name}");
                    }
                }

                _transientPosition = targetPosition;
                _transientRotation = rotation;

            }
        }
        
        private void FindGround(Vector3 position, ref FindGroundResult outGroundResult, bool canUseCachedLocation, HitResult? downSweepResult)
        {
            var heightCheckAdjust = IsMovingOnGround() ? maxFloorDistance : -maxFloorDistance;

            var floorSweepTraceDistance = Mathf.Max(maxFloorDistance, maxStepHeight + heightCheckAdjust);
            var floorLineTraceDistance = floorSweepTraceDistance;
            var needToValidateFloor = true;

            if (floorLineTraceDistance > 0 || floorSweepTraceDistance > 0)
            {
                // TODO: not always check floor
                ComputeGroundDist(position, floorLineTraceDistance, floorSweepTraceDistance, ref outGroundResult, _capsule.Radius, downSweepResult);
            }

            // // TODO: see if need to check perch
            // if (outGroundResult.isBlockingHit && !outGroundResult.isLineTrace)
            // {
            //     var checkRadius = true;
            //     if (ShouldComputePerchResult(outGroundResult.hitResult, checkRadius))
            //     {
            //         var maxPerchFloorDistance = Mathf.Max(maxFloorDistance, maxStepHeight + heightCheckAdjust);
            //         if (IsMovingOnGround())
            //             maxPerchFloorDistance += Mathf.Max(0, perchAdditionalHeight);
            //
            //         FindGroundResult perchGroundResult = new FindGroundResult();
            //         if (ComputePerchResult(GetValidPerchRadius(), outGroundResult.hitResult, maxPerchFloorDistance,
            //                 ref perchGroundResult))
            //         {
            //             
            //         }
            //     }
            // }
        }

        private bool ShouldComputePerchResult(HitResult hitResult, bool checkRadius)
        {
            if (!hitResult.IsValidBlockingHit)
                return false;
            
            if (GetPerchRadiusThreshold() <= SweepEdgeRejectDistance)
                return false;

            if (checkRadius)
            {
                var delta = hitResult.hitInfo.point - hitResult.location;
                delta.y = 0;
                if (delta.magnitude <= GetValidPerchRadius())
                {
                    return false;
                }
            }

            return true;
        }

        private float GetPerchRadiusThreshold()
        {
            return Mathf.Max(0, PerchRadiusThreshold);
        }

        // private bool ComputePerchResult(float radius, HitResult inHit, float maxFloorDistance, ref FindGroundResult outPerchGroundResult)
        // {
        //     if (maxFloorDistance <= 0f)
        //         return false;
        //
        //     var capsuleLocation = inHit.traceStart;
        //
        //     var inHitAboveBase = Mathf.Max(0, inHit.hitInfo.point.y - capsuleLocation.y);
        //     
        // }

        private float GetValidPerchRadius()
        {
            var radius = _capsule.Radius;
            return Mathf.Clamp(radius - GetPerchRadiusThreshold(), 0.11f, radius);
        }

        private void ComputeGroundDist(Vector3 capsuleLocation, float lineDistance, float sweepDistance, ref FindGroundResult outGroundResult, float sweepRadius, HitResult? downSweepResult)
        {
            var skipSweep = false;
            if (downSweepResult != null && downSweepResult.Value.IsValidBlockingHit)
            {
                // 垂直向下
                if (downSweepResult.Value.traceStart.y > downSweepResult.Value.traceEnd.y)
                {
                    if (IsWithinEdgeTolerance(downSweepResult.Value, _capsule))
                    {
                        var start2D = new Vector2(downSweepResult.Value.traceStart.x, downSweepResult.Value.traceStart.z);
                        var end2D = new Vector2(downSweepResult.Value.traceEnd.x, downSweepResult.Value.traceEnd.z);
                        if ((start2D - end2D).magnitude <= 1e-4)
                        {
                            skipSweep = true;

                            var walkable = IsStableOnNormal(downSweepResult.Value.hitInfo.normal);
                            var floorDistance = capsuleLocation.y - downSweepResult.Value.location.y;
                            outGroundResult.SetFromSweep(downSweepResult.Value, floorDistance, walkable);

                            if (walkable)
                                return;
                        }
                    }
                }
            }

            if (!skipSweep && sweepDistance > 0f && lineDistance > 0f)
            {
                var shrinkScale = 0.9f;
                var shrinkScaleOverlap = 0.1f;
                var shrinkHeight = (_capsule.Height * 0.5f - _capsule.Radius) * (1 - shrinkScale);
                var traceDistance = sweepDistance + shrinkHeight;
                Capsule capsule = new Capsule();
                capsule.Init(this);
                capsule.ResizeCapsule(sweepRadius, _capsule.Height - shrinkHeight * 2, capsuleConfig.capsuleYOffset);

                HitResult hit = new HitResult(1f);
                var blockingHit = FloorSweep(capsuleLocation, capsuleLocation + new Vector3(0, -traceDistance, 0),
                    _transientRotation, ref hit, capsule);

                if (blockingHit)
                {
                    if (hit.isStartPenetrating || !IsWithinEdgeTolerance(hit, capsule))
                    {
                        var radius = Mathf.Max(0f, capsule.Radius - SweepEdgeRejectDistance - 1e-4f);
                        if (radius > 1e-4f)
                        {
                            shrinkHeight = (_capsule.Height * 0.5f - _capsule.Radius) *
                                           (1 - shrinkScaleOverlap);
                            traceDistance = sweepDistance + shrinkHeight;
                            
                            capsule.ResizeCapsule(radius, Mathf.Max(radius * 2, _capsule.Height - shrinkHeight * 2f), capsuleConfig.capsuleYOffset);
                            hit.Reset(1f);
                            
                            blockingHit = FloorSweep(capsuleLocation, capsuleLocation + new Vector3(0, -traceDistance, 0),
                                _transientRotation, ref hit, capsule);
                        }
                    }

                    var maxPenetrationAdjust = Mathf.Max(maxFloorDistance, capsule.Radius);
                    var sweepResult = Mathf.Max(-maxPenetrationAdjust, hit.time * traceDistance - shrinkHeight);
                    outGroundResult.SetFromSweep(hit, sweepResult, false);
                    if (hit.IsValidBlockingHit && IsStableOnNormal(hit.hitInfo.normal))
                    {
                        if (sweepResult <= sweepDistance)
                        {
                            outGroundResult.isWalkableFloor = true;
                            return;
                        }
                    }
                }
            }

            if (!outGroundResult.isBlockingHit && !outGroundResult.hitResult.isStartPenetrating)
            {
                outGroundResult.floorDistance = sweepDistance;
                return;
            }

            if (lineDistance > 0f)
            {
                var shrinkHeight = _capsule.Height * 0.5f;
                var lineTraceStart = capsuleLocation + _capsule.Center;
                var traceDistance = lineDistance + shrinkHeight;
                var down = new Vector3(0, -traceDistance, 0);

                HitResult hit = new HitResult(1f);
                var blockingHit = Physics.Linecast(lineTraceStart, lineTraceStart + down, out hit.hitInfo);
                hit.Update(lineTraceStart, lineTraceStart + Vector3.down, hit.hitInfo, blockingHit, false);

                if (blockingHit)
                {
                    if (hit.time > 0)
                    {
                        var maxPenetrationAdjust = Mathf.Max(maxFloorDistance, _capsule.Radius);
                        var lineResult = Mathf.Max(-maxPenetrationAdjust, hit.time * traceDistance - shrinkHeight);

                        outGroundResult.isBlockingHit = true;
                        if (lineResult <= lineDistance && IsStableOnNormal(hit.hitInfo.normal))
                        {
                            outGroundResult.SetFromLineTrace(hit, outGroundResult.floorDistance, lineResult, true);
                            return;
                        }
                    }
                }
            }

            outGroundResult.isWalkableFloor = false;
        }

        private void AdjustFloorHeight()
        {
            if (!CurrentGround.isWalkableFloor)
                return;

            var oldFloorDist = CurrentGround.floorDistance;
            if (CurrentGround.isLineTrace)
            {
                if (oldFloorDist < minFloorDistance && CurrentGround.lineDistance >= minFloorDistance)
                    return;
                else
                    oldFloorDist = CurrentGround.lineDistance;
            }

            if (oldFloorDist < minFloorDistance || oldFloorDist > maxFloorDistance)
            {
                HitResult adjustHit = new HitResult(1f);
                var initialY = _transientPosition.y;
                var avgFloorDist = (minFloorDistance + maxFloorDistance) / 2f;
                var moveDistance = avgFloorDist - oldFloorDist;
                SafeMoveUpdatedComponent(new Vector3(0, moveDistance, 0), _transientRotation, true, ref adjustHit);

                var currentY = _transientPosition.y;
                if (!adjustHit.IsValidBlockingHit)
                {
                    CurrentGround.floorDistance += moveDistance;
                }
                else if (moveDistance > 0f)
                {
                    CurrentGround.floorDistance += currentY - initialY;
                }
                else
                {
                    CurrentGround.floorDistance = currentY - adjustHit.location.y;
                    if (IsStableOnNormal(adjustHit.hitInfo.normal))
                    {
                        CurrentGround.SetFromSweep(adjustHit, CurrentGround.floorDistance, true);
                    }
                }
            }
        }
        
        private bool FloorSweep(Vector3 startPosition, Vector3 endPosition, Quaternion rotation, ref HitResult hit, Capsule capsule)
        {
            return capsule.CollisionFloorSweep(startPosition, endPosition, rotation, ref hit);
        }
        

        private Vector3 PenetrationAdjustment1(Vector3 position, Quaternion rotation, Collider[] internalProbedColliders, ref OverlapInfo overlapInfo)
        {
            var offset = _capsule.PenetrationAdjustment(position, rotation, internalProbedColliders, ref overlapInfo);
            _transientPosition += offset;
            return offset;
        }

        private void Walking(float deltaTime, int iterations)
        {
            if (deltaTime < DCharacterControllerConst.MinTickTime)
                return;
            var remainingTime = deltaTime;
            var checkFall = false;
            var triedLedgeMove = false;
            var stepUp = false;
            while (remainingTime >= DCharacterControllerConst.MinTickTime && iterations < maxSimulationIterations)
            {
                iterations++;
                var oldLocation = _transientPosition;
                var timeTick = GetSimulationTimeStep(remainingTime, iterations);
                remainingTime -= timeTick;
                var oldGround = CurrentGround;
                
                controller.CalcVelocity(ref velocity, timeTick, 8f, false, 20f);

                var delta = velocity * timeTick;
                StepDownResult? stepDownResult = new StepDownResult();
                var zeroDelta = delta.magnitude < 1e-8;
                if (zeroDelta)
                {
                    remainingTime = 0f;
                }
                else
                {
                    MoveAlongGround(velocity, timeTick, ref stepDownResult);
                }

                if (stepDownResult != null && stepDownResult.Value.computedFloor)
                {
                    CurrentGround = stepDownResult.Value.GroundResult;
                    stepUp = true;
                }
                else
                {
                    stepUp = false;
                    FindGround(_transientPosition, ref CurrentGround, zeroDelta, null);
                }
                
                // TODO: 完善悬崖判断
                var checkLedges = !CanWalkOffLedges();
                if (checkLedges && !CurrentGround.isWalkableFloor)
                {
                    // var gravDir = new Vector3(0, -1, 0);
                    // var newDelta = triedLedgeMove ? Vector3.zero : GetLedgeMove(oldLocation, delta, gravDir);
                    
                }
                else
                {
                    if (CurrentGround.isWalkableFloor)
                    {
                        AdjustFloorHeight();
                    }
                    else if (CurrentGround.hitResult.isStartPenetrating && remainingTime <= 0f)
                    {
                        HitResult hit = CurrentGround.hitResult;
                        hit.traceEnd = hit.traceStart + new Vector3(0, maxFloorDistance, 0);
                        PenetrationAdjustment1(_transientPosition, _transientRotation, _internalProbedColliders,
                            ref _overlapInfo);
                    }
                
                    // 下落判断
                    if (!stepUp && !CurrentGround.isWalkableFloor && !CurrentGround.hitResult.isStartPenetrating)
                    {
                        if (CheckFall(oldGround, CurrentGround.hitResult, delta, oldLocation, remainingTime, timeTick,
                                iterations, controller.Jump))
                        {
                            return;
                        }
                    }
                }

                if ((_transientPosition - oldLocation).magnitude < 1e-4)
                {
                    remainingTime = 0f;
                    break;
                }
            }
        }
        

        // private Vector3 GetLedgeMove(Vector3 oldLocation, Vector3 delta, Vector3 gravyDir)
        // {
        //     if (!HasValidData() || delta.magnitude < 1e-4)
        //     {
        //         return Vector3.zero;
        //     }
        //
        //     var sideDir = new Vector3(delta.z, 0, -delta.x);
        //     if()
        // }

        private void Falling(float deltaTime, int iterations)
        {
            if (deltaTime < DCharacterControllerConst.MinTickTime)
                return;
            var remainTime = deltaTime;
            while ((remainTime >= DCharacterControllerConst.MinTickTime) && (iterations < maxSimulationIterations))
            {
                iterations++;
                var timeTick = GetSimulationTimeStep(remainTime, iterations);
                remainTime -= timeTick;

                var oldLocation = _transientPosition;
                var oldRotation = _transientRotation;
                var oldVelocity = velocity;
                
                velocity.y = 0;
                controller.CalcVelocity(ref velocity, deltaTime, fallingLateralFriction, false, 20f);
                velocity.y = oldVelocity.y;

                var vGravity = new Vector3(0, -gravity, 0);
                var gravityTime = timeTick;
                var endingJumpForce = false;
                if (controller.jumpForceTimeRemain > 0f)
                {
                    var jumpForceTime = Mathf.Min(controller.jumpForceTimeRemain, timeTick);
                    gravityTime = Mathf.Max(0f, timeTick - jumpForceTime);

                    controller.jumpForceTimeRemain -= jumpForceTime;
                    if (controller.jumpForceTimeRemain <= 0f)
                    {
                        endingJumpForce = true;
                    }
                }

                velocity = NewFallVelocity(velocity, vGravity, gravityTime);

                var adjusted = 0.5f * (oldVelocity + velocity) * timeTick;
                HitResult hitResult = new HitResult();
                
                SafeMoveUpdatedComponent(adjusted, _transientRotation, true, ref hitResult);

                if (!HasValidData())
                    return;

                var lastMoveTimeSlice = timeTick;
                var subTimeTickRemain = timeTick * (1 - hitResult.time);
                
                // TODO: check Enter Water
                if (hitResult.isBlockingHit)
                {
                    if (IsValidLandingSpot(_transientPosition, hitResult))
                    {
                        remainTime += subTimeTickRemain;
                        ProcessLanded(hitResult, remainTime, iterations);
                    }
                    else
                    {
                        adjusted = velocity * timeTick;
                        if (!hitResult.isStartPenetrating &&
                            ShouldCheckForValidLandingSpot(timeTick, adjusted, hitResult))
                        {
                            var location = _transientPosition;
                            FindGroundResult groundResult = new FindGroundResult();
                            FindGround(location, ref groundResult, false, null);
                            if (groundResult.isWalkableFloor && IsValidLandingSpot(location, groundResult.hitResult))
                            {
                                remainTime += subTimeTickRemain;
                                ProcessLanded(groundResult.hitResult, remainTime, iterations);
                                return;
                            }
                        }

                        if (!HasValidData() || !IsFalling())
                            return;

                        // var velocityNoAirControl = oldVelocity;
                        // var airControlAccel = controller.acceleration;
                        
                        // 斜坡
                        var oldHitNormal = (hitResult.location + _transientRotation * _capsule.Center - hitResult.hitInfo.point).normalized;
                        var oldHitImpactNormal = hitResult.hitInfo.normal;

                        var delta = ComputeSlideVector(adjusted, 1 - hitResult.time, oldHitImpactNormal, hitResult);

                        if (subTimeTickRemain > 1e-4 && Vector3.Dot(delta, adjusted) > 0)
                        {
                            SafeMoveUpdatedComponent(delta, _transientRotation, true, ref hitResult);

                            if (hitResult.isBlockingHit)
                            {
                                // hit second wall
                                lastMoveTimeSlice = subTimeTickRemain;
                                subTimeTickRemain = subTimeTickRemain * (1 - hitResult.time);

                                if (IsValidLandingSpot(_transientPosition, hitResult))
                                {
                                    remainTime += subTimeTickRemain;
                                    ProcessLanded(hitResult, remainTime, iterations);
                                    return;
                                }

                                if (!HasValidData() || !IsFalling())
                                    return;

                                // if (hitResult.hitInfo.normal.y > DCharacterControllerConst.VerticalSlopeNormalY)
                                // {
                                //     var lastMoveNoAirControl = velocityNoAirControl * lastMoveTimeSlice;
                                //     delta = ComputeSlideVector(lastMoveNoAirControl, 1f, oldHitImpactNormal, hitResult);
                                // }

                                var preTwoWallDelta = delta;
                                TwoWallAdjust(delta, ref hitResult, oldHitImpactNormal);
                                
                                SafeMoveUpdatedComponent( delta, _transientRotation, true, ref hitResult);
                                // if (hitResult.time == 0)
                                // {
                                //     // 卡住了，尝试回避
                                //     var sideDelta = oldHitImpactNormal + hitResult.hitInfo.normal;
                                //     sideDelta.y = 0;
                                //
                                //     if (sideDelta.magnitude < 1e-4)
                                //     {
                                //         sideDelta = new Vector3(oldHitImpactNormal.z, 0, -oldHitImpactNormal.x)
                                //             .normalized;
                                //     }
                                //     SafeMoveUpdatedComponent(sideDelta, _transientRotation, true, ref hitResult);
                                // }

                                // 峡谷
                                var ditch = ((oldHitImpactNormal.y > 0f) && (hitResult.hitInfo.normal.y > 0f) &&
                                             (Mathf.Abs(delta.y) <= 1e-4) &&
                                             (Vector3.Dot(hitResult.hitInfo.normal, oldHitImpactNormal) < 0));
                                if (ditch || IsValidLandingSpot(_transientPosition, hitResult) || hitResult.time == 0f)
                                {
                                    remainTime = 0f;
                                    ProcessLanded(hitResult, remainTime, iterations);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            
        }

        private bool ShouldCheckForValidLandingSpot(float deltaTime, Vector3 delta, HitResult hitResult)
        {
            if (hitResult.hitInfo.normal.y > 1e-4)
            {
                if (IsWithinEdgeTolerance(hitResult, _capsule))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 NewFallVelocity(Vector3 currentVelocity, Vector3 vGravity, float deltaTime)
        {
            var result = currentVelocity;
            if (deltaTime > 0f)
            {
                result += vGravity * deltaTime;
                if (result.magnitude > controller.terminalFallingVelocity)
                {
                    var gravityDir = vGravity.normalized;
                    if (Vector3.Dot(result, gravityDir) > controller.terminalFallingVelocity)
                    {
                        result = Vector3.ProjectOnPlane(result, gravityDir) +
                                 gravityDir * controller.terminalFallingVelocity;
                    }
                }
            }

            return result;
        }
        
        private bool CheckFall(FindGroundResult oldFloor, HitResult hit, Vector3 delta, Vector3 oldPosition, float remainTime, float timeTick, int iteration, bool mustJump)
        {
            if (mustJump || CanWalkOffLedges())
            {
                HandleWalkingOffLedge();
                if (IsMovingOnGround())
                {
                    StartFalling(iteration, remainTime, timeTick, delta, oldPosition);
                }
                return true;
            }

            return false;
        }

        private bool IsValidLandingSpot(Vector3 position, HitResult hitResult)
        {
            if (!hitResult.isBlockingHit)
                return false;

            if (!hitResult.isStartPenetrating)
            {
                if (!IsStableOnNormal(hitResult.hitInfo.normal))
                {
                    return false;
                }

                var lowerHemisphereY =  hitResult.location.y + _capsule.Radius;
                if (hitResult.hitInfo.point.y >= lowerHemisphereY)
                {
                    return false;
                }

                if (!IsWithinEdgeTolerance(hitResult, _capsule))
                {
                    return false;
                }
            }
            else
            {
                if (hitResult.hitInfo.normal.y < 1e-4)
                {
                    return false;
                }
            }

            FindGroundResult groundResult = new FindGroundResult();
            FindGround(_transientPosition, ref groundResult, false, hitResult);

            return groundResult.isWalkableFloor;
        }

        private void ProcessLanded(HitResult hitResult, float remainTime, int iterations)
        {
            Debug.Log("[MovementComp] ProcessLanded");
            if (IsFalling())
            {
                SetPostLandedPhysics(hitResult);
            }
            
            StartNewPhysics(remainTime, iterations);
        }

        private void SetPostLandedPhysics(HitResult hitResult)
        {
            var impactAccel = controller.acceleration + (IsFalling() ? new Vector3(0, -gravity, 0) : Vector3.zero);
            var impactVelocity = velocity;
            SetMovementMode(MoveMode.MoveWalking);

            ApplyImpactPhysicsForces(hitResult, impactAccel, impactVelocity);
        }

        private void ApplyImpactPhysicsForces(HitResult hitResult, Vector3 impactAccel, Vector3 impactVelocity)
        {
            
        }
        
        private void HandleWalkingOffLedge()
        {
            
        }

        private bool CanWalkOffLedges()
        {
            
            return canWalkOffLedges;
        }
        
        private void StartFalling(int iterations, float remainTime, float timeTick, Vector3 delta, Vector3 subLoc)
        {
            Debug.Log("[MovementComp] start falling");
            var distance = delta.magnitude;
            var actualDistance = (_transientPosition - subLoc).magnitude;
            remainTime = distance < 1e-4 ? 0f : remainTime + timeTick * (1f - Mathf.Min(1f, actualDistance / distance));

            if (IsMovingOnGround())
            {
                SetMovementMode(MoveMode.MoveFalling);
            }

            StartNewPhysics(remainTime, iterations);
        }
        
        private void MoveAlongGround(Vector3 moveVelocity, float deltaTime, ref StepDownResult? outStepDownResult)
        {
            var delta = moveVelocity * deltaTime;
            // Debug.Log($"Move along floor, delta {delta}, velocity {moveVelocity}, deltaTime {deltaTime}");
            Debug.DrawLine(_transientPosition, _transientPosition + moveVelocity, Color.black);
            var hit = new HitResult();
            float lastMoveSlice = deltaTime;
            
            SafeMoveUpdatedComponent(delta, _transientRotation, true, ref hit);
            
            if (hit.isStartPenetrating)
            {
                Debug.Log("move start penetrating");
                SlideAlongSurface(delta, 1f, hit.hitInfo.normal, ref hit, true);
            }
            else if(hit.IsValidBlockingHit)
            {
                Debug.Log($"move blocking, time {hit.time}");
                var percentTimeApplied = hit.time;
                if (hit.time > 0f && IsStableOnNormal(hit.hitInfo.normal))
                {
                    float percentRemain = 1 - hit.time;
                    lastMoveSlice = percentRemain * lastMoveSlice;
                    var vector = ComputeGroundMovementDelta(delta * percentRemain, hit, false);
                    SafeMoveUpdatedComponent(vector, _transientRotation, true, ref hit);

                    var secondHitPercent = hit.time * percentRemain;
                    percentTimeApplied = Mathf.Clamp(percentTimeApplied + secondHitPercent, 0f, 1f);
                }

                if (hit.IsValidBlockingHit)
                {
                    if (CanStepUp(hit))
                    {
                        var gravDir = Vector3.down;
                        var preStepUpLocation = _transientPosition;
                        if (!StepUp(gravDir, delta * (1f - percentTimeApplied), ref hit, ref outStepDownResult))
                        {
                            SlideAlongSurface(delta, 1 - percentTimeApplied, hit.hitInfo.normal, ref hit, true);
                        }
                        else
                        {
                            if (!maintainHorizontalGroundVelocity)
                            {
                                var stepUpTimeSlice = (1 - percentTimeApplied) * deltaTime;
                                if (stepUpTimeSlice > 0)
                                {
                                    velocity = (_transientPosition - preStepUpLocation) / stepUpTimeSlice;
                                    velocity.y = 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        SlideAlongSurface(delta, 1 - percentTimeApplied, hit.hitInfo.normal, ref hit, true);
                    }
                }
            }
        }

        private Vector3 ComputeGroundMovementDelta(Vector3 delta, HitResult hit, bool isHitFromLineTrace)
        {
            var floorNormal = hit.hitInfo.normal;
            if (IsStableOnNormal(floorNormal) && !isHitFromLineTrace)
            {
                var right = Vector3.Cross(delta, floorNormal);
                var forward = Vector3.Cross(floorNormal, right).normalized;
                
                if (maintainHorizontalGroundVelocity)
                {
                    return forward * delta.magnitude;
                }
                else
                {
                    return Vector3.Project(delta, forward);
                }
            }

            return delta;
        }

        private bool CanStepUp(HitResult hit)
        {
            if (!hit.IsValidBlockingHit)
            {
                return false;
            }

            // check for hit game object can be step up
            
            return true;
        }

        private bool StepUp(Vector3 gravDir, Vector3 delta, ref HitResult inHit, ref StepDownResult? outStepDownResult)
        {
            Debug.Log("Start step up");
            var oldLocation = _transientPosition;
            if (gravDir.magnitude < 1e-8)
            {
                return false;
            }

            var initialHitY = inHit.hitInfo.point.y;
            // 碰撞在顶部,不爬升
            if (initialHitY > oldLocation.y + _capsule.Height - _capsule.Radius)
            {
                return false;
            }

            var stepUpHeight = maxStepHeight;
            var stepDownHeight = stepUpHeight;
            var initialFloorBaseY = oldLocation.y;
            var floorPointY = initialFloorBaseY;

            if (IsMovingOnGround() && CurrentGround.isWalkableFloor)
            {
                var floorDist = Mathf.Max(0, CurrentGround.GetDistanceToFloor());
                initialFloorBaseY -= floorDist;
                stepUpHeight = Mathf.Max(stepUpHeight - floorDist, 0f);
                stepDownHeight = maxStepHeight + maxFloorDistance * 2f;

                // var isHitVerticalFace = inHit.hitInfo.point.y > inHit.location.y + _capsule.Radius;
                var isHitVerticalFace = !IsWithinEdgeTolerance(inHit, _capsule); 
                if (!CurrentGround.isLineTrace && !isHitVerticalFace)
                {
                    floorPointY = CurrentGround.hitResult.hitInfo.point.y;
                }
                else
                {
                    floorPointY -= floorDist;
                }
            }

            // 撞击点在地板下
            if (initialHitY <= initialFloorBaseY)
            {
                return false;
            }

            MovementCache movementCache = new MovementCache(this);

            HitResult sweepUpHit = new HitResult(1f);
            MoveUpdatedComponent(-gravDir.normalized * stepUpHeight, _transientRotation, true, ref sweepUpHit);

            if (sweepUpHit.isStartPenetrating)
            {
                Debug.Log("[StepUp] sweep up fail");
                movementCache.RevertMove();
                return false;
            }

            HitResult hit = new HitResult(1f);
            MoveUpdatedComponent(delta, _transientRotation, true, ref hit);

            if (hit.isBlockingHit)
            {
                if (hit.isStartPenetrating)
                {
                    Debug.Log("[StepUp] sweep forward fail, start penetrate");
                    movementCache.RevertMove();
                    return false;
                }

                if (IsFalling())
                    return true;

                var forwardHitTime = hit.time;
                var forwardSlideAmount = SlideAlongSurface(delta, 1 - forwardHitTime, hit.hitInfo.normal,
                    ref hit, true);

                if (IsFalling())
                {
                    Debug.Log("[StepUp] sweep forward fail, hit and slide and falling");
                    movementCache.RevertMove();
                    return false;
                }

                if (hit.time == 0f && forwardSlideAmount == 0f)
                {
                    Debug.Log("[StepUp] sweep forward fail, no move");
                    movementCache.RevertMove();
                    return false;
                }
            }

            MoveUpdatedComponent(gravDir * stepDownHeight, _transientRotation, true, ref hit);
            if (hit.isStartPenetrating)
            {
                Debug.Log("[StepUp] sweep down fail");
                movementCache.RevertMove();
                return false;
            }

            StepDownResult stepDownResult = new StepDownResult();
            if (hit.IsValidBlockingHit)
            {
                var deltaY = hit.hitInfo.point.y - floorPointY;
                if (deltaY > maxStepHeight)
                {
                    Debug.Log($"[StepUp] down fail, deltaY({deltaY}) greater than maxStepHeight{maxStepHeight}");
                    movementCache.RevertMove();
                    return false;
                }

                // if (!IsStableOnNormal(hit.hitInfo.normal))
                // {
                //     if (Vector3.Dot(delta, hit.hitInfo.normal) < 0f)
                //     {
                //         Debug.Log($"[StepUp] down fail, unWalkable normal opposed then movement");
                //         movementCache.RevertMove();
                //         return false;
                //     }
                //
                //     if (inHit.location.y > oldLocation.y)
                //     {
                //         Debug.Log($"[StepUp] down fail, unWalkable normal above old position");
                //         movementCache.RevertMove();
                //         return false;
                //     }
                // }

                if (!IsWithinEdgeTolerance(hit, _capsule))
                {
                    Debug.Log($"[StepUp] down fail, hit point is in edge");
                    movementCache.RevertMove();
                    return false;
                }

                if (deltaY > 0f && !CanStepUp(hit))
                {
                    Debug.Log($"[StepUp] down fail, up on to surface can't step up");
                    movementCache.RevertMove();
                    return false;
                }

                if (outStepDownResult != null)
                {
                    FindGround(_transientPosition, ref stepDownResult.GroundResult, false, hit);
                    if (hit.location.y > oldLocation.y)
                    {
                        if (!stepDownResult.GroundResult.isBlockingHit)
                        {
                            movementCache.RevertMove();
                            return false;
                        }
                    }

                    stepDownResult.computedFloor = true;
                }
                
            }

            if (outStepDownResult != null)
            {
                outStepDownResult = stepDownResult;
            }

            return true;
        }

        private float SlideAlongSurface(Vector3 delta, float time, Vector3 hitNormal, ref HitResult hit, bool handleImpact)
        {
            Debug.Log($"Slide along surface, collider: {hit.hitInfo.collider.name}");
            if (!hit.isBlockingHit)
                return 0f;

            Vector3 normal = hitNormal;
            if (IsMovingOnGround())
            {
                if (normal.y > 0)
                {
                    if (!IsStableOnNormal(normal))
                    {
                        normal.y = 0;
                    }
                }
            }
            else if (normal.y < -1e-4)
            {
                if (CurrentGround.floorDistance < minFloorDistance && CurrentGround.isBlockingHit)
                {
                    var floorNormal = CurrentGround.hitResult.hitInfo.normal;
                    var floorOpposedToMovement = (Vector3.Dot(delta, floorNormal) < 0f) && (floorNormal.y < 1f - 1e-4f);
                    if (floorOpposedToMovement)
                    {
                        normal = floorNormal;
                    }

                    normal.y = 0;
                }
            }
            
            Debug.Log($"slide normal {normal}");
            return InternalSlideAlongSurface(delta, time, normal.normalized, ref hit, handleImpact);
        }

        private float InternalSlideAlongSurface(Vector3 delta, float time, Vector3 normal, ref HitResult hit,
            bool handleImpact)
        {
            if (!hit.isBlockingHit)
                return 0f;

            var percentTimeApplied = 0f;
            var oldHitNormal = normal;

            var slideDelta = ComputeSlideVector(delta, time, normal, hit);
            Debug.DrawRay(hit.hitInfo.point, slideDelta * 100, Color.magenta);

            if (Vector3.Dot(slideDelta, delta) > 0)
            {
                SafeMoveUpdatedComponent(slideDelta, _transientRotation, true, ref hit);

                var firstHitPercent = hit.time;
                percentTimeApplied = firstHitPercent;
                if (hit.IsValidBlockingHit)
                {
                    if (handleImpact)
                    {
                        
                    }
                    
                    Debug.Log($"Slide along next surface, collider: {hit.hitInfo.collider.name}");
                    slideDelta = TwoWallAdjust(slideDelta, ref hit, oldHitNormal);
                    Debug.DrawRay(hit.hitInfo.point, slideDelta * 100, Color.magenta);
                    if (slideDelta.magnitude > 1e-4f && Vector3.Dot(slideDelta, delta) > 0f)
                    {
                        SafeMoveUpdatedComponent(slideDelta, _transientRotation, true, ref hit);
                        var secondPercent = hit.time * (1 - firstHitPercent);
                        percentTimeApplied += secondPercent;

                        if (handleImpact && hit.isBlockingHit)
                        {
                            
                        }
                    }
                }

                return Mathf.Clamp(percentTimeApplied, 0f, 1f);
            }

            return 0;
        }

        private Vector3 TwoWallAdjust(Vector3 delta, ref HitResult hit, Vector3 oldHitNormal)
        {
            var hitNormal = hit.hitInfo.normal;
            var desireDir = delta;

            // 90 or less corner, use cross product for direction
            if (Vector3.Dot(oldHitNormal, hitNormal) <= 0f)
            {
                var newDir = Vector3.Cross(hitNormal, oldHitNormal).normalized;
                delta = Vector3.Dot(delta, newDir) * (1 - hit.time) * newDir;
                if (Vector3.Dot(desireDir, delta) < 0f)
                    delta = -delta;
            }
            else
            {
                delta = ComputeSlideVector(delta, 1 - hit.time, hitNormal, hit);
                if (Vector3.Dot(delta, desireDir) <= 0f)
                {
                    delta = Vector3.zero;
                }
                else if (Mathf.Abs(Vector3.Dot(hitNormal, oldHitNormal) - 1f) < 1e-4)
                {
                    delta += hitNormal * 0.01f;
                }
            }

            if (IsMovingOnGround())
            {
                if (delta.y > 0)
                {
                    if (IsStableOnNormal(hit.hitInfo.normal) && hit.hitInfo.normal.y > 1e-4f)
                    {
                        var time = 1 - hit.time;
                        var scaledDelta = delta.normalized * desireDir.magnitude;
                        delta = new Vector3(desireDir.x, scaledDelta.y / hitNormal.y, desireDir.z) * time;
                        if (delta.y > maxStepHeight)
                        {
                            var rescale = maxStepHeight / delta.y;
                            delta *= rescale;
                        }
                    }
                    else
                    {
                        delta.y = 0;
                    }
                }
                else if(delta.y < 0)
                {
                    if (CurrentGround.floorDistance < minFloorDistance && CurrentGround.isBlockingHit)
                        delta.y = 0;
                }
            }

            return delta;
        }

        private Vector3 ComputeSlideVector(Vector3 delta, float time, Vector3 normal, HitResult hit)
        {
            var result = Vector3.ProjectOnPlane(delta, normal) * time;
            if (IsFalling())
            {
                result = HandleSlopeBoosting(result, delta, time, normal, hit);
            }

            return result;
        }

        private Vector3 HandleSlopeBoosting(Vector3 slideResult, Vector3 delta, float time, Vector3 normal, HitResult hit)
        {
            var result = slideResult;
            if (result.y > 0f)
            {
                var yLimit = delta.y * time;
                if (result.y - yLimit > 1e-4)
                {
                    if (yLimit > 0)
                    {
                        var upPercent = yLimit / result.y;
                        result *= upPercent;
                    }
                    else
                    {
                        result = Vector3.zero;
                    }

                    var remainderXZ = Vector3.Project(slideResult - result, Vector3.one - Vector3.up);
                    var normalXZ = normal;
                    var adjust = Vector3.ProjectOnPlane(remainderXZ, normalXZ);
                    result += adjust;
                }
            }

            return result;
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

            return Mathf.Max(DCharacterControllerConst.MinTickTime, remainingTime);
        }

        public bool IsStableOnNormal(Vector3 direction)
        {
            return Vector3.Angle(transform.up, direction) <= maxStableAngle;
        }

        public Vector3 GetObstructionNormal(Vector3 hitNormal, bool stableOnHit)
        {
            var obstructionNormal = hitNormal;
            if (CurrentGround.isWalkableFloor && IsMovingOnGround() && !stableOnHit)
            {
                Vector3 obstructionLeftAlongGround = Vector3.Cross(CurrentGround.hitResult.hitInfo.normal, obstructionNormal).normalized;
                obstructionNormal = Vector3.Cross(obstructionLeftAlongGround, transform.up).normalized;
            }

            // Catch cases where cross product between parallel normals returned 0
            if (obstructionNormal.sqrMagnitude == 0f)
            {
                obstructionNormal = hitNormal;
            }

            return obstructionNormal;
        }

        public bool IsMovingOnGround()
        {
            return _movementMode == MoveMode.MoveWalking || _movementMode == MoveMode.MoveNavWalking;
        }

        public bool IsFalling()
        {
            return _movementMode == MoveMode.MoveFalling;
        }

        public void RevertMove(MovementCache cache)
        {
            _transientPosition = cache.position;
            _transientRotation = cache.rotation;
            CurrentGround = cache.oldGround;
        }

        private bool IsWithinEdgeTolerance(HitResult hit, Capsule capsule)
        {
            var delta = hit.hitInfo.point - hit.location;
            delta.y = 0;
            var reducedRadius = Mathf.Max(SweepEdgeRejectDistance, capsule.Radius - SweepEdgeRejectDistance);
            return delta.magnitude < reducedRadius;
        }
    }
}


