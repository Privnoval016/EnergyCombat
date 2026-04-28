using StateMachine;

public class JumpState : State<PlayerController>
{
    public JumpState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsDodging) return host.GetState<DashState>();
            if (host.IsGrounded && host.VerticalVelocity <= 0.01f)
            {
                if (host.IsSliding) return host.GetState<SlideState>();
                if (host.ShouldSprint) return host.GetState<SprintState>();
                if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
                return host.GetState<IdleState>();
            }

            if (host.VerticalVelocity <= 0f) return host.GetState<FallState>();
            return null;
        }));
    }
}
