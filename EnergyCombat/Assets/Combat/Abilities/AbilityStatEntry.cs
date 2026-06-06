using System;

namespace Combat
{
    /**
     * <summary>
     * Associates a <see cref="StatId"/> with a base value on an <see cref="AbilityDefinition"/>.
     * This list is loaded into the execution's <c>StatSheet</c> at runtime.
     * </summary>
     */
    [Serializable]
    public class AbilityStatEntry
    {
        /** <summary>The stat this entry sets a base value for.</summary> */
        public StatId Stat;

        /** <summary>The base value of the stat before any modifiers are applied.</summary> */
        public float BaseValue;
    }
}
