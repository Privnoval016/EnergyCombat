using System.Collections.Generic;
using Player.Config;
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

        public SlideSettings Settings;
        
        #endregion

        #region State

        public bool IsActive { get; private set; }
        private float _slideTimer;
        private Vector3 _slideDirection;

        #endregion

        public SlideAbility(SlideSettings settings)
        {
            Settings = settings;
            IsActive = false;
            _slideTimer = 0f;
            _slideDirection = Vector3.zero;
        }

        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            if (!context.HasTag(MotionTag.Grounded)) return false;
            if (context.HasTag(MotionTag.Dashing) || context.HasTag(MotionTag.WallRunning) || context.HasTag(MotionTag.Swinging)) return false;

            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].Type == MotionRequestType.Slide)
                    return true;
            }
            return false;
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;
            _slideTimer = Settings.SlideMaxDuration > 0f ? Settings.SlideMaxDuration : float.MaxValue;

            float speed;
            if (context.Velocity != Vector3.zero)
            {
                _slideDirection = context.Velocity;
                _slideDirection.y = 0f;
                speed = _slideDirection.magnitude;
                
                if  (speed > 0.001f) _slideDirection /= speed;
                
                if (speed < Settings.SlideMinSpeed) speed = Settings.SlideMinSpeed;
            }
            else
            {
                _slideDirection = context.CharacterTransform.forward.normalized;
                _slideDirection.y = 0f;
                speed = Settings.SlideMinEntrySpeed;
            }

            float boostedSpeed = speed * Settings.SlideBoostMultiplier;
            context.Velocity.x = _slideDirection.x * boostedSpeed;
            context.Velocity.z = _slideDirection.z * boostedSpeed;

            context.SetTag(MotionTag.SlidingCrouch);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            _slideTimer -= deltaTime;

            Vector3 hVel = context.Velocity;
            hVel.y = 0f;
            float speed = hVel.magnitude;

            float newSpeed = Mathf.Max(0f, speed - Settings.SlideFriction * deltaTime);
            if (speed > 0.001f)
            {
                float scale = newSpeed / speed;
                context.Velocity.x *= scale;
                context.Velocity.z *= scale;
            }

            if (newSpeed < Settings.SlideMinSpeed || _slideTimer <= 0f)
            {
                Deactivate(context);
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.SlidingCrouch);
        }
    }
}
