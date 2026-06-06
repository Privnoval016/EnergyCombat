namespace Combat
{
    /**
     * <summary>
     * Built-in <see cref="Tag"/> constants for the combat system.
     * </summary>
     *
     * <remarks>
     * Tags are descriptive classifications, not behaviours. All combat logic that needs to
     * differentiate behaviour should query these tags rather than branching on type enums.
     * Add project-specific tags in a separate static class using the same pattern.
     * </remarks>
     */
    public static class CombatTag
    {
        // ── Attack Type ────────────────────────────────────────────────────────────────

        /** <summary>Attack is a melee strike.</summary> */
        public static readonly Tag Melee = new Tag("Melee");

        /** <summary>Attack is ranged or projectile-based.</summary> */
        public static readonly Tag Ranged = new Tag("Ranged");

        /** <summary>Attack originates from a projectile that travels.</summary> */
        public static readonly Tag Projectile = new Tag("Projectile");

        /** <summary>Ability was triggered from the ground.</summary> */
        public static readonly Tag Grounded = new Tag("CombatGrounded");

        /** <summary>Ability was triggered while airborne.</summary> */
        public static readonly Tag Aerial = new Tag("Aerial");

        // ── Elemental ──────────────────────────────────────────────────────────────────

        /** <summary>Attack carries fire damage or a fire effect.</summary> */
        public static readonly Tag Fire = new Tag("Fire");

        /** <summary>Attack carries ice damage or a freeze effect.</summary> */
        public static readonly Tag Ice = new Tag("Ice");

        /** <summary>Attack carries lightning damage or a shock effect.</summary> */
        public static readonly Tag Lightning = new Tag("Lightning");

        // ── Execution State ────────────────────────────────────────────────────────────

        /** <summary>The attack was fully charged (held beyond threshold).</summary> */
        public static readonly Tag Charged = new Tag("Charged");

        /** <summary>The attack resolved as a critical hit.</summary> */
        public static readonly Tag Crit = new Tag("Crit");

        /** <summary>This ability begins a combo sequence.</summary> */
        public static readonly Tag ComboStarter = new Tag("ComboStarter");

        /** <summary>This ability continues an active combo chain.</summary> */
        public static readonly Tag ComboFollowUp = new Tag("ComboFollowUp");

        /** <summary>The combo cancel window is currently open on this execution.</summary> */
        public static readonly Tag ComboWindowOpen = new Tag("ComboWindowOpen");

        // ── Target State ───────────────────────────────────────────────────────────────

        /** <summary>Target is designated as a boss-tier enemy.</summary> */
        public static readonly Tag Boss = new Tag("Boss");

        /** <summary>Target is currently burning.</summary> */
        public static readonly Tag Burning = new Tag("Burning");

        /** <summary>Target is currently frozen.</summary> */
        public static readonly Tag Frozen = new Tag("Frozen");

        /** <summary>Target is currently stunned.</summary> */
        public static readonly Tag Stunned = new Tag("CombatStunned");
    }
}
