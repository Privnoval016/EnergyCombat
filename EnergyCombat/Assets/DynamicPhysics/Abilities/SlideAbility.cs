using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Abilities
{
    /**
     * <summary>
     * Ground slide ability that boosts speed, lowers the character, and decelerates over time.
     * Activated by a slide request while grounded and moving above a minimum speed threshold.
     * </summary>
     *
     * <remarks>
     * The slide uses a callback delegate for height adjustment so the system remains
     * decoupled from any specific collider type. The gameplay layer provides the height
     * change implementation when registering this ability.
     *
     * Sets <see cref="MotionFlags.SlidingCrouch"/> to reduce steering control during the slide.
     * Exit conditions: speed drops below minimum, max duration exceeded, or slide request cancelled.
     * </remarks>
     */
    public class SlideAbility : IMotionAbility
    {
        #region Configuration

        /** <summary>Speed boost applied at slide start, multiplied by current speed.</summary> */
        public float SpeedBoostMultiplier { get; set; }

        /** <summary>Friction/deceleration applied during the slide in units per second squared.</summary> */
        public float SlideFriction { get; set; }

        /** <summary>Minimum speed to maintain the slide. Below this, the slide ends.</summary> */
        public float MinSlideSpeed { get; set; }

        /** <summary>Maximum slide duration in seconds. 0 = unlimited (friction-limited only).</summary> */
        public float MaxDuration { get; set; }

        /** <summary>Minimum speed required to initiate a slide.</summary> */
        public float MinEntrySpeed { get; set; }

        #endregion

        #region Delegates

        /**
         * <summary>
         * Delegate invoked to adjust the character's height during slide.
         * Parameter is the target height multiplier (e.g., 0.5 for half height).
         * The gameplay layer provides the implementation.
         * </summary>
         */
        public System.Action<float> OnHeightChange { get; set; }

        /** <summary>Height multiplier applied during slide (0.5 = half height).</summary> */
        public float SlideHeightMultiplier { get; set; }

        #endregion

        #region State

        public bool IsActive { get; private set; }

        private float _slideTimer;
        private Vector3 _slideDirection;

        #endregion

        public SlideAbility(float speedBoostMultiplier = 1.3f, float slideFriction = 12f,
            float minSlideSpeed = 2f, float maxDuration = 1.5f,
            float minEntrySpeed = 5f, float slideHeightMultiplier = 0.5f)
        {
            SpeedBoostMultiplier = speedBoostMultiplier;
            SlideFriction = slideFriction;
            MinSlideSpeed = minSlideSpeed;
            MaxDuration = maxDuration;
            MinEntrySpeed = minEntrySpeed;
            SlideHeightMultiplier = slideHeightMultiplier;
        }

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            if ((context.Flags & MotionFlags.Grounded) == 0) return false;
            if ((context.Flags & (MotionFlags.Dashing | MotionFlags.WallRunning | MotionFlags.Swinging)) != 0) return false;

            // Check speed
            Vector3 hVel = context.Velocity;
            hVel.y = 0f;
            if (hVel.sqrMagnitude < MinEntrySpeed * MinEntrySpeed) return false;

            for (int i = 0; i < requestCount; i++)
            {
                if (requests[i].Type == MotionRequestType.Slide)
                    return true;
            }

            return false;
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _slideTimer = MaxDuration > 0f ? MaxDuration : float.MaxValue;

            // Capture slide direction from current velocity
            _slideDirection = context.Velocity;
            _slideDirection.y = 0f;
            float speed = _slideDirection.magnitude;
            if (speed > 0.001f)
            {
                _slideDirection *= 1f / speed;
            }
            else if (context.CharacterTransform != null)
            {
                _slideDirection = context.CharacterTransform.forward;
                _slideDirection.y = 0f;
                speed = context.Velocity.magnitude;
            }

            // Apply speed boost
            float boostedSpeed = speed * SpeedBoostMultiplier;
            context.Velocity.x = _slideDirection.x * boostedSpeed;
            context.Velocity.z = _slideDirection.z * boostedSpeed;

            context.Flags |= MotionFlags.SlidingCrouch;

            // Adjust height
            OnHeightChange?.Invoke(SlideHeightMultiplier);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _slideTimer -= deltaTime;

            // Apply slide friction (decelerate)
            Vector3 hVel = context.Velocity;
            hVel.y = 0f;
            float speed = hVel.magnitude;

            float newSpeed = Mathf.Max(0f, speed - SlideFriction * deltaTime);

            if (speed > 0.001f)
            {
                float scale = newSpeed / speed;
                context.Velocity.x *= scale;
                context.Velocity.z *= scale;
            }

            // Exit conditions
            if (newSpeed < MinSlideSpeed || _slideTimer <= 0f)
            {
                Deactivate(context);
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.Flags &= ~MotionFlags.SlidingCrouch;

            // Restore height
            OnHeightChange?.Invoke(1f);
        }
    }
}
