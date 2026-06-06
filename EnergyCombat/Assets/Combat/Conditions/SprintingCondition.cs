using System;
using Extensions.Logic;

namespace Combat
{
    /**
     * <summary>
     * Condition that passes only when the character is actively sprinting.
     * Use to gate sprint-cancel abilities (e.g. a dash-attack that only triggers
     * if the player is already at full speed).
     * </summary>
     */
    [Serializable]
    public class SprintingCondition : ICondition<CombatContext>
    {
        /** <inheritdoc /> */
        public bool Evaluate(CombatContext context)
        {
            var loco = context?.Controller?.GetComponent<ILocomotionState>();
            return loco == null || loco.IsSprinting;
        }
    }
}
