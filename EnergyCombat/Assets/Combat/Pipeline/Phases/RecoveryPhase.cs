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
     * When <see cref="AllowComboCancel"/> is <c>true</c>, the <see cref="CombatTag.ComboWindowOpen"/>
     * tag is set at phase entry (timer-based mode, for backward compatibility). The tag is removed
     * in a <c>finally</c> block only if <em>this phase</em> set it — if the tag was opened by a
     * <c>OnCombatComboWindowOpen</c> animation event on the clip, it is left for the event system
     * (or <c>OnCombatComboWindowClose</c>) to manage.
     *
     * When <see cref="AllowComboCancel"/> is <c>false</c>, the tag is managed entirely by
     * <see cref="CombatAnimationEventReceiver"/> animation events, giving per-frame control over
     * when the combo chain can be advanced.
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
         * If <c>true</c>, the combo window is opened immediately at phase entry (timer-based mode).
         * If <c>false</c>, the combo window is driven entirely by the <c>OnCombatComboWindowOpen</c>
         * and <c>OnCombatComboWindowClose</c> animation events placed on the clip timeline.
         * </summary>
         */
        [Tooltip("Allow combo cancels during recovery. Disable to use animation events for per-frame control.")]
        public bool AllowComboCancel = true;

        /** <inheritdoc /> */
        public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Track whether this phase opened the tag so the finally block only
            // removes it when we are responsible — if an animation event opened the
            // tag instead, we leave it for the event system to close.
            bool tagSet = false;

            try
            {
                if (AllowComboCancel)
                {
                    context.SetTag(CombatTag.ComboWindowOpen);
                    tagSet = true;
                }

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
                if (tagSet)
                    context.RemoveTag(CombatTag.ComboWindowOpen);
            }
        }
    }
}
