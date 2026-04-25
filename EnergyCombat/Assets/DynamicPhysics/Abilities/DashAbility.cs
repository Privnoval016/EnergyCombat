using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Abilities
{
    /**
     * <summary>
     * Directional burst movement ability with cooldown. Overrides horizontal velocity
     * for a short duration, optionally reducing gravity during the dash window.
     * </summary>
     *
     * <remarks>
     * Sets <see cref="MotionFlags.Dashing"/> while active, which causes the
     * <see cref="Pipeline.Stages.InputSteeringStage"/> to heavily reduce player control.
     * The dash direction defaults to the input direction or character forward if no input.
     * </remarks>
     */
    public class DashAbility : IMotionAbility
    {
        #region Configuration

        /** <summary>Dash speed in units per second.</summary> */
        public float DashSpeed { get; set; }

        /** <summary>Duration of the dash in seconds.</summary> */
        public float DashDuration { get; set; }

        /** <summary>Cooldown between dashes in seconds.</summary> */
        public float Cooldown { get; set; }

        /** <summary>Gravity multiplier during dash (0 = weightless dash).</summary> */
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

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            // Tick cooldown
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= context.DeltaTime;
                return false;
            }

            for (int i = 0; i < requestCount; i++)
            {
                if (requests[i].Type == MotionRequestType.Dash)
                {
                    // Use request direction, fall back to input direction, then character forward
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

            // Set dash velocity
            Vector3 vel = _dashDirection * DashSpeed;
            vel.y = 0f;
            context.Velocity = vel;

            context.Flags |= MotionFlags.Dashing;
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _dashTimer -= deltaTime;

            // Reduce gravity during dash
            context.GravityScale *= DashGravityScale;

            // Maintain dash velocity (prevent steering from overriding)
            context.Velocity.x = _dashDirection.x * DashSpeed;
            context.Velocity.z = _dashDirection.z * DashSpeed;

            if (_dashTimer <= 0f)
            {
                Deactivate(context);
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            _cooldownTimer = Cooldown;
            context.Flags &= ~MotionFlags.Dashing;
        }
    }
}
