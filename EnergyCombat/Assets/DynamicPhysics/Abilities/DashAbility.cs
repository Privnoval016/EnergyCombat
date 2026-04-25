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

        public float DashSpeed { get; set; }
        public float DashDuration { get; set; }
        public float Cooldown { get; set; }
        public float DashGravityScale { get; set; }

        #endregion

        #region State

        public bool IsActive { get; private set; }
        private float _dashTimer;
        private float _cooldownTimer;
        private Vector3 _dashDirection;

        #endregion

        public DashAbility(float dashSpeed = 25f, float dashDuration = 0.15f,
            float cooldown = 0.8f, float dashGravityScale = 0.1f)
        {
            DashSpeed = dashSpeed;
            DashDuration = dashDuration;
            Cooldown = cooldown;
            DashGravityScale = dashGravityScale;
        }

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= context.DeltaTime;
                return false;
            }

            for (int i = 0; i < requestCount; i++)
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
            _dashTimer = DashDuration;

            Vector3 vel = _dashDirection * DashSpeed;
            vel.y = 0f;
            context.Velocity = vel;
            context.SetTag(MotionTag.Dashing);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _dashTimer -= deltaTime;
            context.GravityScale *= DashGravityScale;

            context.Velocity.x = _dashDirection.x * DashSpeed;
            context.Velocity.z = _dashDirection.z * DashSpeed;

            if (_dashTimer <= 0f) Deactivate(context);
        }

        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            _cooldownTimer = Cooldown;
            context.RemoveTag(MotionTag.Dashing);
        }
    }
}
