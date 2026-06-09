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
            if (Time.time - _lastDeactivationTime < _settings.ReactivationCooldown) return false;

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
                Vector2 moveInput = context.Input.MoveInput;
                if (moveInput.sqrMagnitude < 0.01f)
                {
                    Deactivate(context);
                    return;
                }

                Vector3 worldMove = context.Input.CameraForward * moveInput.y
                                  + context.Input.CameraRight * moveInput.x;
                worldMove.y = 0f;
                if (worldMove.sqrMagnitude > 0.001f)
                {
                    worldMove.Normalize();
                    if (Vector3.Angle(worldMove, _wallForward) > _settings.InputSustainAngleThreshold)
                    {
                        Deactivate(context);
                        return;
                    }
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

            if (Physics.Raycast(pos, right, out RaycastHit hitRight,
                    _settings.WallDetectionDistance, _settings.WallLayers, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(-hitRight.normal, forward) >= _settings.MinWallAngle)
                {
                    _wallNormal = hitRight.normal;
                    _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;
                    if (Vector3.Dot(_wallForward, forward) < 0f) _wallForward = -_wallForward;
                    return true;
                }
            }

            if (Physics.Raycast(pos, -right, out RaycastHit hitLeft,
                    _settings.WallDetectionDistance, _settings.WallLayers, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(-hitLeft.normal, forward) >= _settings.MinWallAngle)
                {
                    _wallNormal = hitLeft.normal;
                    _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;
                    if (Vector3.Dot(_wallForward, forward) < 0f) _wallForward = -_wallForward;
                    return true;
                }
            }

            return false;
        }
    }
}
