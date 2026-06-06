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

            if (host.IsGrounded)
            {
                if (host.ConsumeLandingBoostPending() && host.ShouldApplyPostBoost())
                {
                    host.RequestSprint();
                    return host.GetState<MoveState>();
                }

                if (host.IsSliding) return host.GetState<SlideState>();
                if (host.ShouldSprint) return host.GetState<SprintState>();
                if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
                return host.GetState<IdleState>();
            }

            return null;
        }));
    }
}
