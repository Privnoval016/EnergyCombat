using System.Collections.Generic;

namespace Combat
{
    /**
     * <summary>
     * Holds base stat values and a collection of <see cref="StatModifier"/> contributions
     * for a single entity or ability execution. Stats are computed on demand — the evaluation
     * is always deterministic.
     * </summary>
     *
     * <remarks>
     * Evaluation order (fixed, never changes):
     * base → sum(Additive) → ×(1 + sum(Multiplicative)) → highest-priority Override.
     * Example: base 10, +5 additive, ×0.5 multiplicative → (10+5) × 1.5 = 22.5.
     * </remarks>
     */
    public class StatSheet
    {
        private readonly Dictionary<StatId, float> _baseValues = new();
        private readonly Dictionary<StatId, List<StatModifier>> _modifiers = new();

        /** <summary>Sets the base value for a stat.</summary> */
        public void SetBase(StatId stat, float value) => _baseValues[stat] = value;

        /** <summary>Returns the base value for a stat, or 0 if not set.</summary> */
        public float GetBase(StatId stat) =>
            _baseValues.TryGetValue(stat, out var v) ? v : 0f;

        /** <summary>Adds a modifier that will be included in future evaluations.</summary> */
        public void AddModifier(StatId stat, StatModifier modifier)
        {
            if (!_modifiers.TryGetValue(stat, out var list))
            {
                list = new List<StatModifier>();
                _modifiers[stat] = list;
            }
            list.Add(modifier);
        }

        /** <summary>Removes a modifier. Does nothing if not found.</summary> */
        public void RemoveModifier(StatId stat, StatModifier modifier)
        {
            if (_modifiers.TryGetValue(stat, out var list))
                list.Remove(modifier);
        }

        /** <summary>Removes all modifiers from all stats.</summary> */
        public void ClearModifiers() => _modifiers.Clear();

        /**
         * <summary>
         * Computes the final value of a stat by applying all modifiers in deterministic order.
         * </summary>
         * <param name="stat">The stat to evaluate.</param>
         * <param name="context">Optional tag container for conditional modifier evaluation.</param>
         */
        public float Evaluate(StatId stat, ITagContainer context = null)
        {
            float baseVal = GetBase(stat);

            if (!_modifiers.TryGetValue(stat, out var mods) || mods.Count == 0)
                return baseVal;

            float additiveSum = 0f;
            float multiplicativeSum = 0f;
            float overrideValue = float.MinValue;
            int overridePriority = int.MinValue;
            bool hasOverride = false;

            foreach (var mod in mods)
            {
                if (!IsActive(mod, context)) continue;

                switch (mod.Type)
                {
                    case StatModifierType.Additive:
                        additiveSum += mod.Value;
                        break;
                    case StatModifierType.Multiplicative:
                        multiplicativeSum += mod.Value;
                        break;
                    case StatModifierType.Override:
                        if (!hasOverride || mod.Priority > overridePriority)
                        {
                            overrideValue = mod.Value;
                            overridePriority = mod.Priority;
                            hasOverride = true;
                        }
                        break;
                }
            }

            if (hasOverride) return overrideValue;
            return (baseVal + additiveSum) * (1f + multiplicativeSum);
        }

        /**
         * <summary>Returns a human-readable breakdown of stat evaluation for the debug overlay.</summary>
         */
        public string DescribeEvaluation(StatId stat, ITagContainer context = null)
        {
            float baseVal = GetBase(stat);
            var sb = new System.Text.StringBuilder();
            sb.Append($"{stat} | base={baseVal:F2}");

            if (!_modifiers.TryGetValue(stat, out var mods) || mods.Count == 0)
            {
                sb.Append($" → {baseVal:F2}");
                return sb.ToString();
            }

            foreach (var mod in mods)
            {
                if (!IsActive(mod, context)) continue;
                string condStr = mod.Condition.HasValue ? " [cond]" : string.Empty;
                string src = string.IsNullOrEmpty(mod.Source) ? string.Empty : $" ({mod.Source})";
                switch (mod.Type)
                {
                    case StatModifierType.Additive:
                        sb.Append($" +{mod.Value:F2}{condStr}{src}");
                        break;
                    case StatModifierType.Multiplicative:
                        sb.Append($" ×{(1f + mod.Value):F2}{condStr}{src}");
                        break;
                    case StatModifierType.Override:
                        sb.Append($" OVERRIDE={mod.Value:F2}[p={mod.Priority}]{condStr}{src}");
                        break;
                }
            }

            sb.Append($" → {Evaluate(stat, context):F2}");
            return sb.ToString();
        }

        private static bool IsActive(StatModifier mod, ITagContainer context)
        {
            if (context == null || !mod.Condition.HasValue) return true;
            return mod.Condition.Value.Matches(context);
        }
    }
}
