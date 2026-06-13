using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions.Logic;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Pipeline phase that sustains a looping animation for as long as the player continues
     * to press a configured input button AND a set of runtime conditions all pass.
     * Designed for running / sustained attacks where the move should continue while the
     * player is sprinting and mashing, then smoothly exit when they stop.
     * </summary>
     *
     * <remarks>
     * Polled on <c>FixedUpdate</c> each tick. The loop exits when any of the following occur:
     * <list type="bullet">
     *   <item>Any <see cref="ContinueConditions"/> evaluates to <c>false</c>
     *     (e.g. <see cref="SprintingCondition"/> when the player stops sprinting).</item>
     *   <item>The button has not been pressed within <see cref="InputWindowPerLoop"/> seconds
     *     (player stopped mashing).</item>
     *   <item><see cref="MaxDuration"/> seconds have elapsed (safety cap).</item>
     *   <item>The ability is interrupted (cancellation token is cancelled).</item>
     * </list>
     *
     * On clean exit the phase optionally cross-fades to <see cref="ExitClip"/> and updates
     * <see cref="CombatContext.Animation"/> so the following <see cref="RecoveryPhase"/>
     * waits on the exit animation's events rather than the loop clip.
     * </remarks>
     */
    [Serializable]
    public class LoopWhileInputConditionPhase : AbilityPhase
    {
        /**
         * <summary>
         * Looping animation clip to cross-fade to when the phase begins.
         * Leave <c>null</c> to keep whatever clip is currently playing
         * (e.g. the entry clip from <see cref="AbilityDefinition.AnimationRequest"/>).
         * </summary>
         */
        [Tooltip("Looping clip to play during the sustained attack. Leave null to keep the current clip.")]
        public AnimationClip LoopClip;

        /** <summary>Cross-fade blend-in duration when switching to the loop clip.</summary> */
        [Tooltip("Cross-fade duration into the loop clip.")]
        [Min(0f)]
        public float LoopFadeIn = 0.1f;

        /**
         * <summary>
         * Clip to cross-fade to when the loop exits cleanly (not on interruption).
         * Leave <c>null</c> to let the loop clip continue until the next phase
         * transitions or the ability ends.
         * </summary>
         */
        [Tooltip("Clip to play on clean loop exit. Leave null to proceed without clip change.")]
        public AnimationClip ExitClip;

        /** <summary>Cross-fade blend-in duration when switching to the exit clip.</summary> */
        [Tooltip("Cross-fade duration into the exit clip.")]
        [Min(0f)]
        public float ExitFadeIn = 0.1f;

        /**
         * <summary>
         * The combat button the player must repeatedly press to keep the loop going.
         * Typically <see cref="CombatInputButton.LightAttack"/> for a running light-attack chain.
         * Each iteration consumes the input, requiring a fresh press within
         * <see cref="InputWindowPerLoop"/> seconds.
         * </summary>
         */
        [Tooltip("Button the player must repeatedly press to continue looping.")]
        public CombatInputButton InputButton = CombatInputButton.LightAttack;

        /**
         * <summary>
         * How many seconds the player has to press <see cref="InputButton"/> again before the
         * loop ends. Shorter values feel more demanding; 0.4–0.6 s is comfortable for mashing.
         * </summary>
         */
        [Tooltip("Seconds the player has to re-press the button before the loop ends. 0.4–0.6 is comfortable for mashing.")]
        [Range(0.05f, 2f)]
        public float InputWindowPerLoop = 0.5f;

        /**
         * <summary>
         * All conditions must evaluate to <c>true</c> every tick for the loop to continue.
         * Add a <see cref="SprintingCondition"/> here to exit when the player stops sprinting.
         * Supports the full <see cref="ICondition{T}"/> composition tree (AND / OR / NOT).
         * </summary>
         */
        [Tooltip("Conditions that must ALL pass every tick to keep looping (e.g. SprintingCondition). Any failure exits cleanly.")]
        [SerializeReference]
        public ICondition<CombatContext>[] ContinueConditions;

        /**
         * <summary>
         * Maximum total loop duration in seconds regardless of input or conditions.
         * Acts as a safety cap to prevent an unresponsive loop from locking the player.
         * Set to <c>0</c> to disable the cap and rely entirely on conditions and input.
         * </summary>
         */
        [Tooltip("Maximum sustained duration seconds. 0 = no cap (rely on conditions and input alone).")]
        [Min(0f)]
        public float MaxDuration = 8f;

        /**
         * <summary>
         * When <c>true</c>, the loop continues as long as <see cref="InputButton"/> is
         * physically held. No consumption occurs — the loop exits the moment the button is
         * released. Use this for sustained hold-to-attack moves (e.g. running attack).
         *
         * When <c>false</c> (mash mode), the player must repeatedly press the button within
         * <see cref="InputWindowPerLoop"/> seconds each iteration.
         * </summary>
         */
        [Tooltip("Loop while the button is physically held (hold mode). Disable for mash mode where the player must re-press each iteration.")]
        public bool ContinueWhileHeld = false;

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

            var buffer  = context.Controller?.InputBuffer;
            float elapsed = 0f;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (MaxDuration > 0f && elapsed >= MaxDuration) break;

                // All runtime conditions must pass to keep the loop going.
                bool conditionsMet = true;
                if (ContinueConditions != null)
                {
                    foreach (var cond in ContinueConditions)
                    {
                        if (!cond.Evaluate(context)) { conditionsMet = false; break; }
                    }
                }
                if (!conditionsMet) break;

                if (ContinueWhileHeld)
                {
                    // Hold mode: loop as long as the button is physically held.
                    if (buffer == null || !buffer.IsHeld(InputButton)) break;
                }
                else
                {
                    // Mash mode: player must have a fresh press within the window each tick.
                    if (buffer == null || !buffer.HasRecentInput(InputButton, InputWindowPerLoop)) break;
                }

                elapsed += Time.fixedDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken: token);

                // Consume after yield so the current press remains valid for this tick's check.
                // (Original bug: consuming before yield made HasRecentInput false on tick 2.)
                if (!ContinueWhileHeld)
                    buffer?.ConsumeInput(InputButton);
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
