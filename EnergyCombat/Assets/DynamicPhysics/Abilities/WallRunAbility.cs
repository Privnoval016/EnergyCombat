using System.Collections.Generic;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Wall run ability that detects walls via raycasts and applies wall-running physics.
     * Auto-activates when airborne near a wall with sufficient speed.
     * Intercepts Jump requests to perform wall jumps.
     * All parameters are controlled via <see cref="WallRunSettings"/>.
     * </summary>
     */
    public class WallRunAbility : IMotionAbility
    {
        private readonly WallRunSettings _settings;

        public bool IsActive { get; private set; }
        private float _wallRunTimer;
        private Vector3 _wallNormal;
        private Vector3 _wallForward;
        private float _lastDeactivationTime = float.NegativeInfinity;

        public WallRunAbility(WallRunSettings settings)
        {
            _settings = settings;
        }

        /**
         * <summary>
         * On a Jump request during wall run, ends the wall run without consuming the request.
         * Sets <see cref="MotionTag.WallRunJump"/> for one tick so <see cref="WallKickAbility"/>
         * can identify the activation as a wall-run exit and play the correct animation.
         * WallKickAbility (registered after this) handles the kick impulse uniformly.
         * </summary>
         */
        public bool TryConsumeRequest(MotionContext context, MotionRequest request)
        {
            if (!IsActive) return false;
            if (request.Type != MotionRequestType.Jump) return false;
            Deactivate(context);
            context.SetTag(MotionTag.WallRunJump); // read and cleared by WallKickAbility.CanActivate
            return false; // pass the request through so WallKickAbility can handle it
        }

        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            if (!context.HasTag(MotionTag.Airborne)) return false;
            // Skip reactivation cooldown during a wall kick so the player can chain directly
            // from one wall to another without waiting. The approaching-wall check in DetectWall
            // prevents re-latching to the wall just kicked from (player is moving away from it).
            bool isWallKicking = context.HasTag(MotionTag.WallKicking);
            if (!isWallKicking && Time.time - _lastDeactivationTime < _settings.ReactivationCooldown) return false;

            Vector3 hVel = context.Velocity;
            hVel.y = 0f;
            if (hVel.sqrMagnitude < _settings.MinEntrySpeed * _settings.MinEntrySpeed) return false;

            if (context.HasTag(MotionTag.Dashing) || context.HasTag(MotionTag.Swinging)) return false;
            if (context.CharacterTransform == null) return false;

            return DetectWall(context);
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _wallRunTimer = _settings.MaxDuration;

            context.SetTag(MotionTag.WallRunning);
            context.SetTag(MotionTag.WallContact);
            context.SetTag(MotionTag.NoAutoRotate);
            context.RemoveTag(MotionTag.Airborne);
            context.WallNormal = _wallNormal;
            context.Velocity = new Vector3(context.Velocity.x, 0f, context.Velocity.z);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _wallRunTimer -= deltaTime;

            if (!DetectWall(context) || _wallRunTimer <= 0f)
            {
                Deactivate(context);
                return;
            }

            context.WallNormal = _wallNormal;

            if (_settings.RequireInputToSustain)
            {
                // End the wall run if the player releases all directional input.
                // The directional angle check is intentionally omitted: after a wall-to-wall
                // kick the player's input direction is perpendicular to the new wall, and
                // GetVelocityInfluence already steers velocity along it — no need to punish
                // the approach angle here.
                if (context.Input.MoveInput.sqrMagnitude < 0.01f)
                {
                    Deactivate(context);
                    return;
                }
            }

            context.GravityScale *= _settings.WallRunGravityScale;
            float progressRatio = 1f - (_wallRunTimer / _settings.MaxDuration);
            context.Velocity.y -= progressRatio * 5f * deltaTime;

            if (context.HasTag(MotionTag.Grounded))
                Deactivate(context);
        }

        public Vector3 GetVelocityInfluence(MotionContext context)
        {
            if (!IsActive) return Vector3.zero;

            context.DesiredFacingDirection = _wallForward;

            Vector3 desiredVel = _wallForward * _settings.WallRunSpeed;
            Vector3 currentHorizontal = new Vector3(context.Velocity.x, 0f, context.Velocity.z);

            Vector3 influence = (desiredVel - currentHorizontal) * 0.3f;
            influence -= _wallNormal * _settings.WallStickForce * context.DeltaTime;

            return influence;
        }

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            _lastDeactivationTime = Time.time;
            context.RemoveTag(MotionTag.WallRunning);
            context.RemoveTag(MotionTag.WallContact);
            context.RemoveTag(MotionTag.NoAutoRotate);
            context.WallNormal = Vector3.zero;
            if (!context.HasTag(MotionTag.Grounded))
                context.SetTag(MotionTag.Airborne);
        }

        private bool DetectWall(MotionContext context)
        {
            Transform t = context.CharacterTransform;
            Vector3 pos = context.Position + Vector3.up * 0.5f;

            Vector3 forward = new Vector3(context.Velocity.x, 0f, context.Velocity.z);
            if (forward.sqrMagnitude < 0.1f)
                forward = t.forward;
            else
                forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // Side rays: standard wall-run detection for walls running alongside the player.
            // MinWallAngle filters out walls the player is running directly into (e.g. pillar ends).
            if (Physics.Raycast(pos, right, out RaycastHit hitRight,
                    _settings.WallDetectionDistance, _settings.WallLayers, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(-hitRight.normal, forward) >= _settings.MinWallAngle)
                    return SetWall(hitRight.normal, forward, context);
            }

            if (Physics.Raycast(pos, -right, out RaycastHit hitLeft,
                    _settings.WallDetectionDistance, _settings.WallLayers, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(-hitLeft.normal, forward) >= _settings.MinWallAngle)
                    return SetWall(hitLeft.normal, forward, context);
            }

            // Forward and diagonal rays: detect walls the player is moving toward, e.g. the
            // target wall during a wall-to-wall kick where the approach is nearly perpendicular.
            // Uses the same 5-direction sweep as WallKickAbility.DetectKickWall.
            Vector3[] forwardDirs = { forward, (forward + right).normalized, (forward - right).normalized };
            foreach (var dir in forwardDirs)
            {
                if (Physics.Raycast(pos, dir, out RaycastHit hit,
                        _settings.WallDetectionDistance, _settings.WallLayers, QueryTriggerInteraction.Ignore))
                {
                    if (Mathf.Abs(hit.normal.y) < 0.5f)
                        return SetWall(hit.normal, forward, context);
                }
            }

            return false;
        }

        /**
         * <summary>
         * Stores the detected wall normal and computes <see cref="_wallForward"/>.
         * When velocity is nearly perpendicular to the wall (head-on or wall-kick approach) the
         * player's input direction is used instead so the run starts in the intended direction.
         * </summary>
         */
        private bool SetWall(Vector3 normal, Vector3 forward, MotionContext context)
        {
            _wallNormal  = normal;
            _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;

            float velDot = Vector3.Dot(_wallForward, forward);
            if (Mathf.Abs(velDot) >= 0.1f)
            {
                if (velDot < 0f) _wallForward = -_wallForward;
            }
            else
            {
                // Head-on approach: velocity is perpendicular to the wall, so the dot product
                // does not determine direction. Use the player's input direction instead.
                Vector2 moveInput = context.Input.MoveInput;
                Vector3 worldInput = context.Input.CameraForward * moveInput.y
                                   + context.Input.CameraRight  * moveInput.x;
                worldInput.y = 0f;
                if (worldInput.sqrMagnitude > 0.01f && Vector3.Dot(_wallForward, worldInput) < 0f)
                    _wallForward = -_wallForward;
            }

            return true;
        }
    }
}
