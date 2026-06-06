using System.Threading;
using Cysharp.Threading.Tasks;

namespace Combat
{
    /**
     * <summary>
     * A single asynchronous step in an ability's execution pipeline.
     * </summary>
     *
     * <remarks>
     * Steps execute sequentially via <see cref="AbilityPipeline.ExecuteAsync"/>.
     * Each step must respect the provided <see cref="CancellationToken"/> and clean up
     * its own state in a <c>finally</c> block so that cancellation leaves no residual effects.
     *
     * Structural modifiers (see <c>IStructuralModifier</c>) can insert, remove, or wrap
     * steps in the pipeline list <em>before</em> execution begins — they must never mutate
     * the list <em>during</em> execution.
     * </remarks>
     */
    public interface IPipelineStep
    {
        /**
         * <summary>
         * Executes this pipeline step asynchronously.
         * </summary>
         *
         * <param name="context">
         * The per-execution combat context. Shared across all steps in the same execution.
         * </param>
         * <param name="token">
         * Cancellation token propagated from the ability executor.
         * Throw <see cref="System.OperationCanceledException"/> or return early when cancelled.
         * </param>
         * <returns>A <see cref="UniTask"/> that completes when this step finishes.</returns>
         */
        UniTask ExecuteAsync(CombatContext context, CancellationToken token);
    }
}
