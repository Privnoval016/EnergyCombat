namespace Combat.Targeting.UI
{
    /**
     * <summary>
     * Contract for any visual component that displays a targeting indicator.
     * Implement this on a MonoBehaviour and assign it to
     * <see cref="TargetIndicatorController"/> to drive it from the targeting system.
     * </summary>
     *
     * <remarks>
     * The interface is intentionally minimal so both screen-space (canvas ring),
     * world-space (3D marker), and VFX implementations can satisfy it.
     * </remarks>
     */
    public interface ITargetIndicator
    {
        /**
         * <summary>
         * Position the indicator over <paramref name="target"/> and make it visible.
         * Called every <c>LateUpdate</c> for smooth world-to-screen projection.
         * </summary>
         */
        void ShowAt(ITargetable target);

        /** <summary>Hide the indicator. Called when no target is active.</summary> */
        void Hide();
    }
}
