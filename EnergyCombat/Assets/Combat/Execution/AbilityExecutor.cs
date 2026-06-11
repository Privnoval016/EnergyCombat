using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions.EventBus;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Builds and runs the execution pipeline for a single ability invocation.
     * The executor is stateless — it is driven by <c>CombatController</c> and produces
     * one <c>UniTask</c> per ability execution.
     * </summary>
     *
     * <remarks>
     * Execution flow:
     * <list type="number">
     * <item>Build a fresh <see cref="CombatContext"/> and populate stats from the ability.</item>
     * <item>Let <see cref="ModifierContainer"/> apply stat and structural modifiers.</item>
     * <item>Snapshot the pipeline step list — no further structural mutation occurs.</item>
     * <item>Start the animation and raise <see cref="AbilityStartedEvent"/>.</item>
     * <item>Await the pipeline. Forward cancellation through every step.</item>
     * <item>In <c>finally</c>: stop animation, notify selector, raise <see cref="AbilityEndedEvent"/>.</item>
     * </list>
     *
     * The <c>preserveCombo</c> flag (passed from <see cref="CombatController"/>) controls
     * which selector notification fires in <c>finally</c>:
     * <list type="bullet">
     * <item><c>preserveCombo = true</c> → <see cref="AbilitySelector.OnAbilityComboPreserved"/> (combo chain lives).</item>
     * <item><c>interrupted &amp;&amp; !preserveCombo</c> → <see cref="AbilitySelector.OnAbilityInterrupted"/> (combo resets).</item>
     * <item>Normal completion → <see cref="AbilitySelector.OnAbilityCompleted"/> (opens next window).</item>
     * </list>
     * </remarks>
     */
    public class AbilityExecutor
    {
        private readonly ModifierContainer _modifiers;
        private readonly IAnimationDriver _animationDriver;
        private readonly AbilitySelector _selector;

        public AbilityExecutor(
            ModifierContainer modifiers,
            IAnimationDriver animationDriver,
            AbilitySelector selector)
        {
            _modifiers = modifiers;
            _animationDriver = animationDriver;
            _selector = selector;
        }

        /**
         * <summary>
         * Executes the specified ability asynchronously. Returns when execution completes
         * normally or is cancelled.
         * </summary>
         *
         * <param name="ability">The ability to execute.</param>
         * <param name="controller">The <c>CombatController</c> owning this execution.</param>
         * <param name="preserveCombo">
         * When <c>true</c> the selector is notified via <see cref="AbilitySelector.OnAbilityComboPreserved"/>
         * instead of the normal interrupted/completed paths, keeping the active combo chain alive.
         * </param>
         * <param name="token">Cancellation token. Cancel to interrupt the ability mid-execution.</param>
         * <returns>The <see cref="CombatContext"/> of the completed (or interrupted) execution.</returns>
         */
        public async UniTask<CombatContext> ExecuteAsync(
            AbilityDefinition ability,
            CombatController controller,
            bool preserveCombo,
            CancellationToken token)
        {
            var context = BuildContext(ability, controller, token);
            bool interrupted = false;

            AbilityPipeline pipeline = BuildPipeline(ability, context);

            try
            {
                if (ability.AnimationRequest?.Clip != null)
                    context.Animation = _animationDriver.Play(ability.AnimationRequest, token);

                EventBus<AbilityStartedEvent>.Raise(new AbilityStartedEvent
                {
                    Ability = ability,
                    Context = context
                });

                await pipeline.ExecuteAsync(context, token);
            }
            catch (OperationCanceledException)
            {
                interrupted = true;
            }
            catch (Exception ex)
            {
                interrupted = true;
                Debug.LogException(ex);
            }
            finally
            {
                _animationDriver?.Stop();

                if (preserveCombo)
                    _selector.OnAbilityComboPreserved();
                else if (interrupted)
                    _selector.OnAbilityInterrupted();
                else
                    _selector.OnAbilityCompleted(ability);

                EventBus<AbilityEndedEvent>.Raise(new AbilityEndedEvent
                {
                    Ability = ability,
                    WasInterrupted = interrupted
                });
            }

            return context;
        }

        private CombatContext BuildContext(AbilityDefinition ability, CombatController controller, CancellationToken token)
        {
            var context = new CombatContext
            {
                Ability = ability,
                Controller = controller,
                CancellationToken = token,
                Stats = new StatSheet()
            };

            context.LoadAbilityTags();
            context.CurrentTarget = controller.CurrentTarget;

            if (ability.BaseStats != null)
            {
                foreach (var entry in ability.BaseStats)
                    context.Stats.SetBase(entry.Stat, entry.BaseValue);
            }

            _modifiers.ApplyStatModifiers(context.Stats, context);

            return context;
        }

        private AbilityPipeline BuildPipeline(AbilityDefinition ability, CombatContext context)
        {
            var steps = new List<IPipelineStep>();

            if (ability.Phases != null)
            {
                foreach (var phase in ability.Phases)
                {
                    if (phase != null)
                        steps.Add(phase);
                }
            }

            _modifiers.ApplyStructural(steps, context);

            return new AbilityPipeline(steps);
        }
    }
}
