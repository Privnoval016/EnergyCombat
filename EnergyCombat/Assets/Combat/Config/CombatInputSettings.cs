using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * ScriptableObject configuration for the combat input system.
     * Controls input grace windows, combo timing, and attack-entry feel.
     * Create via <c>Assets → Create → Combat → Input Settings</c>.
     * </summary>
     *
     * <remarks>
     * Assign the asset to <see cref="CombatController._inputSettings"/> in the Inspector.
     * Leaving the field null falls back to built-in defaults in <see cref="CombatInputBuffer"/>
     * and <see cref="AbilitySelector"/>.
     * </remarks>
     */
    [CreateAssetMenu(menuName = "Combat/Input Settings", fileName = "CombatInputSettings")]
    public class CombatInputSettings : ScriptableObject
    {
        #region Input Grace

        /**
         * <summary>
         * How long in seconds a tap press remains valid for triggering starter abilities
         * and advancing combo chains.
         * </summary>
         */
        [Header("Input Grace")]
        [Tooltip("Seconds a tap press stays valid for triggering abilities.")]
        [Min(0f)]
        public float GraceWindow = 0.5f;

        /**
         * <summary>
         * How long in seconds a hold-release remains valid after the button is let go.
         * Used for abilities that require holding then releasing a button.
         * </summary>
         */
        [Tooltip("Seconds a hold-release stays valid after the button is let go.")]
        [Min(0f)]
        public float HoldReleaseGraceWindow = 0.5f;

        /**
         * <summary>
         * Override grace window applied only to combo chain transitions.
         * Set to <c>0</c> to use <see cref="GraceWindow"/> for combos as well.
         * Use a lower value for strict, timing-sensitive combo windows.
         * </summary>
         */
        [Tooltip("Seconds a press stays valid for advancing a combo chain. 0 = same as GraceWindow.")]
        [Min(0f)]
        public float ComboTransitionGraceWindow = 0f;

        #endregion

        #region Attack Entry

        /**
         * <summary>
         * Rate in units per second squared at which horizontal velocity is removed
         * when an attack begins. Controls the feel of entering an attack from movement.
         * <list type="bullet">
         *   <item><c>0</c> — instant stop (crisp, character plants immediately).</item>
         *   <item>Low values (e.g. <c>20</c>) — brief momentum carry before stopping.</item>
         *   <item>High values (e.g. <c>200</c>) — near-instant but with a small physical ramp.</item>
         * </list>
         * </summary>
         */
        [Header("Attack Entry")]
        [Tooltip("Horizontal deceleration rate on attack start (units/s²). 0 = instant stop.")]
        [Min(0f)]
        public float AttackEntryDecelerationRate = 0f;

        #endregion

        #region Animation Blending

        /**
         * <summary>
         * Duration in seconds for fading the combat animation layer weight back to zero when
         * an ability ends without an immediate follow-up, allowing locomotion to blend back in.
         * <list type="bullet">
         *   <item><c>0</c> — instant cut back to locomotion.</item>
         *   <item>Low values (e.g. <c>0.1</c>) — snappy but smooth.</item>
         *   <item>Higher values (e.g. <c>0.25</c>) — softer transition out of attacks.</item>
         * </list>
         * Has no effect on combo chaining — the next clip's own
         * <see cref="AnimationRequest.FadeInDuration"/> overrides this immediately.
         * </summary>
         */
        [Header("Animation Blending")]
        [Tooltip("Seconds to fade the combat layer back to zero when an ability ends. 0 = instant.")]
        [Min(0f)]
        public float CombatLayerFadeOutDuration = 0.15f;

        #endregion
    }
}
