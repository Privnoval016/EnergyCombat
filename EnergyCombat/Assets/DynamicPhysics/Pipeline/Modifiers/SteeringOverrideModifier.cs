using DynamicPhysics.Core;

namespace DynamicPhysics.Pipeline.Modifiers
{
    /**
     * <summary>
     * Overrides the steering responsiveness multiplier on the motion context.
     * Use to reduce player control during attacks, hit reactions, or other states
     * where input influence should be dampened.
     * </summary>
     */
    public class SteeringOverrideModifier : IMotionModifier
    {
        /** <summary>Execution order within the modifier stage.</summary> */
        public int Order { get; }

        /**
         * <summary>
         * Multiplier applied to the context's steering multiplier.
         * Values below 1.0 reduce responsiveness, above 1.0 increase it.
         * </summary>
         */
        public float Responsiveness { get; set; }

        public SteeringOverrideModifier(float responsiveness, int order = 0)
        {
            Responsiveness = responsiveness;
            Order = order;
        }

        public void Apply(MotionContext context)
        {
            context.SteeringMultiplier *= Responsiveness;
        }
    }
}
