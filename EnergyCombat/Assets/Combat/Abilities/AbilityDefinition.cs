using Extensions.Logic;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * ScriptableObject data asset describing a single ability or attack move.
     * This is the primary designer-facing unit of the combat system.
     * </summary>
     *
     * <remarks>
     * An <c>AbilityDefinition</c> describes <em>what</em> a move is — its input trigger,
     * preconditions, stats, animation, and pipeline structure. It does not execute logic itself.
     *
     * Phases are configured inline via <c>[SerializeReference]</c>, meaning each ability
     * fully owns its startup/active/recovery settings without shared assets. This makes
     * abilities self-contained and trivial to duplicate and modify.
     * </remarks>
     */
    [CreateAssetMenu(fileName = "New Ability", menuName = "Combat/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        #region Identity

        /** <summary>Human-readable name shown in the debug overlay and editor.</summary> */
        [Header("Identity")]
        [Tooltip("Display name shown in the debug overlay and editor. No gameplay effect.")]
        public string AbilityName;

        /**
         * <summary>
         * Classification tags applied to every execution of this ability.
         * Examples: <c>CombatTag.Melee</c>, <c>CombatTag.Fire</c>.
         * </summary>
         */
        [Tooltip("Tags attached to every execution of this ability (e.g. Melee, Fire, Unblockable). Used by conditions and modifiers to filter or buff this move.")]
        public Tag[] Tags;

        #endregion

        #region Input

        /** <summary>The combat button that triggers this ability when no combo context overrides it.</summary> */
        [Header("Input")]
        [Tooltip("Button that triggers this ability from idle or when no active combo transition matches. Combos can override this with their own button per-transition.")]
        public CombatInputButton PrimaryInput;

        /**
         * <summary>
         * If <c>true</c>, the button must be held past <see cref="HoldThreshold"/> seconds.
         * Otherwise a tap suffices.
         * </summary>
         */
        [Tooltip("Require the player to hold the button rather than tap it. Useful for charged or powered-up versions of an attack.")]
        public bool RequireHold;

        /** <summary>Seconds the button must be held. Only evaluated when <see cref="RequireHold"/> is true.</summary> */
        [Tooltip("Seconds the button must be held before this ability triggers. Only evaluated when Require Hold is enabled.")]
        public float HoldThreshold = 0.3f;

        #endregion

        #region Conditions

        /**
         * <summary>
         * All conditions must evaluate to <c>true</c> before this ability is allowed to execute.
         * Supports full AND/OR/NOT composition via the Extensions.Logic condition tree.
         * Common conditions: <see cref="GroundedCondition"/>, <see cref="AirborneCondition"/>,
         * <see cref="SprintingCondition"/>, <see cref="HasTagCondition"/>.
         * </summary>
         */
        [Header("Conditions")]
        [Tooltip("All conditions must pass before this ability can execute. Common options: GroundedCondition, AirborneCondition, SprintingCondition. AND / OR / NOT composition is supported.")]
        [SerializeReference]
        public ICondition<CombatContext>[] Conditions;

        #endregion

        #region Stats

        /**
         * <summary>
         * Base stat values loaded into the execution's <c>StatSheet</c>.
         * Modifiers from <c>ModifierContainer</c> are layered on top at runtime.
         * The <c>StatId</c> field is fully editable in the Inspector.
         * </summary>
         */
        [Header("Stats")]
        [Tooltip("Base stat values for this move (Damage, Range, Knockback, etc.). Runtime modifiers from equipped items or buffs are layered on top of these values.")]
        public AbilityStatEntry[] BaseStats;

        #endregion

        #region Animation

        /**
         * <summary>
         * Describes the animation clip and playback settings for this ability.
         * Passed to <c>IAnimationDriver.Play</c> at the start of execution.
         * Leave <c>Clip</c> null to skip animation entirely (useful while prototyping).
         * </summary>
         */
        [Header("Animation")]
        [Tooltip("Clip and playback settings for this ability. Leave Clip empty to prototype the move without animation — the pipeline phases still run using their fallback timers.")]
        public AnimationRequest AnimationRequest;

        #endregion

        #region Pipeline

        /**
         * <summary>
         * Ordered list of phases that define the ability's execution timeline.
         * Use the Inspector's <c>[+]</c> button to add <see cref="StartupPhase"/>,
         * <see cref="ActivePhase"/>, <see cref="RecoveryPhase"/>, or any custom phase.
         * Each phase is configured inline — no shared assets required.
         * </summary>
         */
        [Header("Pipeline")]
        [Tooltip("Ordered execution timeline. Add phases in sequence: Startup (wind-up frames) → Active (hitbox window) → Recovery (return to neutral). Each phase runs until its animation event fires or its fallback timer elapses.")]
        [SerializeReference]
        public AbilityPhase[] Phases;

        #endregion

        #region Combo

        /**
         * <summary>
         * Optional combo tree that defines follow-up attacks available after this ability lands.
         * The tree is walked by <c>AbilitySelector</c> during the combo window.
         * <c>null</c> means this ability does not start or continue a combo.
         * </summary>
         */
        [Header("Combo")]
        [Tooltip("Combo tree that becomes available after this ability lands. Assign a Combo Definition asset. Leave empty if this move does not start a chain.")]
        public ComboDefinition FollowUpCombo;

        /**
         * <summary>
         * Controls whether executing this ability resets or preserves the active combo chain.
         * Use <see cref="ComboInterruptBehavior.PreserveCombo"/> for secondary-weapon basic attacks
         * so the primary combo window stays open.
         * </summary>
         */
        [Tooltip("BreakCombo (default): firing this ability resets any active combo chain. PreserveCombo: the chain stays alive — use for dodges or off-hand actions that shouldn't cancel the main combo.")]
        public ComboInterruptBehavior InterruptBehavior = ComboInterruptBehavior.BreakCombo;

        #endregion

        #region Aerial Behavior

        /**
         * <summary>
         * When <c>true</c>, this ability uses <see cref="AerialGravityScaleOverride"/> instead
         * of the global <see cref="Combat.CombatInputSettings.AerialAttackGravityScale"/> when
         * executed in the air. Useful for moves that need a unique gravity feel — for example,
         * a dive kick (high gravity) versus a floaty rising swipe (low gravity).
         * </summary>
         */
        [Header("Aerial Behavior")]
        [Tooltip("When enabled, overrides the global aerial gravity settings for this specific ability. Use for moves that need a unique gravity feel (e.g. a dive kick vs a floaty swipe).")]
        public bool OverrideAerialGravity = false;

        /**
         * <summary>
         * Gravity scale used when this ability is airborne and <see cref="OverrideAerialGravity"/>
         * is <c>true</c>. <c>0</c> = weightless, <c>1</c> = normal gravity.
         * </summary>
         */
        [Tooltip("Gravity scale for this ability when airborne. 0 = weightless, 1 = normal. Only used when Override Aerial Gravity is enabled.")]
        [Range(0f, 1f)]
        public float AerialGravityScaleOverride = 0.2f;

        #endregion

        #region Target Pull

        /**
         * <summary>
         * When <c>true</c>, applies a brief velocity impulse toward the soft target at the
         * moment this ability activates. Produces a natural-feeling lunge that closes small
         * gaps mid-combo without hard-snapping across long distances.
         * Has no effect when no target is selected.
         * </summary>
         */
        [Header("Target Pull")]
        [Tooltip("Apply a brief impulse toward the soft target when this ability starts. Useful for closing small gaps mid-combo. No effect if no target is selected.")]
        public bool EnableTargetPull = false;

        /**
         * <summary>
         * Magnitude of the impulse toward the soft target on ability activation.
         * Tune to feel like a natural lunge rather than a teleport.
         * </summary>
         */
        [Tooltip("Impulse magnitude toward the soft target on attack start. Tune to feel like a natural lunge rather than a teleport.")]
        [Min(0f)]
        public float TargetPullForce = 6f;

        /**
         * <summary>
         * Maximum distance from the target at which the pull impulse activates.
         * Beyond this range no pull is applied, preventing unwanted long-range snapping.
         * </summary>
         */
        [Tooltip("Maximum distance at which target pull activates. Beyond this range no pull is applied. Use to prevent long-range snapping.")]
        [Min(0f)]
        public float TargetPullRange = 3f;

        #endregion

        /**
         * <summary>
         * Evaluates all conditions against the given context.
         * Returns <c>true</c> only if every condition passes, or if there are no conditions.
         * </summary>
         */
        public bool AreConditionsMet(CombatContext context)
        {
            if (Conditions == null) return true;
            foreach (var condition in Conditions)
            {
                if (!condition.Evaluate(context)) return false;
            }

            return true;
        }
    }
}
