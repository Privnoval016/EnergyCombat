namespace Combat
{
    /**
     * <summary>
     * Represents any entity that can be the target of a combat ability or attack.
     * </summary>
     *
     * <remarks>
     * Implement this on enemies, destructible objects, or any damageable entity.
     * The <see cref="TagContainer"/> is used by <c>TagQuery.Matches</c> for hitbox filtering.
     * </remarks>
     */
    public interface ICombatTarget
    {
        /** <summary>The tag container describing the target's current state and classification.</summary> */
        ITagContainer TagContainer { get; }

        /** <summary>The target's runtime stats (health, resistances, etc.).</summary> */
        StatSheet Stats { get; }
    }
}
