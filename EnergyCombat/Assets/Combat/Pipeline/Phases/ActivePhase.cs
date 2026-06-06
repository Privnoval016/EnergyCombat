using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Pipeline phase representing the active frames of an attack — the window during which
     * hitboxes are live and can register hits.
     * </summary>
     *
     * <remarks>
     * At entry all hitboxes listed in <see cref="Hitboxes"/> are activated simultaneously
     * via <c>CombatController.GetHitboxController</c>. They are all deactivated in a
     * <c>finally</c> block, guaranteeing cleanup on both normal completion and cancellation.
     *
     * Multiple <see cref="HitboxActivation"/> entries support abilities that need several
     * simultaneous hitboxes (e.g. a spinning kick that sweeps both a foot and a leg collider).
     *
     * The phase ends when the <c>"ActiveEnd"</c> animation event fires, or after
     * <see cref="FallbackDuration"/> seconds if the event does not arrive.
     * </remarks>
     */
    [Serializable]
    public class ActivePhase : AbilityPhase
    {
        /** <summary>Animation event name that ends the active hitbox window.</summary> */
        [Tooltip("Animation event key that signals the end of active frames.")]
        public string ActiveEndEvent = "ActiveEnd";

        /**
         * <summary>
         * Fallback duration in seconds if the animation event never fires.
         * Set to 0 to disable and rely entirely on animation events.
         * </summary>
         */
        [Tooltip("Fallback seconds if the ActiveEnd event never fires. 0 = disabled.")]
        public float FallbackDuration = 0.2f;

        /**
         * <summary>
         * Hitboxes to activate during this phase. Each entry targets a named
         * <see cref="WeaponHitboxController"/> on the character prefab.
         * Leave empty for abilities without hit detection (buffs, movement).
         * </summary>
         */
        [Tooltip("Hitboxes to activate. Each HitboxId must match a WeaponHitboxController on the prefab.")]
        public HitboxActivation[] Hitboxes;

        /** <inheritdoc /> */
        public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var activatedControllers = new List<IHitboxController>();

            try
            {
                if (Hitboxes != null && context.Controller != null)
                {
                    foreach (var activation in Hitboxes)
                    {
                        if (activation?.Config == null) continue;
                        var ctrl = context.Controller.GetHitboxController(activation.HitboxId);
                        if (ctrl == null) continue;

                        ctrl.Activate(activation.Config, context);
                        activatedControllers.Add(ctrl);
                    }
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
                        context.Animation.WaitForEventAsync(ActiveEndEvent, token),
                        UniTask.WaitForSeconds(FallbackDuration, cancellationToken: token)
                    );
                }
                else
                {
                    await context.Animation.WaitForEventAsync(ActiveEndEvent, token);
                }
            }
            finally
            {
                foreach (var ctrl in activatedControllers)
                    ctrl.Deactivate();
            }
        }
    }
}
