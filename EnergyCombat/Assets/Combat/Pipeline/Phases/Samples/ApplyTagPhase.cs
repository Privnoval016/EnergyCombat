using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Phase that sets one or more tags on the <see cref="CombatContext"/> and then
     * immediately completes. Use to gate downstream abilities or phases on runtime state.
     * </summary>
     *
     * <remarks>
     * Tags are set synchronously and persist for the rest of the ability execution.
     * Combine with <see cref="HasTagCondition"/> on a follow-up ability to create
     * conditional branching: e.g. apply <c>CombatTag.Charged</c> here, then only allow
     * a "charged finisher" ability when that tag is present.
     * </remarks>
     *
     * <example>
     * Inspector: add <c>ApplyTagPhase</c> with <c>TagsToApply = [CombatTag.Charged]</c> as
     * the last phase of a charge windup ability. On the finisher ability's
     * <c>AbilityDefinition.Conditions</c>, add <c>HasTagCondition { RequiredTag = CombatTag.Charged }</c>.
     * </example>
     */
    [Serializable]
    public class ApplyTagPhase : AbilityPhase
    {
        [Tooltip("Tags applied to the CombatContext when this phase runs.")]
        public Tag[] TagsToApply;

        public override UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (TagsToApply != null)
                foreach (var tag in TagsToApply)
                    context.SetTag(tag);

            return UniTask.CompletedTask;
        }
    }
}
