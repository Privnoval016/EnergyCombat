namespace Combat
{
    /**
     * <summary>
     * Raised by <c>AbilityExecutor</c> immediately before the ability pipeline begins execution.
     * </summary>
     */
    public struct AbilityStartedEvent : ICombatEvent
    {
        /** <summary>The ability definition that is about to execute.</summary> */
        public AbilityDefinition Ability;

        /** <summary>The per-execution context for this ability run.</summary> */
        public CombatContext Context;
    }
}
