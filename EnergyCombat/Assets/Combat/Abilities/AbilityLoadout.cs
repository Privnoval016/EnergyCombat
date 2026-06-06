using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * ScriptableObject that defines which abilities are <em>active</em> for a specific
     * weapon, stance, or character build.
     * </summary>
     *
     * <remarks>
     * In a roguelike context, a character may carry multiple loadouts — one per weapon type.
     * Call <c>CombatController.EquipLoadout(loadout)</c> when the player picks up or swaps
     * a weapon to instantly change which abilities are available.
     *
     * Only abilities in <see cref="ActiveAbilities"/> are eligible to execute.
     * Combo follow-ups are accessed via <see cref="AbilityDefinition.FollowUpCombo"/> and
     * do not need to be listed here.
     *
     * For the full library of all known abilities, see <see cref="AbilityLibrary"/>.
     * </remarks>
     */
    [CreateAssetMenu(fileName = "New Ability Loadout", menuName = "Combat/Ability Loadout")]
    public class AbilityLoadout : ScriptableObject
    {
        /**
         * <summary>
         * Abilities active in this loadout. Listed in priority order — the first ability
         * whose input and conditions match will execute.
         * </summary>
         */
        [Tooltip("Active abilities for this weapon/loadout. Order matters: first match wins.")]
        public AbilityDefinition[] ActiveAbilities;
    }
}
