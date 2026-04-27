using StateMachine;

public class DodgeState : State<PlayerController>
{
    public DodgeState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsDodging)
            {
                return null;
            }

            if (!host.IsGrounded)
            {
                return host.VerticalVelocity > 0f
                    ? host.GetState<JumpState>()
                    : host.GetState<FallState>();
            }

            if (host.IsSliding) return host.GetState<SlideState>();
            if (host.ShouldSprint) return host.GetState<SprintState>();
            if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
            return host.GetState<IdleState>();
        }));
    }
}
