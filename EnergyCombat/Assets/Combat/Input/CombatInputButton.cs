namespace Combat
{
    /**
     * <summary>
     * Identifies a combat-specific button. Each value maps directly to an action in the
     * Unity Input System (via <c>PlayerInputAdapter</c>) and can be remapped there without
     * changes to this enum or the combat system.
     * </summary>
     *
     * <remarks>
     * Add new values here when new combat inputs are needed (e.g. <c>Special</c>).
     * Both <see cref="AbilityDefinition.PrimaryInput"/> and <c>ComboTransition.Button</c>
     * reference this enum, keeping input identity data-driven.
     * </remarks>
     */
    public enum CombatInputButton
    {
        /** <summary>Standard attack input — typically mapped to a face button.</summary> */
        LightAttack,

        /** <summary>Heavy or charged attack input.</summary> */
        HeavyAttack,

        /**
         * <summary>
         * Synthetic chord input synthesised by <c>PlayerController</c> when both
         * <see cref="LightAttack"/> and <see cref="HeavyAttack"/> are pressed within
         * <see cref="CombatInputSettings.ChordDetectionWindow"/> seconds of each other.
         * Use as <c>PrimaryInput</c> on any ability that should require both buttons simultaneously.
         * </summary>
         */
        LightHeavyChord
    }
}
