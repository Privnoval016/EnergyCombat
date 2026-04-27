using StateMachine;

public class AirborneState : State<PlayerController>
{
    public AirborneState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.ConsumeDodgePressed())
            {
                host.RequestDashFromMoveInput();
                return host.GetState<DodgeState>();
            }

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
