using System;
using Extensions.Logic;

namespace Combat
{
    /**
     * <summary>
     * Condition that passes whenever the character is physically touching a walkable surface,
     * regardless of what movement state they are in (sliding, wall-kicking, etc.).
     * </summary>
     *
     * <remarks>
     * This is the raw grounded check: it mirrors <see cref="ILocomotionState.IsGrounded"/>
     * directly. Use it when you want an ability available any time the character has ground
     * contact, without excluding special movement states.
     *
     * For a stricter check that excludes slides, wall-runs, ledge-grabs, wall-kicks, and
     * dodges, use <see cref="GroundedCondition"/> instead.
     *
     * If the controller doesn't implement <see cref="ILocomotionState"/>, returns true so
     * the condition degrades gracefully.
     * </remarks>
     */
    [Serializable]
    public class TouchingGroundCondition : ICondition<CombatContext>
    {
        /** <inheritdoc /> */
        public bool Evaluate(CombatContext context)
        {
            var loco = context?.Controller?.GetComponent<ILocomotionState>();
            return loco == null || loco.IsGrounded;
        }
    }
}
