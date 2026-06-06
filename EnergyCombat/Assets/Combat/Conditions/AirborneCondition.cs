using System;
using Extensions.Logic;

namespace Combat
{
    /**
     * <summary>
     * Condition that passes only when the character is airborne (not grounded).
     * Use on air-only abilities like aerial combos or air dashes.
     * </summary>
     *
     * <remarks>
     * Queries <see cref="ILocomotionState.IsAirborne"/> on the controller.
     * Degrades gracefully — returns true if <see cref="ILocomotionState"/> is not found.
     * </remarks>
     */
    [Serializable]
    public class AirborneCondition : ICondition<CombatContext>
    {
        /** <inheritdoc /> */
        public bool Evaluate(CombatContext context)
        {
            var loco = context?.Controller?.GetComponent<ILocomotionState>();
            return loco == null || loco.IsAirborne;
        }
    }
}
