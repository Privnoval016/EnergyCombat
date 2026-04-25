using System;
using System.Collections.Generic;
using DynamicPhysics.Core;
using DynamicPhysics.Orchestration;

namespace DynamicPhysics.Pipeline
{
    /**
     * <summary>
     * Executes an ordered sequence of <see cref="IMotionStage"/> instances against a shared
     * <see cref="MotionContext"/> each fixed tick. Stages are sorted by priority once when
     * the pipeline is configured, then iterated as a flat array for cache-friendly execution.
     * </summary>
     */
    public class MotionPipeline
    {
        private IMotionStage[] _stages = Array.Empty<IMotionStage>();
        private readonly List<IMotionStage> _stageList = new(8);
        private bool _dirty = true;

        #region Configuration

        /**
         * <summary>
         * Adds a stage to the pipeline. The pipeline will re-sort before the next execution.
         * </summary>
         *
         * <param name="stage">The stage to add.</param>
         */
        public void AddStage(IMotionStage stage)
        {
            _stageList.Add(stage);
            _dirty = true;
        }

        /**
         * <summary>
         * Removes a stage from the pipeline by reference.
         * </summary>
         *
         * <param name="stage">The stage to remove.</param>
         * <returns>True if the stage was found and removed.</returns>
         */
        public bool RemoveStage(IMotionStage stage)
        {
            bool removed = _stageList.Remove(stage);
            if (removed) _dirty = true;
            return removed;
        }

        /**
         * <summary>
         * Removes all stages and marks the pipeline as dirty.
         * </summary>
         */
        public void ClearStages()
        {
            _stageList.Clear();
            _dirty = true;
        }

        #endregion

        #region Execution

        /**
         * <summary>
         * Executes all stages in priority order against the provided context and config.
         * If stages have been added or removed since the last execution, the internal
         * array is re-sorted first.
         * </summary>
         *
         * <param name="context">The shared mutable motion state.</param>
         * <param name="config">The resolved runtime configuration.</param>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            if (_dirty)
            {
                RebuildSortedArray();
                _dirty = false;
            }

            for (int i = 0; i < _stages.Length; i++)
            {
                _stages[i].Execute(context, config);
            }
        }

        /**
         * <summary>
         * Sorts the stage list by priority and copies into a flat array for cache-friendly iteration.
         * </summary>
         */
        private void RebuildSortedArray()
        {
            _stageList.Sort(CompareByPriority);
            _stages = _stageList.ToArray();
        }

        private static int CompareByPriority(IMotionStage a, IMotionStage b) => a.Priority.CompareTo(b.Priority);

        #endregion
    }
}
