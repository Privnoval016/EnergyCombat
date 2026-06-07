using StateMachine;

public class MoveState : State<PlayerController>
{
    public MoveState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking) return host.GetState<AttackingState>();
            if (host.IsSliding) return host.GetState<SlideState>();
            if (host.IsDodging) return host.GetState<DashState>();
            if (host.IsQuickTurning) return host.GetState<QuickTurnState>();
            if (host.ShouldSprint) return host.GetState<SprintState>();
            if (!host.HasDirectionalMoveInput) return host.GetState<IdleState>();
            return null;
        }));
    }
}
