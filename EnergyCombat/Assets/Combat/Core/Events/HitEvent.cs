namespace Combat
{
    /**
     * <summary>
     * Raised by <c>WeaponHitboxController</c> when a hit is successfully registered
     * against a valid target during the active phase.
     * </summary>
     */
    public struct HitEvent : ICombatEvent
    {
        /** <summary>Data describing the hit: source, target, damage, tags, position.</summary> */
        public HitData Hit;

        /** <summary>The execution context in which the hit occurred.</summary> */
        public CombatContext Context;
    }
}
