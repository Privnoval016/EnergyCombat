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
        [UnityEngine.Tooltip("Which stat this row sets (e.g. Damage, Range, Knockback, AttackSpeed). Each stat can appear at most once per ability.")]
        public StatId Stat;

        /** <summary>The base value of the stat before any modifiers are applied.</summary> */
        [UnityEngine.Tooltip("Starting value loaded into the stat sheet at execution time. Equipped item modifiers and buffs are layered on top of this.")]
        public float BaseValue;
    }
}
