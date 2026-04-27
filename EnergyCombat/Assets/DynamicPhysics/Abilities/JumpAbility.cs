using System.Collections.Generic;
using Player.Config;
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

        public JumpSettings Settings;

        public MovementProfile MovementProfile;

        #endregion

        #region State

        public bool IsActive { get; private set; }
        private float _lastGroundedTime;
        private float _lastJumpRequestTime;
        private bool _jumpConsumed;
        private bool _jumpCutApplied;

        #endregion

        public JumpAbility(JumpSettings settings, MovementProfile profile)
        {
            Settings = settings;
            MovementProfile = profile;
            _lastGroundedTime = float.NegativeInfinity;
            _lastJumpRequestTime = float.NegativeInfinity;
        }

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            float now = Time.time;

            if (context.HasTag(MotionTag.Grounded))
            {
                _lastGroundedTime = now;
                _jumpConsumed = false;
            }

            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].Type == MotionRequestType.Jump)
                {
                    _lastJumpRequestTime = now;
                    break;
                }
            }

            bool hasRequest = (now - _lastJumpRequestTime) <= Settings.JumpBuffer;
            bool hasGround = (now - _lastGroundedTime) <= Settings.CoyoteTime;
            
            return hasRequest && hasGround && !_jumpConsumed;
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _jumpConsumed = true;
            _jumpCutApplied = false;

            float gravity = Mathf.Abs(Physics.gravity.y) * MovementProfile.GravityScale;
            float jumpSpeed = Mathf.Sqrt(2f * gravity * Settings.JumpHeight);
            context.Velocity.y = jumpSpeed;

            context.RemoveTag(MotionTag.Grounded);
            context.SetTag(MotionTag.Airborne);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            if (!context.Input.JumpHeld && context.Velocity.y > 0f && !_jumpCutApplied)
            {
                context.Velocity.y *= Settings.JumpCutMultiplier;
                _jumpCutApplied = true;
            }

            float absVy = Mathf.Abs(context.Velocity.y);
            if (absVy < Settings.ApexThreshold && context.HasTag(MotionTag.Airborne))
            {
                context.GravityScale *= Settings.ApexGravityMultiplier;
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
