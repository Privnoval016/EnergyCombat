using StateMachine;

public class IdleState : State<PlayerController>
{
    public IdleState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsSliding) return host.GetState<SlideState>();
            if (host.IsDodging) return host.GetState<DashState>();
            if (host.ShouldSprint) return host.GetState<SprintState>();
            if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
            return null;
        }));
    }
}