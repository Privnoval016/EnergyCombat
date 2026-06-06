namespace Combat
{
    /**
     * <summary>
     * A modifier that reacts to runtime combat events such as hits, kills, or ability state
     * changes. Dynamic modifiers receive events dispatched by <c>ModifierContainer</c>
     * and can mutate the <see cref="CombatContext"/> in response.
     * </summary>
     *
     * <remarks>
     * Dynamic modifiers are the preferred place to implement "on-hit" effects such as
     * adding tags, applying secondary damage, or triggering visual feedback. Unlike
     * <see cref="IStructuralModifier"/>, dynamic modifiers receive events during execution
     * and must never mutate the pipeline step list.
     * </remarks>
     */
    public interface IDynamicModifier : ICombatModifier
    {
        /**
         * <summary>
         * Called by <c>ModifierContainer.DispatchEvent</c> when a combat event occurs.
         * </summary>
         *
         * <param name="evt">The event that fired (e.g. <see cref="HitEvent"/>, <see cref="KillEvent"/>).</param>
         * <param name="context">The active execution context. May be <c>null</c> for out-of-execution events.</param>
         */
        void OnCombatEvent(ICombatEvent evt, CombatContext context);
    }
}
