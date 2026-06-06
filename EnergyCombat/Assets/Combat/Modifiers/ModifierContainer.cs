using System.Collections.Generic;

namespace Combat
{
    /**
     * <summary>
     * Owns all <see cref="ICombatModifier"/> instances attached to a <c>CombatController</c>
     * and exposes operations to apply them at the correct phase of ability execution.
     * </summary>
     *
     * <remarks>
     * The three application methods map to the three modifier layers:
     * <list type="bullet">
     * <item><description><see cref="ApplyStatModifiers"/> — called before pipeline build to populate the stat sheet.</description></item>
     * <item><description><see cref="ApplyStructural"/> — called after stat setup, before pipeline snapshot.</description></item>
     * <item><description><see cref="DispatchEvent"/> — called at runtime when combat events fire.</description></item>
     * </list>
     * </remarks>
     */
    public class ModifierContainer
    {
        private readonly List<ICombatModifier> _modifiers = new();

        /** <summary>Read-only view of all modifiers including expired ones.</summary> */
        public IReadOnlyList<ICombatModifier> All => _modifiers;

        /** <summary>Adds a modifier. Does nothing if null.</summary> */
        public void Add(ICombatModifier modifier)
        {
            if (modifier != null) _modifiers.Add(modifier);
        }

        /** <summary>Removes a specific modifier instance. Does nothing if not found.</summary> */
        public void Remove(ICombatModifier modifier) => _modifiers.Remove(modifier);

        /** <summary>Removes all expired modifiers.</summary> */
        public void RemoveExpired() => _modifiers.RemoveAll(m => m.IsExpired);

        /** <summary>Returns all non-expired modifiers of the specified type.</summary> */
        public List<T> GetAll<T>() where T : class, ICombatModifier
        {
            var result = new List<T>();
            foreach (var mod in _modifiers)
            {
                if (!mod.IsExpired && mod is T typed)
                    result.Add(typed);
            }

            return result;
        }

        /**
         * <summary>
         * Applies all non-expired stat-contributing modifiers to the execution's stat sheet.
         * Called by the executor before building the pipeline.
         * </summary>
         */
        public void ApplyStatModifiers(StatSheet stats, CombatContext context)
        {
            foreach (var mod in _modifiers)
            {
                if (mod.IsExpired) continue;
                if (mod is IStatContributor contributor)
                    contributor.ContributeStats(stats, context);
            }
        }

        /**
         * <summary>
         * Calls <see cref="IStructuralModifier.ModifyPipeline"/> on all non-expired structural
         * modifiers, allowing them to insert, remove, or wrap pipeline steps.
         * Called after stat setup, before the pipeline snapshot.
         * </summary>
         */
        public void ApplyStructural(List<IPipelineStep> steps, CombatContext context)
        {
            foreach (var mod in _modifiers)
            {
                if (mod.IsExpired) continue;
                if (mod is IStructuralModifier structural)
                    structural.ModifyPipeline(steps, context);
            }
        }

        /**
         * <summary>
         * Dispatches a combat event to all non-expired <see cref="IDynamicModifier"/> instances.
         * </summary>
         */
        public void DispatchEvent(ICombatEvent evt, CombatContext context)
        {
            foreach (var mod in _modifiers)
            {
                if (mod.IsExpired) continue;
                if (mod is IDynamicModifier dynamic)
                    dynamic.OnCombatEvent(evt, context);
            }
        }

        /** <summary>Returns all active modifier debug labels for the debug overlay.</summary> */
        public IEnumerable<string> GetDebugLabels()
        {
            foreach (var mod in _modifiers)
            {
                if (!mod.IsExpired)
                    yield return mod.DebugLabel ?? mod.GetType().Name;
            }
        }
    }

    /**
     * <summary>
     * Optional interface for modifiers that contribute <see cref="StatModifier"/> entries
     * to the execution's <see cref="StatSheet"/>. Implemented alongside <see cref="ICombatModifier"/>.
     * </summary>
     */
    public interface IStatContributor
    {
        /** <summary>Adds one or more stat modifiers to the sheet for this execution.</summary> */
        void ContributeStats(StatSheet stats, CombatContext context);
    }
}
