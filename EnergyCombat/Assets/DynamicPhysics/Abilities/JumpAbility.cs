using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Full-featured jump ability with coyote time, jump buffering, variable jump height,
     * and apex hang. Jump impulse derived from desired height: v₀ = √(2gh).
     * </summary>
     */
    public class JumpAbility : IMotionAbility
    {
        #region Configuration

        public float JumpHeight { get; set; }
        public float CoyoteTime { get; set; }
        public float JumpBufferTime { get; set; }
        public float JumpCutMultiplier { get; set; }
        public float ApexThreshold { get; set; }
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

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            float now = Time.time;

            if (context.HasTag(MotionTag.Grounded))
            {
                _lastGroundedTime = now;
                _jumpConsumed = false;
            }

            for (int i = 0; i < requestCount; i++)
            {
                if (requests[i].Type == MotionRequestType.Jump)
                {
                    _lastJumpRequestTime = now;
                    break;
                }
            }

            bool hasRequest = (now - _lastJumpRequestTime) <= JumpBufferTime;
            bool hasGround = (now - _lastGroundedTime) <= CoyoteTime;

            return hasRequest && hasGround && !_jumpConsumed;
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _jumpConsumed = true;
            _jumpCutApplied = false;

            float gravity = Mathf.Abs(Physics.gravity.y);
            float jumpSpeed = Mathf.Sqrt(2f * gravity * JumpHeight);
            context.Velocity.y = jumpSpeed;

            context.RemoveTag(MotionTag.Grounded);
            context.SetTag(MotionTag.Airborne);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            if (!context.Input.JumpHeld && context.Velocity.y > 0f && !_jumpCutApplied)
            {
                context.Velocity.y *= JumpCutMultiplier;
                _jumpCutApplied = true;
            }

            float absVy = Mathf.Abs(context.Velocity.y);
            if (absVy < ApexThreshold && context.HasTag(MotionTag.Airborne))
            {
                context.GravityScale *= ApexGravityMultiplier;
            }

            if (context.HasTag(MotionTag.Grounded) && context.Velocity.y <= 0.01f)
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
