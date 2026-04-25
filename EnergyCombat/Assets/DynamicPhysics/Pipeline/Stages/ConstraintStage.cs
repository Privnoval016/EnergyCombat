
namespace DynamicPhysics
{
    /**
     * <summary>
     * Final pipeline stage that enforces all hard physical constraints.
     * Constraints execute in priority order and may destructively clamp velocity
     * to maintain physical invariants (rope length, speed limits, ground adherence).
     * </summary>
     */
    public class ConstraintStage : IMotionStage
    {
        /** <summary>Execution priority. Always runs last.</summary> */
        public int Priority => InfluencePriority.Constraints;

        /**
         * <summary>
         * Iterates and enforces all active constraints in priority order.
         * </summary>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            var constraints = config.ActiveConstraints;
            for (int i = 0; i < constraints.Count; i++)
            {
                constraints[i].Enforce(context);
            }
        }
    }
}
