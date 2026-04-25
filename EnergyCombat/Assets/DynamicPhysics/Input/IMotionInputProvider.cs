namespace DynamicPhysics.Input
{
    /**
     * <summary>
     * Abstraction for providing continuous input data to the motion system.
     * Implement this interface to bridge any input system (Unity Input System, Rewired, custom)
     * into the DynamicPhysics pipeline.
     * </summary>
     *
     * <remarks>
     * This interface handles only continuous input (movement direction, held buttons).
     * Discrete actions (jump press, dash press) should be routed through
     * <see cref="DynamicPhysics.Orchestration.MotionOrchestrator"/> request methods.
     * </remarks>
     */
    public interface IMotionInputProvider
    {
        /**
         * <summary>
         * Captures and returns the current frame's input state.
         * Called once per fixed tick by the orchestrator.
         * </summary>
         *
         * <returns>A snapshot of the current input state.</returns>
         */
        MotionInputData GetInput();
    }
}
