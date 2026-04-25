
namespace DynamicPhysics
{
    /**
     * <summary>
     * A stateless transformation applied to the <see cref="MotionContext"/> during the modifier stage.
     * Each modifier performs a single-responsibility transformation such as scaling gravity,
     * applying drag, or capping speed.
     * </summary>
     *
     * <remarks>
     * Modifiers are executed in deterministic order based on <see cref="Order"/>.
     * Because they are stateless, they can be freely shared across movement modes and abilities.
     * To create a time-limited modifier, wrap any modifier with <see cref="TemporalModifier"/>.
     * </remarks>
     */
    public interface IMotionModifier
    {
        /** <summary>Execution order within the modifier stage. Lower values execute first.</summary> */
        int Order { get; }

        /**
         * <summary>
         * Applies this modifier's transformation to the motion context.
         * </summary>
         *
         * <param name="context">The shared mutable motion state for this tick.</param>
         */
        void Apply(MotionContext context);
    }
}
