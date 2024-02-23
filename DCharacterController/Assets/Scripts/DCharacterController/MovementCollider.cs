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
        
        private float capsuleHeight;
        private float capsuleRadius;
        private float capsuleYOffset;
        
        private Vector3 _capsuleBottom;
        private Vector3 _capsuleTop;
        private Vector3 _capsuleCenter;
        private Vector3 _capsuleCenterBottom;
        private Vector3 _capsuleCenterTop;

        public void Init(MovementComponent movementComponent)
        {
            _movementComponent = movementComponent;
            capsule = _movementComponent.GetComponent<CapsuleCollider>();
            var capsuleConfig = movementComponent.capsuleConfig;
            ResizeCapsule(capsuleConfig.capsuleRadius, capsuleConfig.capsuleHeight, capsuleConfig.capsuleYOffset);
        }

        public void ResizeCapsule(float radius, float height, float yOffset)
        {
            height = Mathf.Max(height, radius * 2 + 0.01f);

            capsule.radius = radius;
            capsule.height = height;
            capsule.center = new Vector3(0, yOffset, 0);

            capsuleHeight = height;
            capsuleRadius = radius;
            capsuleYOffset = yOffset;
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

        protected virtual int CollisionOverlap(Vector3 position, Quaternion rotation, Collider[] overlappedColliders, float inflate = 0f, bool acceptOnlyStableGroundLayer = false, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            int queryLayers = _movementComponent.collidableLayers;
            if (acceptOnlyStableGroundLayer)
                queryLayers &= _movementComponent.stableGroundLayer;

            var offset = rotation * Vector3.up * (capsuleHeight * 0.5f - capsuleRadius + inflate);
            var bottom = position - offset;
            var top = position + offset;

            int nbUnfilteredHits = Physics.OverlapCapsuleNonAlloc(
                bottom, top, capsuleRadius + inflate, overlappedColliders, queryLayers, queryTriggerInteraction);

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

        public int CollisionSweep(Vector3 startPosition, Vector3 endPosition, Quaternion rotation,  out RaycastHit closestHit, RaycastHit[] hits, float inflate = 0f, bool acceptOnlyStableGroundLayer = false, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            int queryLayers = _movementComponent.collidableLayers;
            if (acceptOnlyStableGroundLayer)
                queryLayers &= _movementComponent.stableGroundLayer;
            
            var offset = rotation * Vector3.up * (capsuleHeight * 0.5f - capsuleRadius + inflate);
            var bottom = startPosition - offset;
            var top = startPosition + offset;

            var direction = (endPosition - startPosition).normalized;
            var distance = (endPosition - startPosition).magnitude;

            int nbHits = 0;
            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                bottom, top, capsuleRadius + inflate,
                direction, hits, distance,
                queryLayers, queryTriggerInteraction
            );

            closestHit = new RaycastHit();
            float closestDistance = Mathf.Infinity;
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
                else
                {
                    if (hit.distance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hit.distance;
                    }
                }
            }

            return nbHits;
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