using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Phase that waits a fixed number of seconds before the pipeline continues.
     * Use between other phases to add a deliberate delay — e.g. a brief pause between
     * a startup windup and an active window, or a forced hold at peak of a jump.
     * </summary>
     *
     * <example>
     * Inspector setup: add a <c>WaitPhase</c> to <c>AbilityDefinition.Phases</c> and set
     * <c>Duration</c> to the desired number of seconds.
     * </example>
     */
    [Serializable]
    public class WaitPhase : AbilityPhase
    {
        [Tooltip("Seconds to wait before the next phase begins.")]
        [Range(0f, 5f)]
        public float Duration = 0.1f;

        public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.WaitForSeconds(Duration, cancellationToken: token);
        }
    }
}
