using StateMachine;

/**
 * <summary>
 * Active while the character is sprinting. Transitions to Move, Idle, or priority action states.
 * A short grace period prevents the sprint exit clip from firing during rapid direction changes:
 * the sprint→move/idle transition only commits after <see cref="PlayerAnimationConfig.SprintIdleGracePeriod"/>
 * seconds of sustained non-sprint conditions.
 * </summary>
 */
public class SprintState : State<PlayerController>
{
    private float _noSprintTimer;

    public SprintState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking) return host.GetState<AttackingState>();
            if (host.IsSliding)   return host.GetState<SlideState>();
            if (host.IsDodging)   return host.GetState<DashState>();
            if (host.IsQuickTurning) return host.GetState<QuickTurnState>();

            var gracePeriod = host.AnimationController?.Config?.SprintIdleGracePeriod ?? 0.1f;
            if (!host.ShouldSprint && _noSprintTimer >= gracePeriod)
            {
                return host.HasDirectionalMoveInput
                    ? host.GetState<MoveState>()
                    : host.GetState<IdleState>();
            }

            return null;
        }));
    }

    protected override void OnEnter() => _noSprintTimer = 0f;

    protected override void OnUpdate(float dt)
    {
        if (!Host.ShouldSprint)
            _noSprintTimer += dt;
        else
            _noSprintTimer = 0f;
    }
}
