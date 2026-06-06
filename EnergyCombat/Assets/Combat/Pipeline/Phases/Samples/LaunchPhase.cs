using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DynamicPhysics;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Phase that launches the attacker and/or all hit targets into the air, then waits
     * a brief hang duration before the pipeline continues. Ideal for aerial-combo openers:
     * both the player and any struck enemies are suspended together, giving time to follow
     * up with air combos.
     * </summary>
     *
     * <remarks>
     * <list type="bullet">
     * <item><see cref="LaunchSelf"/> uses <see cref="MotionOrchestrator.AddImpulse"/> so the
     *   impulse integrates naturally through the physics pipeline without fighting gravity
     *   immediately.</item>
     * <item><see cref="LaunchHitTargets"/> calls <c>Rigidbody.AddForce(ForceMode.Impulse)</c>
     *   on every target recorded in <see cref="CombatContext.RegisteredHits"/>.</item>
     * <item><see cref="HangDuration"/> lets the pipeline pause here so the player floats at
     *   the peak before the recovery phase starts.</item>
     * </list>
     * This phase is fully cancellable — the hang await respects the cancellation token, so
     * interrupting the ability during the hang exits cleanly.
     * </remarks>
     *
     * <example>
     * Pipeline: StartupPhase → ActivePhase → LaunchPhase(LaunchSelf=true, LaunchHitTargets=true,
     *   LaunchForce=12, HangDuration=0.4) → RecoveryPhase
     * </example>
     */
    [Serializable]
    public class LaunchPhase : AbilityPhase
    {
        [Header("Attacker")]
        [Tooltip("Apply upward impulse to the attacker (the player character).")]
        public bool LaunchSelf = true;

        [Tooltip("Upward speed added to the attacker via MotionOrchestrator.AddImpulse.")]
        [Range(0f, 30f)]
        public float LaunchForce = 10f;

        [Header("Targets")]
        [Tooltip("Apply upward impulse to every Rigidbody target recorded in context.RegisteredHits.")]
        public bool LaunchHitTargets = true;

        [Tooltip("Upward force magnitude applied to each hit target's Rigidbody.")]
        [Range(0f, 30f)]
        public float TargetLaunchForce = 8f;

        [Header("Hang")]
        [Tooltip("Seconds to wait at peak before the next phase begins. 0 = skip immediately.")]
        [Range(0f, 2f)]
        public float HangDuration = 0.3f;

        public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (LaunchSelf && context.Controller != null)
            {
                var orchestrator = context.Controller.GetComponent<MotionOrchestrator>();
                orchestrator?.AddImpulse(Vector3.up * LaunchForce);
            }

            if (LaunchHitTargets)
            {
                foreach (var hit in context.RegisteredHits)
                {
                    // TODO: swap to motion orchestrator
                    if (hit?.Target == null) continue;
                    var rb = hit.Target.GetComponent<Rigidbody>();
                    rb?.AddForce(Vector3.up * TargetLaunchForce, ForceMode.Impulse);
                }
            }

            if (HangDuration > 0f)
                await UniTask.WaitForSeconds(HangDuration, cancellationToken: token);
        }
    }
}
