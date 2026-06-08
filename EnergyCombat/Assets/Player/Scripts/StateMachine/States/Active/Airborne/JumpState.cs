using StateMachine;

/**
 * <summary>
 * Airborne state active during the ascent phase of a jump.
 * Registered in the exit-skip policy so no exit clip plays from the preceding state while the
 * character is already mid-air.
 * </summary>
 */
public class JumpState : State<PlayerController>
{
    /** <summary>Minimum seconds in the jump state before the Fall transition is allowed.</summary> */
    private const float MinFallDelay = 0.15f;

    private float _timeInState;

    protected override void OnEnter() => _timeInState = 0f;
    protected override void OnUpdate(float dt) => _timeInState += dt;

    public JumpState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking) return host.GetState<AttackingState>();
            if (host.IsDodging) return host.GetState<DashState>();
            if (host.IsWallRunning) return host.GetState<WallRunState>();
            if (host.IsLedgeGrabbing) return host.GetState<LedgeGrabState>();
            if (host.IsWallKicking) return host.GetState<WallKickState>();

            if (host.ConsumeJumpPressed()) host.RequestJump();

            if (host.IsGrounded && host.VerticalVelocity <= 0.01f) return host.GetState<LandState>();

            // Guard prevents apex oscillation from triggering rapid Jump↔Fall replays
            if (host.VerticalVelocity <= 0f && _timeInState >= MinFallDelay)
                return host.GetState<FallState>();

            return null;
        }));
    }
}
