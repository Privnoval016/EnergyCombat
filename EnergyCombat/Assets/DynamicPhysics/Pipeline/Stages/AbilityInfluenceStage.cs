using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Iterates active abilities and additively applies their velocity influences to the context.
     * Abilities never destructively overwrite velocity — they contribute additive deltas.
     * </summary>
     */
    public class AbilityInfluenceStage : IMotionStage
    {
        /** <summary>Execution priority. Runs after input steering.</summary> */
        public int Priority => InfluencePriority.AbilityInfluence;

        /**
         * <summary>
         * Collects and applies velocity influences from all active abilities.
         * </summary>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            var abilities = config.ActiveAbilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                var ability = abilities[i];
                if (!ability.IsActive) continue;

                Vector3 influence = ability.GetVelocityInfluence(context);
                context.Velocity += influence;
            }
        }
    }
}
