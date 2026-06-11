using System.Collections.Generic;
using System.Threading;
using Combat.Targeting;

namespace Combat
{
    /**
     * <summary>
     * Per-execution runtime state that flows through the entire ability pipeline.
     * A new <c>CombatContext</c> is created for each ability execution and discarded
     * when execution completes or is cancelled.
     * </summary>
     *
     * <remarks>
     * <c>CombatContext</c> is scoped to a single ability run so it can safely accumulate
     * hit records, dynamic tags, and execution-specific state without cross-frame contamination.
     * Implements <see cref="ITagContainer"/> so all subsystems query and mutate tags uniformly.
     * </remarks>
     */
    public class CombatContext : ITagContainer
    {
        #region Identity

        /** <summary>The ability definition driving this execution.</summary> */
        public AbilityDefinition Ability;

        /** <summary>The <c>CombatController</c> that owns this execution.</summary> */
        public CombatController Controller;

        /**
         * <summary>
         * The soft-targeted point at the moment this ability fired. Snapshotted at execution
         * start so mid-strike target switches do not affect the active pipeline.
         * May be <c>null</c> if no target was selected when the ability triggered.
         * </summary>
         */
        public ITargetable CurrentTarget;

        #endregion

        #region Stats

        /**
         * <summary>
         * Per-execution stat sheet. Base values are loaded from <see cref="Ability"/>
         * at execution start; modifiers from <c>ModifierContainer</c> are applied before
         * the pipeline begins.
         * </summary>
         */
        public StatSheet Stats;

        #endregion

        #region Animation

        /**
         * <summary>
         * The animation handle returned by the <c>IAnimationDriver</c> for this execution.
         * Pipeline phases use this to await specific animation events or completion.
         * </summary>
         */
        public AnimationHandle Animation;

        #endregion

        #region Execution Control

        /** <summary>Cancellation token propagated through every async pipeline step.</summary> */
        public CancellationToken CancellationToken;

        #endregion

        #region Hit Records

        /** <summary>All hits registered during this execution.</summary> */
        public readonly List<HitData> RegisteredHits = new();

        #endregion

        #region Tags

        private readonly HashSet<Tag> _attackTags = new(8);

        /** <inheritdoc /> */
        public bool HasTag(Tag tag) => _attackTags.Contains(tag);

        /** <inheritdoc /> */
        public void SetTag(Tag tag) => _attackTags.Add(tag);

        /** <inheritdoc /> */
        public void RemoveTag(Tag tag) => _attackTags.Remove(tag);

        /** <inheritdoc /> */
        public void ClearTags() => _attackTags.Clear();

        /** <summary>Returns a snapshot of all currently active tags for debug display.</summary> */
        public IReadOnlyCollection<Tag> GetAllTags() => _attackTags;

        #endregion

        /**
         * <summary>
         * Initialises the context with all tags from the ability's static tag list.
         * Called by <c>AbilityExecutor</c> immediately after context construction.
         * </summary>
         */
        public void LoadAbilityTags()
        {
            if (Ability?.Tags == null) return;
            foreach (var tag in Ability.Tags)
                _attackTags.Add(tag);
        }
    }
}
