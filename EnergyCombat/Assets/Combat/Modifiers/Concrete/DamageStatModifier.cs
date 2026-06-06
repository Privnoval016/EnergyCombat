using System;

namespace Combat
{
    /**
     * <summary>
     * A modifier that contributes a <see cref="StatModifier"/> to the <c>Damage</c> stat
     * on the execution's <see cref="StatSheet"/> each time it is applied.
     * </summary>
     *
     * <remarks>
     * This is the primary mechanism for items, buffs, and roguelike upgrades to affect
     * attack damage. Combine multiple instances with different <see cref="StatModifierType"/>
     * values to stack additive bonuses, multiplicative scalers, and override effects.
     * </remarks>
     */
    public class DamageStatModifier : ICombatModifier, IStatContributor
    {
        private readonly StatModifier _modifier;

        /** <summary>Creates a damage stat modifier.</summary> */
        public DamageStatModifier(StatModifier modifier)
        {
            _modifier = modifier;
        }

        /** <inheritdoc /> */
        public Tag[] Tags => Array.Empty<Tag>();

        /** <inheritdoc /> */
        public bool IsExpired => false;

        /** <inheritdoc /> */
        public string DebugLabel =>
            $"Damage {_modifier.Type} {_modifier.Value:+0.##;-0.##;0}" +
            (string.IsNullOrEmpty(_modifier.Source) ? string.Empty : $" ({_modifier.Source})");

        /** <inheritdoc /> */
        public void ContributeStats(StatSheet stats, CombatContext context)
        {
            stats.AddModifier(StatId.Damage, _modifier);
        }
    }
}
