using StateMachine;

/**
 * <summary>
 * Active for the entire upward arc of a wall kick (while <c>IsWallKicking</c> is true).
 * Sibling of <see cref="WallRunState"/> and <see cref="LedgeGrabState"/> under <c>ActiveState</c>.
 *
 * Entry: any airborne state (FallState, JumpState, WallRunState) routes here when
 * <c>IsWallKicking</c> becomes true.
 *
 * The <see cref="MotionTag.WallKicking"/> tag lives until vertical velocity reaches 0 or the
 * player lands — not just one FixedUpdate tick. This makes the state robust to multiple
 * FixedUpdates firing between Update frames (low framerate).
 *
 * On exit: sets the landing boost pending flag so a sprint burst fires on the next grounded landing.
 * </summary>
 */
public class WallKickState : State<PlayerController>
{
    public WallKickState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking) return host.GetState<AttackingState>();
            if (host.IsLedgeGrabbing) return host.GetState<LedgeGrabState>();
            if (host.IsWallKicking) return null; // arc still in progress — stay

            if (host.IsGrounded) return host.GetState<GroundedState>();
            return host.GetState<FallState>(); // arc ended, descending
        }));
    }

    protected override void OnExit()
    {
        Host.SetLandingBoostPending();
    }
}
