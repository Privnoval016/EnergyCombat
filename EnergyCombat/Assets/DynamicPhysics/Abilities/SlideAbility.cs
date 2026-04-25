using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Ground slide ability that boosts speed, lowers the character, and decelerates over time.
     * Height adjustment is handled via a callback delegate for collider decoupling.
     * </summary>
     */
    public class SlideAbility : IMotionAbility
    {
        #region Configuration

        public float SpeedBoostMultiplier { get; set; }
        public float SlideFriction { get; set; }
        public float MinSlideSpeed { get; set; }
        public float MaxDuration { get; set; }
        public float MinEntrySpeed { get; set; }
        public float SlideHeightMultiplier { get; set; }

        /** <summary>Delegate invoked to adjust character height. Parameter is height multiplier.</summary> */
        public System.Action<float> OnHeightChange { get; set; }

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

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            if (!context.HasTag(MotionTag.Grounded)) return false;
            if (context.HasTag(MotionTag.Dashing) || context.HasTag(MotionTag.WallRunning) || context.HasTag(MotionTag.Swinging)) return false;

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

            float boostedSpeed = speed * SpeedBoostMultiplier;
            context.Velocity.x = _slideDirection.x * boostedSpeed;
            context.Velocity.z = _slideDirection.z * boostedSpeed;

            context.SetTag(MotionTag.SlidingCrouch);
            OnHeightChange?.Invoke(SlideHeightMultiplier);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _slideTimer -= deltaTime;

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

            if (newSpeed < MinSlideSpeed || _slideTimer <= 0f)
            {
                Deactivate(context);
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.SlidingCrouch);
            OnHeightChange?.Invoke(1f);
        }
    }
}
