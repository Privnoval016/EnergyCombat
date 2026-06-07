using StateMachine;

public class JumpState : State<PlayerController>
{
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

            if (host.VerticalVelocity <= 0f) return host.GetState<FallState>();
            return null;
        }));
    }
}
