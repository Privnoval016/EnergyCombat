using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Pipeline phase representing recovery frames — the window after an attack during
     * which the character returns to neutral. The combo cancel window, if any, opens here.
     * </summary>
     *
     * <remarks>
     * At entry the <see cref="CombatTag.ComboWindowOpen"/> tag is added to the context,
     * signalling to <c>AbilitySelector</c> that combo follow-ups may be queued.
     * The tag is removed in a <c>finally</c> block on both completion and cancellation.
     * </remarks>
     */
    [Serializable]
    public class RecoveryPhase : AbilityPhase
    {
        /** <summary>Animation event name that ends recovery and returns to neutral.</summary> */
        [Tooltip("Animation event key that signals the end of recovery frames.")]
        public string RecoveryEndEvent = "RecoveryEnd";

        /**
         * <summary>
         * Fallback duration in seconds if the animation event never fires.
         * Set to 0 to disable and rely entirely on animation events.
         * </summary>
         */
        [Tooltip("Fallback seconds if the RecoveryEnd event never fires. 0 = disabled.")]
        public float FallbackDuration = 0.3f;

        /**
         * <summary>
         * If <c>true</c>, combo follow-up inputs are accepted during this phase.
         * If <c>false</c>, the combo window is not opened.
         * </summary>
         */
        [Tooltip("Allow combo cancels during recovery.")]
        public bool AllowComboCancel = true;

        /** <inheritdoc /> */
        public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                if (AllowComboCancel)
                    context.SetTag(CombatTag.ComboWindowOpen);

                if (context.Animation == null)
                {
                    if (FallbackDuration > 0f)
                        await UniTask.WaitForSeconds(FallbackDuration, cancellationToken: token);
                    return;
                }

                if (FallbackDuration > 0f)
                {
                    await UniTask.WhenAny(
                        context.Animation.WaitForEventAsync(RecoveryEndEvent, token),
                        UniTask.WaitForSeconds(FallbackDuration, cancellationToken: token)
                    );
                }
                else
                {
                    await context.Animation.WaitForEventAsync(RecoveryEndEvent, token);
                }
            }
            finally
            {
                context.RemoveTag(CombatTag.ComboWindowOpen);
            }
        }
    }
}
