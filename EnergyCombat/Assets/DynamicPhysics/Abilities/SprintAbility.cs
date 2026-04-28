using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Sprint ability that increases movement speed while grounded and moving.
     * Continuously active while sprint is toggled and conditions are met.
     * Deactivates when speed drops below threshold or input stops.
     * </summary>
     */
    public class SprintAbility : IMotionAbility
    {

        #region State

        public bool IsActive { get; private set; }

        #endregion

        /**
         * <summary>
         * Active abilities try to consume requests first. Sprint doesn't intercept other requests.
         * </summary>
         */
        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        /**
         * <summary>
         * Sprint activates when:
         * - Player is grounded
         * - Sprint is toggled on (detected via input)
         * - Player has directional input
         * </summary>
         */
        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            if (!context.HasTag(MotionTag.Grounded))
                return false;

            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].Type == MotionRequestType.Sprint)
                {
                    if (context.Input.MoveInput.sqrMagnitude > 0.01f)
                    {
                        return true;
                    }
                    break;
                }
            }

            return false;
        }

        /**
         * <summary>
         * Activates sprint, which sets the Sprinting tag so that InputSteeringStage
         * will use SprintSpeed and SprintAcceleration instead of normal values.
         * </summary>
         */
        public void Activate(MotionContext context)
        {
            IsActive = true;
            context.SetTag(MotionTag.Sprinting);
        }
        
        public void Tick(MotionContext context, float deltaTime)
        {
            if (context.Velocity.ZeroVector3Axis().sqrMagnitude < 0.01f)
            {
                Deactivate(context);
            }
        }

        /**
         * <summary>
         * Returns the velocity influence. Sprint doesn't directly modify velocity;
         * InputSteeringStage handles speed increase based on the Sprinting tag.
         * </summary>
         */
        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        /**
         * <summary>
         * Deactivates sprint and removes the Sprinting tag.
         * </summary>
         */
        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.Sprinting);
        }
    }
}

