
namespace DynamicPhysics
{
    /**
     * <summary>
     * A single processing stage within the motion pipeline.
     * Stages execute in deterministic priority order each fixed tick,
     * reading and writing into the shared <see cref="MotionContext"/>.
     * </summary>
     *
     * <remarks>
     * Lower priority values execute first. Stages should not cache references
     * to the context between frames — it is reset and re-populated each tick.
     * </remarks>
     */
    public interface IMotionStage
    {
        /** <summary>Execution order. Lower values execute first.</summary> */
        int Priority { get; }

        /**
         * <summary>
         * Processes the motion context for this frame.
         * </summary>
         *
         * <param name="context">The shared mutable motion state for this tick.</param>
         * <param name="config">The resolved runtime configuration for this tick.</param>
         */
        void Execute(MotionContext context, RuntimeMotionConfig config);
    }
}
