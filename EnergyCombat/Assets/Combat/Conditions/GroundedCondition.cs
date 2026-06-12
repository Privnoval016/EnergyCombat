using System;
using Extensions.Logic;

namespace Combat
{
    /**
     * <summary>
     * Condition that passes only when the character is grounded <em>and</em> in a normal
     * locomotion state — walking, sprinting, or idle. Slides, wall-runs, ledge-grabs,
     * wall-kicks, and dodges are excluded so abilities gated on this condition cannot be
     * triggered during those special movement states.
     * </summary>
     *
     * <remarks>
     * Queries <see cref="ILocomotionState"/> on
     * <c>context.Controller.GetComponent&lt;ILocomotionState&gt;()</c>.
     * If the controller doesn't implement <see cref="ILocomotionState"/>, returns true
     * so the condition degrades gracefully.
     *
     * Use <see cref="TouchingGroundCondition"/> when you only need the raw grounded check
     * without filtering out special movement states.
     * </remarks>
     */
    [Serializable]
    public class GroundedCondition : ICondition<CombatContext>
    {
        /** <inheritdoc /> */
        public bool Evaluate(CombatContext context)
        {
            var loco = context?.Controller?.GetComponent<ILocomotionState>();
            if (loco == null) return true;
            return loco.IsGrounded
                && !loco.IsSliding
                && !loco.IsWallRunning
                && !loco.IsLedgeGrabbing
                && !loco.IsWallKicking
                && !loco.IsDodging;
        }
    }
}
