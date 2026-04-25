using DynamicPhysics.Core;
using DynamicPhysics.Orchestration;
using UnityEngine;

namespace DynamicPhysics.Pipeline.Stages
{
    /**
     * <summary>
     * Applies gravity to the vertical velocity component with support for context-aware scaling.
     * Gravity scale is modified by abilities (apex hang), modifiers, and profiles before this stage runs.
     * </summary>
     *
     * <remarks>
     * Uses the cached gravity vector from Unity's <see cref="Physics.gravity"/> setting.
     * Gravity is only applied when not grounded, unless the character is on a steep slope.
     * The gravity vector is cached on construction to avoid per-frame property access.
     * </remarks>
     */
    public class GravityStage : IMotionStage
    {
        /** <summary>Execution priority. Runs after external forces.</summary> */
        public int Priority => InfluencePriority.Gravity;

        private Vector3 _cachedGravity;

        public GravityStage()
        {
            _cachedGravity = Physics.gravity;
        }

        /**
         * <summary>
         * Applies scaled gravity to the velocity. Only affects airborne or sliding characters.
         * </summary>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            bool grounded = (context.Flags & MotionFlags.Grounded) != 0;
            bool sliding = (context.Flags & MotionFlags.Sliding) != 0;

            // Don't apply gravity when solidly grounded (and not sliding off a steep slope)
            if (grounded && !sliding) return;

            float scale = context.GravityScale * config.GravityScale;
            context.Velocity += _cachedGravity * (scale * context.DeltaTime);
        }

        /**
         * <summary>
         * Refreshes the cached gravity vector. Call this if Physics.gravity changes at runtime.
         * </summary>
         */
        public void RefreshGravity()
        {
            _cachedGravity = Physics.gravity;
        }
    }
}
