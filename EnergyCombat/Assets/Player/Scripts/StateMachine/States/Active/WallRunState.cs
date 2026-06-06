using StateMachine;

/**
 * <summary>
 * Active while the character is running along a wall.
 * Sibling of <see cref="GroundedState"/> and <see cref="AirborneState"/> under <c>ActiveState</c>.
 *
 * Transitions:
 * - → <see cref="AttackingState"/> on attack input
 * - → <see cref="GroundedState"/> if the character lands
 * - → <see cref="FallState"/> when wall contact is lost or the timer expires
 *
 * On exit: sets the landing boost pending flag so a dash boost fires on the next landing.
 * </summary>
 */
public class WallRunState : State<PlayerController>
{
    public WallRunState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking) return host.GetState<AttackingState>();
            // Queue the jump request; WallKickAbility processes it next FixedUpdate.
            // Do NOT immediately return WallKickState — the tag isn't set until physics runs.
            if (host.ConsumeJumpPressed()) host.RequestJump();
            if (host.IsWallKicking) return host.GetState<WallKickState>();
            if (host.IsGrounded) return host.GetState<GroundedState>();
            if (!host.IsWallRunning) return host.GetState<FallState>();
            return null;
        }));
    }

    protected override void OnExit()
    {
        /* Flag a landing boost so the dash fires when the player touches down. */
        Host.SetLandingBoostPending();
    }
}
