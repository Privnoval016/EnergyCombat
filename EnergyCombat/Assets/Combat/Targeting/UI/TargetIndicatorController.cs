using UnityEngine;

namespace Combat.Targeting.UI
{
    /**
     * <summary>
     * Drives any <see cref="ITargetIndicator"/> implementation from the
     * <see cref="SoftTargetingSystem"/>. Place on the HUD/canvas root and assign
     * both the player's <see cref="SoftTargetingSystem"/> and a MonoBehaviour that
     * implements <see cref="ITargetIndicator"/> (e.g. <see cref="ScreenSpaceTargetRing"/>).
     * </summary>
     *
     * <remarks>
     * The indicator is refreshed in <c>LateUpdate</c> every frame while a target is active
     * so screen-space projection stays smooth regardless of the targeting evaluation interval.
     * </remarks>
     */
    [AddComponentMenu("Combat/Targeting/UI/Target Indicator Controller")]
    public class TargetIndicatorController : MonoBehaviour
    {
        /** <summary>The player's targeting system. Drives indicator visibility.</summary> */
        [Tooltip("The SoftTargetingSystem on the player.")]
        [SerializeField] private SoftTargetingSystem _targeting;

        /**
         * <summary>
         * The screen-space ring that tracks the current target.
         * Swap this field type for a different <see cref="ITargetIndicator"/> implementation
         * if a custom indicator is needed (world-space marker, VFX, etc.).
         * </summary>
         */
        [Tooltip("ScreenSpaceTargetRing (or any ITargetIndicator MonoBehaviour) on the HUD.")]
        [SerializeField] private ScreenSpaceTargetRing _indicator;

        private void OnEnable()
        {
            if (_targeting != null) _targeting.OnTargetChanged += HandleTargetChanged;
        }

        private void OnDisable()
        {
            if (_targeting != null) _targeting.OnTargetChanged -= HandleTargetChanged;
            _indicator?.Hide();
        }

        private void LateUpdate()
        {
            if (_indicator == null || _targeting == null) return;
            if (_targeting.HasTarget)
                _indicator.ShowAt(_targeting.CurrentTarget);
        }

        private void HandleTargetChanged(ITargetable _, ITargetable newTarget)
        {
            if (newTarget == null) _indicator?.Hide();
        }
    }
}
