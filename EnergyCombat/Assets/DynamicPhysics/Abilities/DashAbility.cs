using System.Collections.Generic;
using Player.Config;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Directional burst movement ability with cooldown. Overrides horizontal velocity
     * for a short duration, optionally reducing gravity during the dash window.
     * </summary>
     */
    public class DashAbility : IMotionAbility
    {
        #region Configuration

        public DashSettings Settings;

        #endregion

        #region State

        public bool IsActive { get; private set; }
        private float _dashTimer;
        private float _cooldownTimer;
        private Vector3 _dashDirection;

        #endregion

        public DashAbility(DashSettings settings)
        {
            Settings = settings;
        }

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= context.DeltaTime;
                return false;
            }

            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].Type == MotionRequestType.Dash)
                {
                    _dashDirection = requests[i].Direction;
                    if (_dashDirection.sqrMagnitude < 0.01f)
                    {
                        var input = context.Input;
                        _dashDirection = input.CameraForward * input.MoveInput.y + input.CameraRight * input.MoveInput.x;
                        _dashDirection.y = 0f;
                    }
                    if (_dashDirection.sqrMagnitude < 0.01f && context.CharacterTransform != null)
                    {
                        _dashDirection = context.CharacterTransform.forward;
                    }
                    _dashDirection.y = 0f;
                    float mag = _dashDirection.magnitude;
                    if (mag > 0.001f) _dashDirection *= 1f / mag;
                    return true;
                }
            }

            return false;
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _dashTimer = Settings.DashDuration;

            Vector3 vel = _dashDirection * Settings.DashSpeed;
            vel.y = 0f;
            context.Velocity = vel;
            context.SetTag(MotionTag.Dashing);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _dashTimer -= deltaTime;
            context.GravityScale *= Settings.DashGravityScale;

            context.Velocity.x = _dashDirection.x * Settings.DashSpeed;
            context.Velocity.z = _dashDirection.z * Settings.DashSpeed;

            if (_dashTimer <= 0f) Deactivate(context);
        }

        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            _cooldownTimer = Settings.DashCooldown;
            context.RemoveTag(MotionTag.Dashing);
        }
    }
}
