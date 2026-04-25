using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Abilities
{
    /**
     * <summary>
     * Full-featured jump ability with coyote time, jump buffering, variable jump height,
     * and apex hang. Produces physically consistent jump arcs while preserving game-feel
     * through input-timing systems.
     * </summary>
     *
     * <remarks>
     * <para><b>Coyote time</b>: Jump request remains valid for a short window after leaving ground.</para>
     * <para><b>Jump buffering</b>: Jump input pressed slightly before landing triggers automatically on contact.</para>
     * <para><b>Variable height</b>: Releasing jump early multiplies upward velocity by a cut factor.</para>
     * <para><b>Apex hang</b>: Gravity is reduced when vertical velocity approaches zero at the jump peak.</para>
     *
     * Jump impulse is derived from desired height using kinematics: v₀ = √(2 * g * h).
     * </remarks>
     */
    public class JumpAbility : IMotionAbility
    {
        #region Configuration

        /** <summary>Desired jump height in world units.</summary> */
        public float JumpHeight { get; set; }

        /** <summary>Duration in seconds after leaving ground where jump is still valid.</summary> */
        public float CoyoteTime { get; set; }

        /** <summary>Duration in seconds before landing where a jump request is buffered.</summary> */
        public float JumpBufferTime { get; set; }

        /** <summary>Multiplier on upward velocity when jump button is released early (0-1).</summary> */
        public float JumpCutMultiplier { get; set; }

        /** <summary>Vertical speed threshold below which apex hang activates.</summary> */
        public float ApexThreshold { get; set; }

        /** <summary>Gravity multiplier during apex hang (typically 0.3-0.5).</summary> */
        public float ApexGravityMultiplier { get; set; }

        #endregion

        #region State

        public bool IsActive { get; private set; }

        private float _lastGroundedTime;
        private float _lastJumpRequestTime;
        private bool _jumpConsumed;
        private bool _jumpCutApplied;

        #endregion

        public JumpAbility(float jumpHeight = 2.5f, float coyoteTime = 0.12f,
            float jumpBufferTime = 0.1f, float jumpCutMultiplier = 0.5f,
            float apexThreshold = 1.5f, float apexGravityMultiplier = 0.4f)
        {
            JumpHeight = jumpHeight;
            CoyoteTime = coyoteTime;
            JumpBufferTime = jumpBufferTime;
            JumpCutMultiplier = jumpCutMultiplier;
            ApexThreshold = apexThreshold;
            ApexGravityMultiplier = apexGravityMultiplier;

            _lastGroundedTime = float.NegativeInfinity;
            _lastJumpRequestTime = float.NegativeInfinity;
        }

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            float now = Time.time;

            // Track grounded state for coyote time
            if ((context.Flags & MotionFlags.Grounded) != 0)
            {
                _lastGroundedTime = now;
                _jumpConsumed = false;
            }

            // Check for jump request
            for (int i = 0; i < requestCount; i++)
            {
                if (requests[i].Type == MotionRequestType.Jump)
                {
                    _lastJumpRequestTime = now;
                    break;
                }
            }

            // Can jump if: request is buffered AND (grounded OR within coyote window) AND not already consumed
            bool hasRequest = (now - _lastJumpRequestTime) <= JumpBufferTime;
            bool hasGround = (now - _lastGroundedTime) <= CoyoteTime;

            return hasRequest && hasGround && !_jumpConsumed;
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _jumpConsumed = true;
            _jumpCutApplied = false;

            // Calculate jump impulse: v₀ = √(2 * g * h)
            float gravity = Mathf.Abs(Physics.gravity.y);
            float jumpSpeed = Mathf.Sqrt(2f * gravity * JumpHeight);

            // Set vertical velocity to jump speed (overwrite, not additive, for consistency)
            context.Velocity.y = jumpSpeed;

            // Clear grounded flags
            context.Flags &= ~MotionFlags.Grounded;
            context.Flags |= MotionFlags.Airborne;
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            // Variable jump height: if jump released early, cut upward velocity
            if (!context.Input.JumpHeld && context.Velocity.y > 0f && !_jumpCutApplied)
            {
                context.Velocity.y *= JumpCutMultiplier;
                _jumpCutApplied = true;
            }

            // Apex hang: reduce gravity when near the peak
            float absVy = Mathf.Abs(context.Velocity.y);
            if (absVy < ApexThreshold && (context.Flags & MotionFlags.Airborne) != 0)
            {
                context.GravityScale *= ApexGravityMultiplier;
            }

            // Deactivate when landing
            if ((context.Flags & MotionFlags.Grounded) != 0 && context.Velocity.y <= 0.01f)
            {
                Deactivate(context);
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
        }
    }
}
