
namespace DynamicPhysics
{
    /**
     * <summary>
     * A hard physical constraint that corrects the <see cref="MotionContext"/> velocity
     * to enforce invariants after all other motion processing is complete.
     * </summary>
     *
     * <remarks>
     * Constraints are not suggestions — they are corrections. They execute last in the pipeline
     * and may destructively clamp velocity or position to maintain physical validity.
     * Examples include rope distance limits, ground snapping, and speed caps.
     * Constraints execute in priority order (lower first).
     * </remarks>
     */
    public interface IMotionConstraint
    {
        /** <summary>Execution order within the constraint stage. Lower values execute first.</summary> */
        int Priority { get; }

        /**
         * <summary>
         * Enforces this constraint by correcting the motion context's velocity.
         * </summary>
         *
         * <param name="context">The shared mutable motion state for this tick.</param>
         */
        void Enforce(MotionContext context);
    }
}
