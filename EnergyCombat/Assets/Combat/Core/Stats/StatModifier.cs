using System;

namespace Combat
{
    /**
     * <summary>
     * Describes how a single modifier contributes to the evaluation of one stat.
     * </summary>
     *
     * <remarks>
     * Evaluation order (fixed):
     * <list type="number">
     * <item><description>Base value.</description></item>
     * <item><description>Sum all Additive modifiers.</description></item>
     * <item><description>Multiply by (1 + sum of Multiplicative values).</description></item>
     * <item><description>Apply highest-priority Override if any exist.</description></item>
     * </list>
     * </remarks>
     */
    [Serializable]
    public class StatModifier
    {
        /** <summary>How this modifier contributes to the stat evaluation.</summary> */
        public StatModifierType Type;

        /**
         * <summary>
         * The modifier value.
         * Additive: added to the running total. Multiplicative: total × (1 + Value).
         * Override: replaces the final value if this has the highest priority.
         * </summary>
         */
        public float Value;

        /** <summary>Used to order Override modifiers. The highest-priority override wins.</summary> */
        public int Priority;

        /**
         * <summary>
         * Optional tag condition. If set, this modifier only applies when the execution
         * context satisfies the query.
         * </summary>
         */
        public TagQuery? Condition;

        /** <summary>Human-readable label shown in the debug overlay.</summary> */
        public string Source;

        /** <summary>Creates an additive modifier that shifts the stat by a flat amount.</summary> */
        public static StatModifier Additive(float value, string source = null,
            TagQuery? condition = null) =>
            new StatModifier { Type = StatModifierType.Additive, Value = value, Source = source, Condition = condition };

        /** <summary>Creates a multiplicative modifier. A value of 0.1 means +10%.</summary> */
        public static StatModifier Multiplicative(float value, string source = null,
            TagQuery? condition = null) =>
            new StatModifier { Type = StatModifierType.Multiplicative, Value = value, Source = source, Condition = condition };

        /** <summary>Creates an override modifier that replaces the final computed value.</summary> */
        public static StatModifier Override(float value, int priority = 0, string source = null) =>
            new StatModifier { Type = StatModifierType.Override, Value = value, Priority = priority, Source = source };
    }

    /** <summary>The category of a <see cref="StatModifier"/>.</summary> */
    public enum StatModifierType
    {
        /** <summary>Added to the base value as a flat bonus.</summary> */
        Additive,

        /** <summary>Scales the additive total. Applied as total × (1 + value).</summary> */
        Multiplicative,

        /** <summary>Replaces the final computed value. Highest-priority override wins.</summary> */
        Override
    }
}
