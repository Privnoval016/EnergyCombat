using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Applies gravity to the vertical velocity component with support for context-aware scaling.
     * Uses cached gravity vector. Only applied when not solidly grounded.
     * </summary>
     */
    public class GravityStage : IMotionStage
    {
        public int Priority => InfluencePriority.Gravity;

        private Vector3 _cachedGravity;

        public GravityStage()
        {
            _cachedGravity = Physics.gravity;
        }

        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            bool grounded = context.HasTag(MotionTag.Grounded);
            bool sliding = context.HasTag(MotionTag.Sliding);

            if (grounded && !sliding) return;

            float scale = context.GravityScale * config.GravityScale;
            context.Velocity += _cachedGravity * (scale * context.DeltaTime);
        }

        /** <summary>Refreshes cached gravity if Physics.gravity changes at runtime.</summary> */
        public void RefreshGravity()
        {
            _cachedGravity = Physics.gravity;
        }
    }
}
