namespace Combat
{
    /**
     * <summary>
     * Raised by <c>AbilityExecutor</c> when an ability pipeline finishes,
     * whether by completing normally or being cancelled.
     * </summary>
     */
    public struct AbilityEndedEvent : ICombatEvent
    {
        /** <summary>The ability definition that just finished.</summary> */
        public AbilityDefinition Ability;

        /**
         * <summary>
         * <c>true</c> if the ability was cancelled before its recovery phase completed.
         * </summary>
         */
        public bool WasInterrupted;
    }
}
