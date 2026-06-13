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

        #region Aerial Combat — Gravity

        /**
         * <summary>
         * Global gravity multiplier applied while an aerial attack is executing.
         * Produces the "hang in the air" feel seen in DMC, Nier, and MGRR.
         * <list type="bullet">
         *   <item><c>0</c> — completely weightless during the attack.</item>
         *   <item><c>0.2</c> — DMC-style light hang.</item>
         *   <item><c>1</c> — no effect (normal gravity applies).</item>
         * </list>
         * Per-attack overrides are available on each <see cref="AbilityDefinition"/> via
         * <c>OverrideAerialGravity</c> / <c>AerialGravityScaleOverride</c>.
         * </summary>
         */
        [Header("Aerial Combat — Gravity")]
        [Tooltip("Gravity multiplier during aerial attacks. 0 = weightless, 1 = normal gravity. ~0.2 gives DMC-style hang.")]
        [Range(0f, 1f)]
        public float AerialAttackGravityScale = 0.2f;

        /**
         * <summary>
         * Fraction of downward velocity cancelled each time a new aerial attack starts.
         * Produces the momentary height-hold seen on hit-linking in action games.
         * <list type="bullet">
         *   <item><c>0</c> — no cancellation (fall continues uninterrupted).</item>
         *   <item><c>0.85</c> — 85% of downward speed zeroed at each hit.</item>
         *   <item><c>1</c> — instant full stop on each aerial hit.</item>
         * </list>
         * </summary>
         */
        [Tooltip("Fraction of downward velocity cancelled each time a new aerial attack starts. 1 = instant stop, 0 = no effect.")]
        [Range(0f, 1f)]
        public float AerialAttackVelocityDamping = 0.85f;

        /**
         * <summary>
         * Exponential multiplier applied to <see cref="AerialAttackGravityScale"/> after each
         * consecutive aerial attack, so gravity gradually returns to normal.
         * <list type="bullet">
         *   <item><c>1</c> — no decay; every hit floats equally.</item>
         *   <item><c>1.25</c> — gravity returns to normal over roughly 4 hits.</item>
         *   <item><c>2</c> — fast decay; gravity nearly doubles each hit.</item>
         * </list>
         * </summary>
         */
        [Tooltip("Each consecutive aerial attack multiplies gravity scale by this factor, gradually returning to normal. 1 = no decay, 1.3 = returns over ~4 hits.")]
        [Range(1f, 2f)]
        public float AerialGravityDecayPerHit = 1.25f;

        /**
         * <summary>
         * Maximum number of consecutive aerial attacks that apply gravity decay before
         * gravity is treated as fully returned to normal.
         * Set to <c>0</c> to let gravity degrade without a hard cap.
         * </summary>
         */
        [Tooltip("Aerial attacks before gravity fully returns. 0 = unlimited. Combine with Decay Per Hit.")]
        [Min(0)]
        public int MaxAerialAttacksBeforeGravityReturns = 5;

        /**
         * <summary>
         * Seconds gravity remains suppressed after the last aerial attack finishes executing.
         * Provides a float window between hits so the player can queue the next attack without
         * the character immediately dropping. Set to <c>0</c> for gravity to return instantly
         * when the attack pipeline ends.
         * </summary>
         */
        [Tooltip("Seconds gravity stays suppressed after an aerial attack ends. Gives a brief float window between hits. 0 = gravity returns instantly when the attack ends.")]
        [Min(0f)]
        public float PostAttackHoverDuration = 0.2f;

        #endregion

        #region Combat — Facing

        /**
         * <summary>
         * Rotation speed used when <see cref="DynamicPhysics.CombatTargetFacingStage"/> overrides
         * facing direction toward a soft target during an attack.
         * Replaces <c>SteeringSettings.RotationSpeed</c> for that frame so combat snap can be
         * tuned independently from locomotion rotation responsiveness.
         * <list type="bullet">
         *   <item><c>10</c> — same as typical locomotion (gradual).</item>
         *   <item><c>25</c> — fast snap; character faces enemy well within a single attack.</item>
         *   <item><c>50+</c> — near-instant; similar to a hard lock-on.</item>
         * </list>
         * </summary>
         */
        [Header("Combat — Facing")]
        [Tooltip("Rotation speed when auto-facing a soft target during an attack. Higher values snap faster than locomotion rotation. 25 is a good combat default.")]
        [Min(1f)]
        public float CombatFacingRotationSpeed = 25f;

        #endregion

        #region Aerial Combat — Vertical Aim

        /**
         * <summary>
         * When <c>true</c>, the player character pitches up or down toward a soft-targeted enemy
         * during aerial attacks, compensating for elevation differences automatically.
         * Has no effect when no target is selected.
         * </summary>
         */
        [Header("Aerial Combat — Vertical Aim")]
        [Tooltip("Enable vertical (pitch) rotation toward the soft target during aerial attacks.")]
        public bool EnableAerialVerticalAim = true;

        /**
         * <summary>
         * Maximum degrees of pitch (up or down) the character can rotate toward the target.
         * Prevents extreme upward or downward tilts for targets at extreme elevations.
         * </summary>
         */
        [Tooltip("Maximum up/down pitch angle toward the target (degrees). Beyond this the character holds its current pitch.")]
        [Range(0f, 60f)]
        public float MaxAerialPitchAngle = 35f;

        /**
         * <summary>
         * How quickly vertical pitch interpolates toward the clamped target angle each frame.
         * Matches the feel of <c>SteeringSettings.RotationSpeed</c> in <c>MovementProfile</c>;
         * higher values produce a snappier snap-to-enemy feel.
         * </summary>
         */
        [Tooltip("How quickly vertical pitch interpolates toward the target angle. Matches the feel of RotationSpeed in SteeringSettings.")]
        [Range(1f, 20f)]
        public float AerialPitchSpeed = 8f;

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
