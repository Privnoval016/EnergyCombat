using StateMachine;

/**
 * <summary>
 * Active while the character is grabbing and climbing a ledge.
 * Sibling of <see cref="GroundedState"/> and <see cref="AirborneState"/> under <c>ActiveState</c>.
 *
 * The <c>LedgeGrabAbility</c> handles all physics — this state simply mirrors the
 * <c>IsLedgeGrabbing</c> flag and hands off when the climb is complete.
 *
 * Transitions:
 * - → <see cref="GroundedState"/> when climb is done and character is grounded
 * - → <see cref="FallState"/> if the grab fails without landing (edge case)
 *
 * On exit (grounded): sets the landing boost pending flag.
 * </summary>
 */
public class LedgeGrabState : State<PlayerController>
{
    public LedgeGrabState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsLedgeGrabbing) return null;

            if (host.IsGrounded) return host.GetState<GroundedState>();
            return host.GetState<FallState>();
        }));
    }

    protected override void OnExit()
    {
        if (Host.IsGrounded && Host.ShouldApplyPostBoost())
            Host.RequestSprint();
    }
}
