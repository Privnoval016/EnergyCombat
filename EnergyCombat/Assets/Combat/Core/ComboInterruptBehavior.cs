namespace Combat
{
    /**
     * <summary>
     * Controls whether executing an ability resets the active combo chain or preserves it.
     * Assign on <see cref="AbilityDefinition.InterruptBehavior"/>.
     * </summary>
     *
     * <remarks>
     * Use <see cref="PreserveCombo"/> for basic attacks from a secondary weapon (e.g. an off-hand
     * axe basic) so that the main-hand combo window stays open during the off-hand attack.
     * Use <see cref="BreakCombo"/> for launching specials, dodges, or any ability that should
     * reset the combo chain.
     *
     * The number of consecutive <c>PreserveCombo</c> interrupts is capped by
     * <see cref="ComboDefinition.MaxConcurrentInterrupts"/> — set that to 0 for unlimited.
     * </remarks>
     */
    public enum ComboInterruptBehavior
    {
        /** <summary>Resets the active combo chain. Default for most abilities.</summary> */
        BreakCombo = 0,

        /**
         * <summary>
         * Keeps the active combo chain alive. The combo window timer continues running and
         * the current node does not advance. Use for secondary-weapon basic attacks that
         * should not destroy an in-progress primary combo.
         * </summary>
         */
        PreserveCombo = 1,
    }
}
