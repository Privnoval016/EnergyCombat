namespace Combat
{
    /**
     * <summary>
     * The phase of a button interaction. Mirrors <c>Systems.Input.PlayerInputPhase</c>
     * so combat-specific code never depends on the general input adapter directly.
     * </summary>
     */
    public enum CombatInputPhase
    {
        /** <summary>Button was just pressed this frame.</summary> */
        Started,

        /** <summary>Button press was confirmed (past debounce/threshold).</summary> */
        Performed,

        /** <summary>Button was released this frame.</summary> */
        Canceled
    }
}
