using StateMachine;

public class FallState : State<PlayerController>
{
    public FallState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking) return host.GetState<AttackingState>();
            if (host.IsDodging) return host.GetState<DashState>();
            if (host.IsWallRunning) return host.GetState<WallRunState>();
            if (host.IsLedgeGrabbing) return host.GetState<LedgeGrabState>();
            if (host.IsWallKicking) return host.GetState<WallKickState>();

            if (host.ConsumeJumpPressed()) host.RequestJump();

            if (host.IsGrounded) return host.GetState<LandState>();

            return null;
        }));
    }
}
