using System;
using System.Collections.Generic;

namespace Combat
{
    /**
     * <summary>
     * A structural modifier that inserts an <see cref="IPipelineStep"/> at a specified
     * index in the pipeline before execution begins.
     * </summary>
     *
     * <remarks>
     * Use a negative <see cref="_insertIndex"/> to append to the end of the pipeline
     * regardless of its current length. All indices ≥ 0 are clamped to valid range.
     * </remarks>
     */
    public class PipelineStepInserter : IStructuralModifier
    {
        private readonly IPipelineStep _step;
        private readonly int _insertIndex;

        /**
         * <summary>Creates a step inserter.</summary>
         * <param name="step">The step to insert.</param>
         * <param name="insertIndex">
         * Index to insert at. Use -1 (or any negative value) to append to the end.
         * </param>
         */
        public PipelineStepInserter(IPipelineStep step, int insertIndex = -1)
        {
            _step = step;
            _insertIndex = insertIndex;
        }

        /** <inheritdoc /> */
        public Tag[] Tags => Array.Empty<Tag>();

        /** <inheritdoc /> */
        public bool IsExpired => false;

        /** <inheritdoc /> */
        public string DebugLabel => $"Insert Step [{_insertIndex}]";

        /** <inheritdoc /> */
        public void ModifyPipeline(List<IPipelineStep> steps, CombatContext context)
        {
            if (_step == null) return;

            if (_insertIndex < 0 || _insertIndex >= steps.Count)
                steps.Add(_step);
            else
                steps.Insert(_insertIndex, _step);
        }
    }
}
