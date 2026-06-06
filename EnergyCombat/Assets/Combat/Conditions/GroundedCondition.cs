using System;
using Extensions.Logic;

namespace Combat
{
    /**
     * <summary>
     * Condition that passes only when the character is grounded.
     * Use on aerial-only abilities that should be blocked on the ground.
     * Invert with <c>NotCondition&lt;CombatContext&gt;</c> to get "must be grounded".
     * </summary>
     *
     * <remarks>
     * Queries <see cref="ILocomotionState.IsGrounded"/> on
     * <c>context.Controller.GetComponent&lt;ILocomotionState&gt;()</c>.
     * If the controller doesn't implement <see cref="ILocomotionState"/>, returns true
     * so the condition degrades gracefully.
     * </remarks>
     */
    [Serializable]
    public class GroundedCondition : ICondition<CombatContext>
    {
        /** <inheritdoc /> */
        public bool Evaluate(CombatContext context)
        {
            var loco = context?.Controller?.GetComponent<ILocomotionState>();
            return loco == null || loco.IsGrounded;
        }
    }
}
