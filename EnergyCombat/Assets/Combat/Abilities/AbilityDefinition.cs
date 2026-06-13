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
