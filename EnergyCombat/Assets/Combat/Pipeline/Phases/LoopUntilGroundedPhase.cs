using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DynamicPhysics;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Pipeline phase that loops a falling animation until the player lands, then optionally
     * cross-fades to a landing clip. Designed for plunge / spike-down attacks where the
     * active window should fire exactly on ground contact.
     * </summary>
     *
     * <remarks>
     * Polled on <c>FixedUpdate</c> (one check per physics frame) for grounded state via
     * <see cref="ILocomotionState.IsGrounded"/>. <see cref="MinPlungeSpeed"/> enforces a
     * floor on downward velocity every tick so ledge clips, minor bounces, or residual
     * upward velocity cannot stall the descent. <see cref="MaxDuration"/> is a hard timeout
     * that exits the phase even if grounded detection never fires, preventing a softlock
     * when falling off a ledge or into a void.
     *
     * Set <see cref="AnimationRequest.Loop"/> on the loop clip when supplying it via
     * <see cref="LoopClip"/> — the phase plays it through <see cref="CombatContext.AnimationDriver"/>
     * and updates <see cref="CombatContext.Animation"/> so subsequent phases (e.g. a
     * <see cref="RecoveryPhase"/> awaiting <c>RecoveryEnd</c>) target the exit clip.
     * </remarks>
     */
    [Serializable]
    public class LoopUntilGroundedPhase : AbilityPhase
    {
        /**
         * <summary>
         * Looping animation clip to play while the player falls.
         * Leave <c>null</c> to keep the entry clip (set on <see cref="AbilityDefinition.AnimationRequest"/>)
         * running — useful when the entry clip naturally flows into a looping fall pose.
         * </summary>
         */
        [Tooltip("Looping clip to play while falling. Leave null to keep the entry clip running.")]
        public AnimationClip LoopClip;

        /** <summary>Cross-fade blend-in duration when switching to the loop clip.</summary> */
        [Tooltip("Cross-fade duration into the loop clip.")]
        [Min(0f)]
        public float LoopFadeIn = 0.1f;

        /**
         * <summary>
         * Clip to play the moment the player touches ground.
         * The following <see cref="RecoveryPhase"/> (or any next phase) will then
         * await animation events on this clip. Leave <c>null</c> if the next phase
         * handles its own animation or uses only a fallback timer.
         * </summary>
         */
        [Tooltip("Clip to play the moment the player touches ground. The next RecoveryPhase waits for its end event on this clip.")]
        public AnimationClip ExitClip;

        /** <summary>Cross-fade blend-in duration when switching to the landing clip.</summary> */
        [Tooltip("Cross-fade duration into the exit/landing clip.")]
        [Min(0f)]
        public float ExitFadeIn = 0.05f;

        /**
         * <summary>
         * Minimum downward speed in m/s maintained each physics tick.
         * If <c>velocity.y</c> rises above <c>-MinPlungeSpeed</c> (e.g. from a ledge clip,
         * a small bounce, or residual upward velocity at ability start), a corrective impulse
         * is applied via <c>MotionOrchestrator.AddImpulse</c>.
         * Set to <c>0</c> to rely solely on <see cref="AbilityDefinition.AerialGravityScaleOverride"/>
         * for downward acceleration.
         * </summary>
         */
        [Tooltip("Minimum downward speed (m/s) enforced every physics tick. Prevents stalls from ledge clips or bounces. 0 = no enforcement.")]
        [Min(0f)]
        public float MinPlungeSpeed = 10f;

        /**
         * <summary>
         * Hard safety timeout in seconds. The phase exits after this duration regardless of
         * whether the player has landed, preventing an infinite loop when falling off a ledge
         * or into a void. Always keep this above 0.
         * </summary>
         */
        [Tooltip("Hard safety timeout seconds. Phase exits even if the player never lands. Always set above 0.")]
        [Min(0.5f)]
        public float MaxDuration = 12f;

        /**
         * <summary>
         * Aerial gravity scale applied for the duration of this phase, overriding the ability-level
         * <see cref="AbilityDefinition.AerialGravityScaleOverride"/> stored in
         * <see cref="CombatContext.PhaseAerialGravityOverride"/>.
         * <c>0</c> = weightless, <c>1</c> = normal gravity, <c>2+</c> = fast fall.
         * Set high (e.g. 2.5) so the player slams downward while the entry frames use
         * gravity 0 via <see cref="AbilityDefinition.AerialGravityScaleOverride"/>.
         * </summary>
         */
        [Tooltip("Gravity scale while falling. 0=weightless, 1=normal, 5=very fast plunge. Overrides the ability-level aerial gravity for this phase only. No upper limit — tune to taste.")]
        [Min(0f)]
        public float PlungeGravityScale = 2.5f;

        /**
         * <summary>
         * Maximum downward speed in m/s allowed during the plunge, overriding the global
         * <see cref="SpeedLimitConstraint.MaxSpeed"/> (default 50). Set above 50 to allow the
         * plunge to exceed the global speed cap. Combined with a high <see cref="MinPlungeSpeed"/>
         * this gives an immediate, visually dramatic slam. Set to <c>0</c> to use the global cap.
         * </summary>
         */
        [Tooltip("Max plunge speed (m/s), overriding the global SpeedLimitConstraint cap (default 50). Set to 0 to respect the global limit. Values above 50 allow a visually faster slam.")]
        [Min(0f)]
        public float PlungeSpeedCap = 150f;

        /** <inheritdoc /> */
        public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (LoopClip != null && context.AnimationDriver != null)
            {
                context.Animation = context.AnimationDriver.Play(
                    new AnimationRequest { Clip = LoopClip, Loop = true, FadeInDuration = LoopFadeIn },
                    token);
            }

            // Override gravity and speed cap for the duration of the fall phase.
            // Entry frames remain weightless via AerialGravityScaleOverride=0; the loop sets
            // PlungeGravityScale so the player accelerates sharply once the fall begins.
            // PlungeSpeedCap bypasses the global SpeedLimitConstraint so very fast plunges work.
            var loco         = context.Controller?.GetComponent<ILocomotionState>();
            var orchestrator = context.Controller?.GetComponent<MotionOrchestrator>();

            context.PhaseAerialGravityOverride = PlungeGravityScale;
            if (orchestrator != null && PlungeSpeedCap > 0f)
                orchestrator.Context.SpeedCapOverride = PlungeSpeedCap;

            try
            {
                float elapsed = 0f;

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    if (loco != null && loco.IsGrounded) break;
                    if (elapsed >= MaxDuration) break;

                    // Enforce a floor on downward speed so ledge clips or small bounces
                    // cannot stall the descent and trap the player mid-ability.
                    if (orchestrator != null && MinPlungeSpeed > 0f)
                    {
                        float downSpeed = -orchestrator.Velocity.y;
                        if (downSpeed < MinPlungeSpeed)
                            orchestrator.AddImpulse(Vector3.down * (MinPlungeSpeed - downSpeed));
                    }

                    elapsed += Time.fixedDeltaTime;
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken: token);
                }
            }
            finally
            {
                context.PhaseAerialGravityOverride = null;
                if (orchestrator != null) orchestrator.Context.SpeedCapOverride = 0f;
            }

            if (ExitClip != null && context.AnimationDriver != null)
            {
                context.Animation = context.AnimationDriver.Play(
                    new AnimationRequest { Clip = ExitClip, Loop = false, FadeInDuration = ExitFadeIn },
                    token);
            }
        }
    }
}
