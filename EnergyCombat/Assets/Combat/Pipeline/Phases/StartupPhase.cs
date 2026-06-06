using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Pipeline phase representing startup frames — the window before the hitbox becomes
     * active and the attack "comes out".
     * </summary>
     *
     * <remarks>
     * Waits for the <see cref="StartupEndEvent"/> animation event. If the event does not
     * fire within <see cref="FallbackDuration"/> seconds (when &gt; 0), the phase completes
     * automatically so the pipeline can continue.
     *
     * By design the startup phase is NOT cancellable mid-execution — it always runs to
     * completion before the executor's cancellation is honoured. This mirrors fighting-game
     * conventions where startup frames are a committed action. Override this behaviour in
     * a subclass if your design requires interruptible startup.
     * </remarks>
     */
    [Serializable]
    public class StartupPhase : AbilityPhase
    {
        /** <summary>Animation event name that ends startup and begins the active phase.</summary> */
        [Tooltip("Animation event key that signals the end of startup frames.")]
        public string StartupEndEvent = "StartupEnd";

        /**
         * <summary>
         * Fallback duration in seconds. If the animation event never fires, startup ends
         * after this many seconds. Set to 0 to disable and rely entirely on animation events.
         * </summary>
         */
        [Tooltip("Fallback seconds if the StartupEnd event never fires. 0 = disabled.")]
        public float FallbackDuration = 0.25f;

        /** <inheritdoc /> */
        public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (context.Animation == null)
            {
                if (FallbackDuration > 0f)
                    await UniTask.WaitForSeconds(FallbackDuration, cancellationToken: token);
                return;
            }

            if (FallbackDuration > 0f)
            {
                /* Race: either the StartupEnd animation event fires, or the fallback timer
                 * expires — whichever comes first advances the pipeline. */
                await UniTask.WhenAny(
                    context.Animation.WaitForEventAsync(StartupEndEvent, token),
                    UniTask.WaitForSeconds(FallbackDuration, cancellationToken: token)
                );
            }
            else
            {
                await context.Animation.WaitForEventAsync(StartupEndEvent, token);
            }
        }
    }
}
