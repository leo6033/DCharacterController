using System;
using UnityEngine;

namespace Disc0ver
{
    [Serializable]
    public struct CapsuleConfig
    {
        public float capsuleHeight;
        public float capsuleRadius;
        public float capsuleYOffset;
    }

    public class Capsule
    {
        public CapsuleCollider capsule;
        private MovementComponent _movementComponent;
        
        private float _capsuleHeight;
        private float _capsuleRadius;
        private float _capsuleYOffset;
        
        private Vector3 _capsuleBottom;
        private Vector3 _capsuleTop;
        private Vector3 _capsuleCenter;
        private Vector3 _capsuleCenterBottom;
        private Vector3 _capsuleCenterTop;

        private RaycastHit[] _raycastHits = new RaycastHit[16];

        public Vector3 CapsuleBottom => _capsuleBottom;
        public Vector3 CapsuleTop => _capsuleTop;
        public Vector3 CapsuleCenterBottom => _capsuleCenterBottom;
        public Vector3 CapsuleCenterTop => _capsuleCenterTop;
        public Vector3 Center => _capsuleCenter;
        public float Radius => _capsuleRadius;
        public float Height => _capsuleHeight;

        public void Init(MovementComponent movementComponent, bool changeCollider = false)
        {
            _movementComponent = movementComponent;
            capsule = _movementComponent.GetComponent<CapsuleCollider>();
            var capsuleConfig = movementComponent.capsuleConfig;
            ResizeCapsule(capsuleConfig.capsuleRadius, capsuleConfig.capsuleHeight, capsuleConfig.capsuleYOffset, changeCollider);
        }

        public void ResizeCapsule(float radius, float height, float yOffset, bool changeCollider = false)
        {
            height = Mathf.Max(height, radius * 2 + 0.01f);

            if (changeCollider)
            {
                capsule.radius = radius;
                capsule.height = height;
                capsule.center = new Vector3(0, yOffset, 0);
            }

            _capsuleHeight = height;
            _capsuleRadius = radius;
            _capsuleYOffset = yOffset;

            _capsuleCenter = capsule.center;
            _capsuleBottom = _capsuleCenter - Vector3.up * height * 0.5f;
            _capsuleTop = _capsuleCenter + Vector3.up * height * 0.5f;
            _capsuleCenterBottom = _capsuleBottom + Vector3.up * radius;
            _capsuleCenterTop = _capsuleTop - Vector3.up * radius;
        }

        /// <summary>
        /// InitialOverlaps，初始渗透检测，调整位置
        /// </summary>
        public Vector3 PenetrationAdjustment(Vector3 position, Quaternion rotation, Collider[] internalProbedColliders, ref OverlapInfo overlapInfo)
        {
            int count = 0;
            bool solved = false;
            Vector3 startPosition = position;
            Vector3 resolutionDirection = Vector3.up;
            float resolutionDistance = 0f;
            overlapInfo.Reset();
            while (count++ < _movementComponent.maxDecollisionIterations && !solved)
            {
                int nbOverlaps = CollisionOverlap(position, rotation, internalProbedColliders);

                if (nbOverlaps > 0)
                {
                    for (int i = 0; i < nbOverlaps; i++)
                    {
                        var rigidBody = internalProbedColliders[i].GetComponent<Rigidbody>();
                        if (rigidBody == null || rigidBody.isKinematic)
                        {
                            var overlappedTransform = internalProbedColliders[i].transform;
                            if (Physics.ComputePenetration(capsule, position, rotation,
                                    internalProbedColliders[i], overlappedTransform.position, overlappedTransform.rotation,
                                    out resolutionDirection, out resolutionDistance))
                            {
                                var isStable = _movementComponent.IsStableOnNormal(resolutionDirection);
                                resolutionDirection =
                                    _movementComponent.GetObstructionNormal(resolutionDirection, isStable);

                                position += resolutionDirection * (resolutionDistance + DCharacterControllerConst.CollisionOffset);
                                
                                overlapInfo.AddInfo(resolutionDirection, internalProbedColliders[i]);
                            }
                            break;
                        }
                    }
                }
                else
                {
                    solved = true;
                }

            }
            return position - startPosition;
        }

        public virtual int CollisionOverlap(Vector3 position, Quaternion rotation, Collider[] overlappedColliders, float inflate = 0f, bool acceptOnlyStableGroundLayer = false, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            int queryLayers = _movementComponent.collidableLayers;
            if (acceptOnlyStableGroundLayer)
                queryLayers &= _movementComponent.stableGroundLayer;

            var offset = rotation * Vector3.up * inflate;
            var bottom = position + rotation * CapsuleCenterBottom - offset;
            var top = position + rotation * CapsuleCenterTop + offset;

            int nbUnfilteredHits = Physics.OverlapCapsuleNonAlloc(
                bottom, top, _capsuleRadius + inflate, overlappedColliders, queryLayers, queryTriggerInteraction);

            int nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                if (!CheckIfColliderValidForCollisions(overlappedColliders[i]))
                {
                    nbHits--;
                    if (i < nbHits)
                        overlappedColliders[i] = overlappedColliders[nbHits];
                }
            }

            return nbHits;
        }

        public int CollisionSweep(Vector3 startPosition, Vector3 endPosition, Quaternion rotation, RaycastHit[] hits, float inflate = 0f, bool acceptOnlyStableGroundLayer = false, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            int queryLayers = _movementComponent.collidableLayers;
            if (acceptOnlyStableGroundLayer)
                queryLayers &= _movementComponent.stableGroundLayer;
            
            var offset = rotation * Vector3.up * inflate;
            var direction = (endPosition - startPosition).normalized;

            var bottom = startPosition + rotation * CapsuleCenterBottom - offset - direction * DCharacterControllerConst.SweepBackOffset;
            var top = startPosition + rotation * CapsuleCenterTop + offset - direction * DCharacterControllerConst.SweepBackOffset;
            
            Debug.DrawLine(bottom, top, Color.blue);
            Debug.DrawLine(endPosition, startPosition, Color.blue);

            var distance = (endPosition - startPosition).magnitude;

            int nbHits = 0;
            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                bottom, top, _capsuleRadius + inflate,
                direction, hits, distance + DCharacterControllerConst.SweepBackOffset,
                queryLayers, queryTriggerInteraction
            );

            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                var hit = hits[i];

                if (hit.distance <= 0 || !CheckIfColliderValidForCollisions(hit.collider))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
            }
            
            Sort(hits, nbHits);

            return nbHits;
        }

        private void Sort(RaycastHit[] hits, int nbHits)
        {
            for (int i = 0; i < nbHits; i++)
            {
                for (int j = i + 1; j < nbHits; j++)
                {
                    if (hits[i].distance > hits[j].distance)
                    {
                        (hits[j], hits[i]) = (hits[i], hits[j]);
                    }
                }
            }
        }

        public bool CollisionFloorSweep(Vector3 startPosition, Vector3 endPosition, Quaternion rotation,
            ref HitResult hit, bool acceptOnlyStableGroundLayer = false,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            int queryLayers = _movementComponent.collidableLayers;
            if (acceptOnlyStableGroundLayer)
                queryLayers &= _movementComponent.stableGroundLayer;
            
            var direction = (endPosition - startPosition).normalized;

            var bottom = startPosition + rotation * CapsuleCenterBottom - direction * DCharacterControllerConst.SweepBackOffset;
            var top = startPosition + rotation * CapsuleCenterTop - direction * DCharacterControllerConst.SweepBackOffset;

            var distance = (endPosition - startPosition).magnitude;

            int nbHits = 0;
            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                bottom, top, Radius ,
                direction, _raycastHits, distance + DCharacterControllerConst.SweepBackOffset,
                queryLayers, queryTriggerInteraction
            );
            
            float closestDistance = float.MaxValue;
            var index = -1;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                if (CheckIfColliderValidForCollisions(_raycastHits[i].collider))
                {
                    if (_raycastHits[i].distance < closestDistance)
                    {
                        index = i;
                        closestDistance = _raycastHits[i].distance;
                    }
                }
            }

            if (index == -1)
                return false;

            var closestHit = _raycastHits[index];
            closestHit.distance = Mathf.Max(0, closestHit.distance - DCharacterControllerConst.SweepBackOffset);
            hit.isBlockingHit = true;
            hit.isStartPenetrating = closestHit.distance <= 0;
            hit.time = closestHit.distance / distance;
            hit.traceStart = startPosition;
            hit.traceEnd = endPosition;
            hit.hitInfo = closestHit;
            hit.location = startPosition + hit.time * (endPosition - startPosition);

            return true;
        }

        private bool CheckIfColliderValidForCollisions(Collider collider)
        {
            if (collider == capsule || !InternalIsColliderValidForCollisions(collider))
                return false;
            return true;
        }

        private bool InternalIsColliderValidForCollisions(Collider collider)
        {
            var rigidbody = collider.attachedRigidbody;
            if (rigidbody)
            {
                if(!rigidbody.isKinematic)
                    rigidbody.WakeUp();
                return false;
            }

            return true;
        }
    }
}