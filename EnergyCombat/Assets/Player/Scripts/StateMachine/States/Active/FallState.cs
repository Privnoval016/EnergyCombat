using StateMachine;

public class FallState : State<PlayerController>
{
    public FallState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsDodging) return host.GetState<DodgeState>();
            if (host.IsGrounded)
            {
                if (host.IsSliding) return host.GetState<SlideState>();
                if (host.ShouldSprint) return host.GetState<SprintState>();
                if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
                return host.GetState<IdleState>();
            }

            return null;
        }));
    }
}
