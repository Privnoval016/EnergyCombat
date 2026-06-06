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
        public string AbilityName;

        /**
         * <summary>
         * Classification tags applied to every execution of this ability.
         * Examples: <c>CombatTag.Melee</c>, <c>CombatTag.Fire</c>.
         * </summary>
         */
        public Tag[] Tags;

        #endregion

        #region Input

        /** <summary>The combat button that triggers this ability when no combo context overrides it.</summary> */
        [Header("Input")]
        public CombatInputButton PrimaryInput;

        /**
         * <summary>
         * If <c>true</c>, the button must be held past <see cref="HoldThreshold"/> seconds.
         * Otherwise a tap suffices.
         * </summary>
         */
        public bool RequireHold;

        /** <summary>Seconds the button must be held. Only evaluated when <see cref="RequireHold"/> is true.</summary> */
        [Tooltip("Seconds button must be held. Only used when RequireHold = true.")]
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
        public ComboDefinition FollowUpCombo;

        /**
         * <summary>
         * Controls whether executing this ability resets or preserves the active combo chain.
         * Use <see cref="ComboInterruptBehavior.PreserveCombo"/> for secondary-weapon basic attacks
         * so the primary combo window stays open.
         * </summary>
         */
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
