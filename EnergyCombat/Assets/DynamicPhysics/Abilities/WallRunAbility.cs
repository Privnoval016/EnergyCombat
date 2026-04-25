using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Abilities
{
    /**
     * <summary>
     * Wall run ability that detects walls via raycasts and applies wall-running physics.
     * Auto-activates when the character is airborne, near a wall, and moving forward.
     * </summary>
     *
     * <remarks>
     * Wall detection uses two horizontal raycasts (left and right) relative to the
     * character's velocity direction. When conditions are met, the ability:
     * <list type="bullet">
     *   <item>Reduces gravity for sustained wall-running.</item>
     *   <item>Projects movement along the wall surface.</item>
     *   <item>Applies a wall-sticking force to maintain contact.</item>
     *   <item>Sets <see cref="MotionFlags.WallRunning"/> to reduce steering input.</item>
     * </list>
     * Exit conditions: max duration exceeded, no wall contact, too slow, or grounded.
     * </remarks>
     */
    public class WallRunAbility : IMotionAbility
    {
        #region Configuration

        /** <summary>Maximum distance to detect a wall from the character center.</summary> */
        public float WallDetectionDistance { get; set; }

        /** <summary>Layer mask for surfaces that can be wall-run on.</summary> */
        public LayerMask WallLayers { get; set; }

        /** <summary>Maximum duration of a single wall run in seconds.</summary> */
        public float MaxDuration { get; set; }

        /** <summary>Speed maintained during wall running.</summary> */
        public float WallRunSpeed { get; set; }

        /** <summary>Gravity multiplier during wall run (low = longer runs).</summary> */
        public float WallRunGravityScale { get; set; }

        /** <summary>Force pushing character toward the wall to maintain contact.</summary> */
        public float WallStickForce { get; set; }

        /** <summary>Minimum horizontal speed required to initiate a wall run.</summary> */
        public float MinEntrySpeed { get; set; }

        /** <summary>Minimum angle between character velocity and wall normal for valid wall running (degrees).</summary> */
        public float MinWallAngle { get; set; }

        /** <summary>Upward velocity applied when jumping off a wall.</summary> */
        public float WallJumpUpForce { get; set; }

        /** <summary>Outward velocity applied when jumping off a wall (away from wall).</summary> */
        public float WallJumpOutForce { get; set; }

        #endregion

        #region State

        public bool IsActive { get; private set; }

        private float _wallRunTimer;
        private Vector3 _wallNormal;
        private Vector3 _wallForward;
        private int _wallSide; // -1 = left, 1 = right

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

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            // Must be airborne
            if ((context.Flags & MotionFlags.Airborne) == 0) return false;

            // Must have enough horizontal speed
            Vector3 hVel = context.Velocity;
            hVel.y = 0f;
            if (hVel.sqrMagnitude < MinEntrySpeed * MinEntrySpeed) return false;

            // Must not be dashing or swinging
            if ((context.Flags & (MotionFlags.Dashing | MotionFlags.Swinging)) != 0) return false;

            if (context.CharacterTransform == null) return false;

            // Detect wall
            return DetectWall(context);
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _wallRunTimer = MaxDuration;

            context.Flags |= MotionFlags.WallRunning | MotionFlags.WallContact;
            context.Flags &= ~MotionFlags.Airborne;

            // Zero out vertical velocity on entry for clean wall run start
            context.Velocity.y = 0f;
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _wallRunTimer -= deltaTime;

            // Re-detect wall each frame
            if (!DetectWall(context) || _wallRunTimer <= 0f)
            {
                Deactivate(context);
                return;
            }

            // Reduce gravity
            context.GravityScale *= WallRunGravityScale;

            // Apply gradual downward pull as wall run progresses (losing grip)
            float progressRatio = 1f - (_wallRunTimer / MaxDuration);
            context.Velocity.y -= progressRatio * 5f * deltaTime;

            // Check if grounded (end wall run)
            if ((context.Flags & MotionFlags.Grounded) != 0)
            {
                Deactivate(context);
                return;
            }

            // Check for jump request (wall jump)
            // The jump ability will handle this if registered, but we need to detach from wall
            // Wall jump is handled by providing velocity influence when deactivating via jump
        }

        public Vector3 GetVelocityInfluence(MotionContext context)
        {
            if (!IsActive) return Vector3.zero;

            // Project velocity along wall forward direction at wall run speed
            Vector3 desiredVel = _wallForward * WallRunSpeed;
            Vector3 currentHorizontal = context.Velocity;
            currentHorizontal.y = 0f;

            // Blend toward wall-run velocity
            Vector3 influence = (desiredVel - currentHorizontal) * 0.3f;

            // Add wall-sticking force (toward wall)
            influence -= _wallNormal * WallStickForce * context.DeltaTime;

            return influence;
        }

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.Flags &= ~(MotionFlags.WallRunning | MotionFlags.WallContact);
            if ((context.Flags & MotionFlags.Grounded) == 0)
            {
                context.Flags |= MotionFlags.Airborne;
            }
        }

        /**
         * <summary>
         * Requests a wall jump, applying upward and outward forces relative to the wall.
         * Should be called by the orchestrator when a jump request occurs during wall running.
         * </summary>
         *
         * <param name="context">Current motion context.</param>
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

            // Use velocity direction as forward reference, fall back to transform forward
            Vector3 forward = context.Velocity;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.1f)
            {
                forward = t.forward;
            }
            else
            {
                float invMag = 1f / Mathf.Sqrt(forward.sqrMagnitude);
                forward *= invMag;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // Cast to both sides
            if (Physics.Raycast(pos, right, out RaycastHit hitRight, WallDetectionDistance, WallLayers, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(-hitRight.normal, forward);
                if (angle >= MinWallAngle)
                {
                    _wallNormal = hitRight.normal;
                    _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;
                    // Ensure wall forward aligns with movement direction
                    if (Vector3.Dot(_wallForward, forward) < 0f) _wallForward = -_wallForward;
                    _wallSide = 1;
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
                    _wallSide = -1;
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
