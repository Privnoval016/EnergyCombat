using StateMachine;

public class SprintState : State<PlayerController>
{
    public SprintState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsSliding) return host.GetState<SlideState>();
            if (host.IsDodging) return host.GetState<DodgeState>();
            if (!host.ShouldSprint)
            {
                return host.HasDirectionalMoveInput
                    ? host.GetState<MoveState>()
                    : host.GetState<IdleState>();
            }

            return null;
        }));
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (!Host.IsFullThrottleMove)
        {
            Host.CancelSprintToggle();
        }
    }
}
