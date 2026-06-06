namespace Combat
{
    /**
     * <summary>
     * Base interface for all combat modifiers. A modifier is any object that contributes
     * gameplay variation to an ability execution without directly owning or mutating
     * execution logic.
     * </summary>
     *
     * <remarks>
     * Modifiers contribute in three non-overlapping ways via sub-interfaces:
     * <list type="bullet">
     * <item><description><see cref="IStructuralModifier"/> — alter the pipeline step sequence before execution.</description></item>
     * <item><description><c>IStatContributor</c> — contribute to stat evaluation math.</description></item>
     * <item><description><see cref="IDynamicModifier"/> — react to runtime combat events during or after execution.</description></item>
     * </list>
     *
     * Modifiers never call each other directly. They each contribute to independent layers,
     * preventing hidden dependencies and ensuring composable, predictable stacking.
     * </remarks>
     */
    public interface ICombatModifier
    {
        /** <summary>Classification tags that describe what this modifier is or requires.</summary> */
        Tag[] Tags { get; }

        /**
         * <summary>
         * <c>true</c> when this modifier should no longer be applied and should be pruned
         * from the container on the next cleanup pass.
         * </summary>
         */
        bool IsExpired { get; }

        /** <summary>Human-readable description shown in the debug overlay.</summary> */
        string DebugLabel { get; }
    }
}
