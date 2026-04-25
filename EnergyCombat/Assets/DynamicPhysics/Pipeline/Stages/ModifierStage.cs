using DynamicPhysics.Core;
using DynamicPhysics.Orchestration;
using DynamicPhysics.Pipeline.Modifiers;

namespace DynamicPhysics.Pipeline.Stages
{
    /**
     * <summary>
     * Applies all active <see cref="IMotionModifier"/> instances to the context in deterministic order.
     * Handles expiration of <see cref="TemporalModifier"/> wrappers by skipping expired modifiers.
     * </summary>
     */
    public class ModifierStage : IMotionStage
    {
        /** <summary>Execution priority. Runs after gravity.</summary> */
        public int Priority => InfluencePriority.Modifiers;

        /**
         * <summary>
         * Iterates and applies all active modifiers in order.
         * </summary>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            var modifiers = config.ActiveModifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];

                // Skip expired temporal modifiers
                if (modifier is TemporalModifier temporal && temporal.IsExpired) continue;

                modifier.Apply(context);
            }
        }
    }
}
