namespace Combat.Targeting
{
    /**
     * <summary>
     * Dependency-inversion contract for the targeting system.
     * <see cref="Combat.CombatController"/> depends on this interface rather than
     * <see cref="SoftTargetingSystem"/> directly, so the targeting strategy can be
     * swapped or mocked without modifying the combat pipeline.
     * </summary>
     */
    public interface ITargetProvider
    {
        /** <summary>The currently selected target point, or <c>null</c> if none is active.</summary> */
        ITargetable CurrentTarget { get; }

        /** <summary>Convenience shorthand for <c>CurrentTarget != null</c>.</summary> */
        bool HasTarget { get; }
    }
}
