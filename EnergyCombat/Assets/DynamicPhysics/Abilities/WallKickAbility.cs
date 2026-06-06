using System.Collections.Generic;
using Player.Config;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Allows the character to kick off any wall while airborne.
     * Activated by a Jump request near a wall — does NOT auto-activate.
     *
     * Handles both standalone kicks (from JumpState/FallState) and wall-run exits:
     * WallRunAbility.TryConsumeRequest calls Deactivate() and returns false on a Jump
     * request, so this ability receives the unconsumed request and fires uniformly.
     *
     * Impulse calculation uses the correct effective gravity (Physics.gravity.y × GravityScale)
     * so KickHeight is a true world-space metre value matching JumpHeight.
     *
     * Arc management: the WallKicking tag and IsActive live for the full upward arc
     * (until Vy ≤ 0 or grounded). This means:
     * - JumpAbility.Tick skips for the whole arc (already guards on WallKicking tag) — no interference.
     * - Apex hang and variable-height cut are owned here, sharing JumpSettings parameters
     *   for a consistent feel with normal jumps.
     * - The state machine stays in WallKickState for the entire ascent, then exits to FallState.
     * </summary>
     */
    public class WallKickAbility : IMotionAbility
    {
        private readonly WallKickSettings _settings;
        private readonly JumpSettings _jumpSettings;
        private readonly MovementProfile _profile;

        public bool IsActive { get; private set; }
        private Vector3 _lastKickNormal;
        private bool _hasKicked;
        private float _lastKickTime;
        private Vector3 _kickWallNormal;
        private float _lastGroundedTime = float.NegativeInfinity;
        private bool _cutApplied;

        public WallKickAbility(WallKickSettings settings, JumpSettings jumpSettings, MovementProfile profile)
        {
            _settings = settings;
            _jumpSettings = jumpSettings;
            _profile = profile;
        }

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            // Track grounded time so MinAirborneTime blocks co-activation with normal jumps.
            // Updated only when the Grounded tag is live (not during wall run or other airborne states).
            if (context.HasTag(MotionTag.Grounded))
                _lastGroundedTime = Time.time;

            if (!context.HasTag(MotionTag.Airborne)) return false;
            if (context.HasTag(MotionTag.Dashing)) return false;
            if (Time.time - _lastKickTime < _settings.KickCooldown) return false;

            // Prevent wall kick from firing in the same tick as a grounded jump.
            // JumpAbility removes the Grounded tag on activation, so _lastGroundedTime was
            // set one tick earlier — this gap is always < MinAirborneTime.
            if (Time.time - _lastGroundedTime < _settings.MinAirborneTime) return false;

            if (!HasJumpRequest(requests)) return false;
            return DetectKickWall(context);
        }

        public void Activate(MotionContext context)
        {
            bool isSameWall = _hasKicked &&
                Vector3.Dot(_kickWallNormal, _lastKickNormal) >
                Mathf.Cos(_settings.SameWallAngleThreshold * Mathf.Deg2Rad);

            float effectiveHeight = isSameWall
                ? _settings.KickHeight * _settings.SameWallHeightScale
                : _settings.KickHeight;

            // Use effective gravity (Physics.gravity × profile scale) so KickHeight is a
            // true world-space metre value — same convention as JumpAbility.
            float g = Mathf.Abs(Physics.gravity.y) * _profile.GravityScale;
            float vy, vOut;

            if (effectiveHeight >= 0f)
            {
                vy = Mathf.Sqrt(2f * g * Mathf.Max(effectiveHeight, 0.001f));
                float tApex = vy / g;
                vOut = _settings.KickOutDistance / tApex;
            }
            else
            {
                vy = -Mathf.Sqrt(2f * g * Mathf.Abs(effectiveHeight));
                float tLand = Mathf.Sqrt(2f * Mathf.Abs(effectiveHeight) / g);
                vOut = _settings.KickOutDistance / Mathf.Max(tLand, 0.1f);
            }

            // Preserve velocity running along the wall, discard any into-wall component,
            // then apply the outward kick and vertical impulse.
            Vector3 hVel = new Vector3(context.Velocity.x, 0f, context.Velocity.z);
            Vector3 wallParallel = hVel - _kickWallNormal * Vector3.Dot(hVel, _kickWallNormal);
            context.Velocity = wallParallel + _kickWallNormal * vOut;
            context.Velocity.y = vy;

            _lastKickNormal = _kickWallNormal;
            _hasKicked = true;
            _lastKickTime = Time.time;
            _cutApplied = false;
            IsActive = true;

            context.SetTag(MotionTag.WallKicking);
            context.SetTag(MotionTag.NoAutoRotate);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            // End the arc as soon as vertical velocity turns negative or we land.
            // Tag lives for the whole ascent — no need for a 1-tick sentinel.
            if (context.HasTag(MotionTag.Grounded) || context.Velocity.y <= 0f)
            {
                Deactivate(context);
                return;
            }

            // Variable-height cut: same multiplier as normal jumps for consistent feel.
            if (!context.Input.JumpHeld && !_cutApplied)
            {
                context.Velocity.y *= _jumpSettings.JumpCutMultiplier;
                _cutApplied = true;
            }

            // Apex hang: reduce gravity near the top — same params as normal jump.
            if (Mathf.Abs(context.Velocity.y) < _jumpSettings.ApexThreshold)
                context.GravityScale *= _jumpSettings.ApexGravityMultiplier;
        }

        public Vector3 GetVelocityInfluence(MotionContext context)
        {
            if (!IsActive) return Vector3.zero;
            context.DesiredFacingDirection = _kickWallNormal;
            return Vector3.zero;
        }

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.WallKicking);
            context.RemoveTag(MotionTag.NoAutoRotate);
        }

        private bool HasJumpRequest(List<MotionRequest> requests)
        {
            foreach (var r in requests)
                if (r.Type == MotionRequestType.Jump) return true;
            return false;
        }

        private bool DetectKickWall(MotionContext context)
        {
            Transform t = context.CharacterTransform;
            if (t == null) return false;

            Vector3 pos = context.Position + Vector3.up * 0.5f;
            Vector3 hVel = new Vector3(context.Velocity.x, 0f, context.Velocity.z);
            Vector3 forward = hVel.sqrMagnitude > 0.1f ? hVel.normalized : t.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // Sweep 5 directions so head-on walls and diagonal approaches are both detected.
            Vector3[] dirs = {
                right,
                -right,
                forward,
                (forward + right).normalized,
                (forward - right).normalized,
            };

            foreach (var dir in dirs)
            {
                if (Physics.Raycast(pos, dir, out RaycastHit hit,
                        _settings.WallDetectionDistance, _settings.WallLayers, QueryTriggerInteraction.Ignore))
                {
                    // Accept roughly-vertical surfaces only — avoids kicking floors/ceilings.
                    if (Mathf.Abs(hit.normal.y) < 0.5f)
                    {
                        _kickWallNormal = hit.normal;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
