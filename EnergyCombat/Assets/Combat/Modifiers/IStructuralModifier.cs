using System.Collections.Generic;

namespace Combat
{
    /**
     * <summary>
     * A modifier that alters the structure of an ability's execution pipeline by
     * inserting, removing, or wrapping <see cref="IPipelineStep"/> instances.
     * </summary>
     *
     * <remarks>
     * Structural modifiers are applied <em>before</em> execution begins. The executor
     * calls <see cref="ModifyPipeline"/> on all structural modifiers in the container,
     * then snapshots the resulting step list. Once execution starts, the list is never
     * mutated again.
     * </remarks>
     */
    public interface IStructuralModifier : ICombatModifier
    {
        /**
         * <summary>
         * Modifies the pipeline step list before execution begins.
         * </summary>
         *
         * <param name="steps">
         * The mutable step list. Insert, remove, or replace entries as needed.
         * </param>
         * <param name="context">
         * The partially-populated execution context (ability and stats are set;
         * animation handle and hit records are not yet populated).
         * </param>
         */
        void ModifyPipeline(List<IPipelineStep> steps, CombatContext context);
    }
}
