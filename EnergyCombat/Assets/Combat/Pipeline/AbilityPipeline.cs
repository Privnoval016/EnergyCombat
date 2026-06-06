using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Combat
{
    /**
     * <summary>
     * Executes a linear sequence of <see cref="IPipelineStep"/> instances that form an
     * ability's runtime timeline (startup → active → recovery → ...).
     * </summary>
     *
     * <remarks>
     * The pipeline is built and structurally modified <em>before</em> execution begins.
     * Once <see cref="ExecuteAsync"/> is called the step list is never mutated, ensuring
     * stable asynchronous behaviour across the lifetime of one ability execution.
     *
     * Cancellation propagates through every step. Each step is responsible for its own
     * cleanup in its <c>finally</c> block.
     * </remarks>
     */
    public class AbilityPipeline
    {
        private readonly List<IPipelineStep> _steps;

        /**
         * <summary>
         * The ordered list of steps that will execute. Structural modifiers may add,
         * remove, or wrap entries in this list before <see cref="ExecuteAsync"/> is called.
         * </summary>
         */
        public IReadOnlyList<IPipelineStep> Steps => _steps;

        /**
         * <summary>Index of the step currently executing. -1 when not running.</summary>
         */
        public int CurrentStepIndex { get; private set; } = -1;

        /**
         * <summary>Creates a pipeline from an ordered list of steps.</summary>
         * <param name="steps">The initial step sequence. Copied into an internal list.</param>
         */
        public AbilityPipeline(List<IPipelineStep> steps)
        {
            _steps = new List<IPipelineStep>(steps);
        }

        /**
         * <summary>
         * Runs all steps in order. Stops immediately if the token is cancelled or
         * a step throws.
         * </summary>
         *
         * <param name="context">Per-execution state shared across all steps.</param>
         * <param name="token">Cancellation token from the ability executor.</param>
         */
        public async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            CurrentStepIndex = 0;
            try
            {
                for (int i = 0; i < _steps.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    CurrentStepIndex = i;
                    await _steps[i].ExecuteAsync(context, token);
                }
            }
            finally
            {
                CurrentStepIndex = -1;
            }
        }

        /** <summary>Inserts a step at the specified index.</summary> */
        public void InsertStep(int index, IPipelineStep step) => _steps.Insert(index, step);

        /** <summary>Appends a step to the end of the pipeline.</summary> */
        public void AddStep(IPipelineStep step) => _steps.Add(step);

        /** <summary>Removes the first occurrence of a step. Returns true if found.</summary> */
        public bool RemoveStep(IPipelineStep step) => _steps.Remove(step);

        /** <summary>Replaces a step at the specified index.</summary> */
        public void ReplaceStep(int index, IPipelineStep step) => _steps[index] = step;

        /** <summary>Exposes the mutable step list for structural modifiers to edit.</summary> */
        public List<IPipelineStep> GetMutableSteps() => _steps;
    }
}
