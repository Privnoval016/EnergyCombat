using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Wall run ability that detects walls via raycasts and applies wall-running physics.
     * Auto-activates when airborne near a wall with sufficient speed.
     * Intercepts Jump requests to perform wall jumps.
     * </summary>
     */
    public class WallRunAbility : IMotionAbility
    {
        #region Configuration

        public float WallDetectionDistance { get; set; }
        public LayerMask WallLayers { get; set; }
        public float MaxDuration { get; set; }
        public float WallRunSpeed { get; set; }
        public float WallRunGravityScale { get; set; }
        public float WallStickForce { get; set; }
        public float MinEntrySpeed { get; set; }
        public float MinWallAngle { get; set; }
        public float WallJumpUpForce { get; set; }
        public float WallJumpOutForce { get; set; }

        #endregion

        #region State

        public bool IsActive { get; private set; }
        private float _wallRunTimer;
        private Vector3 _wallNormal;
        private Vector3 _wallForward;

        #endregion

        public WallRunAbility(float wallDetectionDistance = 0.7f, float maxDuration = 1.2f,
            float wallRunSpeed = 10f, float wallRunGravityScale = 0.15f,
            float wallStickForce = 8f, float minEntrySpeed = 4f,
            float minWallAngle = 60f, float wallJumpUpForce = 8f,
            float wallJumpOutForce = 6f)
        {
            WallDetectionDistance = wallDetectionDistance;
            WallLayers = ~0;
            MaxDuration = maxDuration;
            WallRunSpeed = wallRunSpeed;
            WallRunGravityScale = wallRunGravityScale;
            WallStickForce = wallStickForce;
            MinEntrySpeed = minEntrySpeed;
            MinWallAngle = minWallAngle;
            WallJumpUpForce = wallJumpUpForce;
            WallJumpOutForce = wallJumpOutForce;
        }

        /**
         * <summary>
         * Intercepts Jump requests during wall run to perform a wall jump.
         * </summary>
         */
        public bool TryConsumeRequest(MotionContext context, MotionRequest request)
        {
            if (request.Type == MotionRequestType.Jump)
            {
                WallJump(context);
                return true;
            }
            return false;
        }

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            if (!context.HasTag(MotionTag.Airborne)) return false;

            Vector3 hVel = context.Velocity;
            hVel.y = 0f;
            if (hVel.sqrMagnitude < MinEntrySpeed * MinEntrySpeed) return false;

            if (context.HasTag(MotionTag.Dashing) || context.HasTag(MotionTag.Swinging)) return false;
            if (context.CharacterTransform == null) return false;

            return DetectWall(context);
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _wallRunTimer = MaxDuration;

            context.SetTag(MotionTag.WallRunning);
            context.SetTag(MotionTag.WallContact);
            context.RemoveTag(MotionTag.Airborne);
            context.Velocity.y = 0f;
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _wallRunTimer -= deltaTime;

            if (!DetectWall(context) || _wallRunTimer <= 0f)
            {
                Deactivate(context);
                return;
            }

            context.GravityScale *= WallRunGravityScale;
            float progressRatio = 1f - (_wallRunTimer / MaxDuration);
            context.Velocity.y -= progressRatio * 5f * deltaTime;

            if (context.HasTag(MotionTag.Grounded))
            {
                Deactivate(context);
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context)
        {
            if (!IsActive) return Vector3.zero;

            Vector3 desiredVel = _wallForward * WallRunSpeed;
            Vector3 currentHorizontal = context.Velocity;
            currentHorizontal.y = 0f;

            Vector3 influence = (desiredVel - currentHorizontal) * 0.3f;
            influence -= _wallNormal * WallStickForce * context.DeltaTime;

            return influence;
        }

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.WallRunning);
            context.RemoveTag(MotionTag.WallContact);
            if (!context.HasTag(MotionTag.Grounded))
            {
                context.SetTag(MotionTag.Airborne);
            }
        }

        /**
         * <summary>
         * Applies wall jump forces and deactivates the wall run.
         * </summary>
         */
        public void WallJump(MotionContext context)
        {
            if (!IsActive) return;
            context.Velocity.y = WallJumpUpForce;
            context.Velocity += _wallNormal * WallJumpOutForce;
            Deactivate(context);
        }

        #region Wall Detection

        private bool DetectWall(MotionContext context)
        {
            Transform t = context.CharacterTransform;
            Vector3 pos = context.Position + Vector3.up * 0.5f;

            Vector3 forward = context.Velocity;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.1f)
            {
                forward = t.forward;
            }
            else
            {
                forward *= 1f / Mathf.Sqrt(forward.sqrMagnitude);
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            if (Physics.Raycast(pos, right, out RaycastHit hitRight, WallDetectionDistance, WallLayers, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(-hitRight.normal, forward);
                if (angle >= MinWallAngle)
                {
                    _wallNormal = hitRight.normal;
                    _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;
                    if (Vector3.Dot(_wallForward, forward) < 0f) _wallForward = -_wallForward;
                    return true;
                }
            }

            if (Physics.Raycast(pos, -right, out RaycastHit hitLeft, WallDetectionDistance, WallLayers, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(-hitLeft.normal, forward);
                if (angle >= MinWallAngle)
                {
                    _wallNormal = hitLeft.normal;
                    _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;
                    if (Vector3.Dot(_wallForward, forward) < 0f) _wallForward = -_wallForward;
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
