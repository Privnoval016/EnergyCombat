using StateMachine;

public class SlideState : State<PlayerController>
{
    public SlideState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (!host.IsSliding)
            {
                if (host.ShouldSprint) return host.GetState<SprintState>();
                if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
                return host.GetState<IdleState>();
            }

            return null;
        }));
    }
}
