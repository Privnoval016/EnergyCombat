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
     * so KickHeight is a true world-space metre value matching JumpHeight. All pre-kick
     * horizontal velocity is stripped so the outward impulse is clean and perpendicular to the
     * wall — this enables reliable wall-to-wall bouncing regardless of approach speed.
     *
     * Every kick uses the same height and outward speed regardless of which wall is kicked or
     * how many times the same wall has been kicked — consistent feel at all times.
     *
     * <see cref="KickSign"/> is set at activation time (+1 = wall to the right, -1 = left).
     * <see cref="IsWallRunExit"/> is set when the kick was triggered by jumping out of a wall run,
     * so the animation layer can choose the wall-run-exit clip instead of the standalone-kick clip.
     * Both are read by the state constructor for animation selection.
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

        /** <summary>+1 if the kicked wall was to the character's right, -1 if left. Set at activation time.</summary> */
        public float KickSign { get; private set; }

        /** <summary>True if this kick was triggered by jumping out of a wall run; false for standalone kicks.</summary> */
        public bool IsWallRunExit { get; private set; }

        // Time at which the most recent wall-run-exit jump signal was detected.
        // A kick is treated as a wall-run exit if it fires within the cooldown window of this time.
        private float _wallRunExitTime = float.NegativeInfinity;
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

            // Record the time of the wall-run-exit signal and clear the tag.
            // Using a timestamp (not a bool) means the signal survives multiple CanActivate calls —
            // if cooldown delays the kick by a tick or two, IsWallRunExit is still set correctly.
            if (context.HasTag(MotionTag.WallRunJump))
            {
                _wallRunExitTime = Time.time;
                context.RemoveTag(MotionTag.WallRunJump);
            }

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
            // Use effective gravity (Physics.gravity × profile scale) so KickHeight is a
            // true world-space metre value — same convention as JumpAbility.
            float g = Mathf.Abs(Physics.gravity.y) * _profile.GravityScale;
            float effectiveHeight = Mathf.Max(_settings.KickHeight, 0.001f);
            float vy = Mathf.Sqrt(2f * g * effectiveHeight);

            // tApex from the full arc so KickOutDistance is always honoured.
            float tApex = Mathf.Sqrt(2f * effectiveHeight / g);
            float vOut  = _settings.KickOutDistance / tApex;

            // Wall is to the right when the direction toward it (−normal) aligns with character right.
            Vector3 charRight = Vector3.Cross(Vector3.up, context.CharacterTransform.forward);
            KickSign = Mathf.Sign(Vector3.Dot(charRight, -_kickWallNormal));

            // Wall-run exit if the signal arrived within the cooldown window of this activation.
            IsWallRunExit = Time.time - _wallRunExitTime <= _settings.KickCooldown + 0.1f;

            // Strip all pre-kick horizontal velocity — clean perpendicular impulse for wall-to-wall bouncing.
            context.Velocity = new Vector3(
                _kickWallNormal.x * vOut,
                vy,
                _kickWallNormal.z * vOut
            );

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
