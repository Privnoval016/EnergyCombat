using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Combat
{
    /**
     * <summary>
     * A structural modifier that decorates an existing <see cref="IPipelineStep"/> with
     * pre- and post-execution logic, using the Decorator pattern.
     * </summary>
     *
     * <remarks>
     * Wrapping allows behaviour to be injected around a step without replacing it.
     * The target step is matched by reference equality.
     * </remarks>
     */
    public class PipelineStepWrapper : IStructuralModifier
    {
        private readonly IPipelineStep _target;
        private readonly Func<CombatContext, CancellationToken, UniTask> _before;
        private readonly Func<CombatContext, CancellationToken, UniTask> _after;

        /**
         * <summary>Creates a wrapper around a target step.</summary>
         * <param name="target">The step in the pipeline to wrap. Matched by reference.</param>
         * <param name="before">Optional async action to run before the target step.</param>
         * <param name="after">Optional async action to run after the target step.</param>
         */
        public PipelineStepWrapper(
            IPipelineStep target,
            Func<CombatContext, CancellationToken, UniTask> before = null,
            Func<CombatContext, CancellationToken, UniTask> after = null)
        {
            _target = target;
            _before = before;
            _after = after;
        }

        /** <inheritdoc /> */
        public Tag[] Tags => Array.Empty<Tag>();

        /** <inheritdoc /> */
        public bool IsExpired => false;

        /** <inheritdoc /> */
        public string DebugLabel => "Step Wrapper";

        /** <inheritdoc /> */
        public void ModifyPipeline(List<IPipelineStep> steps, CombatContext context)
        {
            int index = steps.IndexOf(_target);
            if (index < 0) return;

            steps[index] = new WrappedStep(_target, _before, _after);
        }

        private class WrappedStep : IPipelineStep
        {
            private readonly IPipelineStep _inner;
            private readonly Func<CombatContext, CancellationToken, UniTask> _before;
            private readonly Func<CombatContext, CancellationToken, UniTask> _after;

            public WrappedStep(
                IPipelineStep inner,
                Func<CombatContext, CancellationToken, UniTask> before,
                Func<CombatContext, CancellationToken, UniTask> after)
            {
                _inner = inner;
                _before = before;
                _after = after;
            }

            public async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
            {
                if (_before != null) await _before(context, token);
                await _inner.ExecuteAsync(context, token);
                if (_after != null) await _after(context, token);
            }
        }
    }
}
