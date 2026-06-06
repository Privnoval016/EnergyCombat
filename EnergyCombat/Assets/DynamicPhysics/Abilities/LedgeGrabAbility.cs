using System.Collections.Generic;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Auto-activates when the character is airborne and near a climbable ledge.
     * Once active the character hangs briefly then climbs up automatically.
     * All parameters are controlled via <see cref="LedgeGrabSettings"/>.
     * </summary>
     *
     * <remarks>
     * Detection sweeps an arc around the horizontal velocity direction using multiple raycasts,
     * allowing diagonal approach angles. Each candidate direction fires two raycasts:
     * 1. Forward from chest height → finds the wall face.
     * 2. Downward from above the hit point → finds the ledge top.
     * If the top is within <see cref="LedgeGrabSettings.MaxLedgeReach"/> and above
     * <see cref="LedgeGrabSettings.MinLedgeHeight"/>, the grab activates.
     *
     * Climb phases:
     * - Phase 1 (Grab): character holds still for <see cref="LedgeGrabSettings.GrabHoldTime"/>.
     * - Phase 2 (Climb): upward then forward velocity lifts the character onto the ledge.
     * On completion, a fraction of the entry speed is restored in the original approach direction.
     * When the character becomes grounded the ability self-deactivates.
     * </remarks>
     */
    public class LedgeGrabAbility : IMotionAbility
    {
        private readonly LedgeGrabSettings _settings;

        public bool IsActive { get; private set; }
        public bool IsInClimbPhase => IsActive && _climbPhase == ClimbPhase.Climb;
        private float _phase1Timer;
        private float _phase2Timer;
        private Vector3 _climbForward;
        private Vector3 _entryVelocity;
        private float _computedClimbUpSpeed;
        private float _computedClimbForwardSpeed;

        private enum ClimbPhase { Grab, Climb }
        private ClimbPhase _climbPhase;

        public LedgeGrabAbility(LedgeGrabSettings settings)
        {
            _settings = settings;
        }

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            if (!context.HasTag(MotionTag.Airborne)) return false;
            if (context.HasTag(MotionTag.Dashing) || context.HasTag(MotionTag.WallRunning)) return false;
            if (context.Velocity.y > _settings.MaxEntryUpwardVelocity) return false;
            if (context.CharacterTransform == null) return false;

            return DetectLedge(context, out _, out _, out _);
        }

        public void Activate(MotionContext context)
        {
            if (!DetectLedge(context, out Vector3 ledgeTop, out Vector3 wallForward, out float wallHitDistance))
                return;

            IsActive = true;
            _climbPhase = ClimbPhase.Grab;
            _phase1Timer = _settings.GrabHoldTime;
            _phase2Timer = _settings.ClimbDuration;
            _climbForward = wallForward;
            _entryVelocity = new Vector3(context.Velocity.x, 0f, context.Velocity.z);

            float halfDuration = _settings.ClimbDuration * 0.5f;
            float deltaY = ledgeTop.y + _settings.ClimbClearanceAbove - context.Position.y;
            _computedClimbUpSpeed = Mathf.Max(deltaY / halfDuration, _settings.MinClimbUpSpeed);
            float deltaX = wallHitDistance + _settings.ClimbClearanceForward;
            _computedClimbForwardSpeed = Mathf.Max(deltaX / halfDuration, _settings.MinClimbForwardSpeed);

            context.SetTag(MotionTag.LedgeGrabbing);
            context.RemoveTag(MotionTag.Airborne);
            context.Velocity = Vector3.zero;
            context.GravityScale = 0f;
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            if (!IsActive) return;

            if (context.HasTag(MotionTag.Grounded))
            {
                Deactivate(context);
                return;
            }

            context.GravityScale = 0f;

            if (_climbPhase == ClimbPhase.Grab)
            {
                _phase1Timer -= deltaTime;
                if (_phase1Timer <= 0f)
                    _climbPhase = ClimbPhase.Climb;
                return;
            }

            _phase2Timer -= deltaTime;
            if (_phase2Timer <= 0f)
            {
                if (_entryVelocity.sqrMagnitude > 0.01f)
                {
                    float speed = _entryVelocity.magnitude * _settings.MomentumPreservationRatio;
                    context.Velocity = _entryVelocity.normalized * speed;
                }
                Deactivate(context);
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context)
        {
            if (!IsActive) return Vector3.zero;

            // AbilityInfluenceStage does context.Velocity += influence (no deltaTime).
            // Returning (target - current) sets velocity exactly to target in one step.
            if (_climbPhase == ClimbPhase.Grab)
                return -context.Velocity; // hold perfectly still during the hang

            float progress = 1f - Mathf.Clamp01(_phase2Timer / _settings.ClimbDuration);

            Vector3 targetVelocity = progress < 0.5f
                ? Vector3.up * _computedClimbUpSpeed
                : Vector3.up * (_computedClimbUpSpeed * 0.3f) + _climbForward * _computedClimbForwardSpeed;

            return targetVelocity - context.Velocity;
        }

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.LedgeGrabbing);
            if (!context.HasTag(MotionTag.Grounded))
                context.SetTag(MotionTag.Airborne);
        }

        private bool DetectLedge(MotionContext context, out Vector3 ledgeTop, out Vector3 wallForward, out float wallHitDistance)
        {
            ledgeTop = Vector3.zero;
            wallForward = Vector3.zero;
            wallHitDistance = 0f;

            Transform t = context.CharacterTransform;
            if (t == null) return false;

            Vector3 chestPos = context.Position + Vector3.up * _settings.DetectionChestHeight;

            Vector3 hVel = new Vector3(context.Velocity.x, 0f, context.Velocity.z);
            Vector3 baseForward = hVel.sqrMagnitude > 0.25f ? hVel.normalized : t.forward;

            float halfAngle = _settings.WallSearchArcAngle * 0.5f;
            const int Steps = 5;

            for (int i = 0; i < Steps; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / (Steps - 1));
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * baseForward;

                if (TryRaycastLedge(context, chestPos, dir, out ledgeTop, out wallForward, out wallHitDistance))
                    return true;
            }

            return false;
        }

        private bool TryRaycastLedge(MotionContext context, Vector3 chestPos, Vector3 forward,
            out Vector3 ledgeTop, out Vector3 wallForward, out float wallHitDistance)
        {
            ledgeTop = Vector3.zero;
            wallForward = Vector3.zero;
            wallHitDistance = 0f;

            if (!Physics.Raycast(chestPos, forward, out RaycastHit wallHit,
                    _settings.DetectionDistance, _settings.LedgeLayers, QueryTriggerInteraction.Ignore))
                return false;

            Vector3 aboveHit = wallHit.point + Vector3.up * 0.5f + forward * 0.1f;
            if (!Physics.Raycast(aboveHit, Vector3.down, out RaycastHit topHit,
                    1f, _settings.LedgeLayers, QueryTriggerInteraction.Ignore))
                return false;

            float heightDiff = topHit.point.y - context.Position.y;
            if (heightDiff < _settings.MinLedgeHeight || heightDiff > _settings.MaxLedgeReach)
                return false;

            ledgeTop = topHit.point;
            wallHitDistance = wallHit.distance;
            wallForward = -wallHit.normal;
            wallForward.y = 0f;
            wallForward.Normalize();
            return true;
        }
    }
}
