using StateMachine;
using UnityEngine;

/**
 * <summary>
 * Active while the character is walking. Transitions to Sprint, Idle, or priority action states.
 * A short grace period prevents the walk exit clip from firing during rapid direction changes:
 * Idle is only entered after directional input has been absent for <see cref="PlayerAnimationConfig.WalkIdleGracePeriod"/> seconds.
 * </summary>
 *
 * <remarks>
 * <see cref="TimeSinceExit"/> is readable by external systems (e.g. <c>LoopAnimActivity</c>) to decide
 * whether to skip the walk entry animation on rapid re-entry — so the enter clip only plays when the
 * player has genuinely been idle long enough to warrant it.
 * </remarks>
 */
public class MoveState : State<PlayerController>
{
    private float _noInputTimer;
    private float _lastExitTime = float.MinValue;

    /** <summary>Seconds elapsed since this state last exited. Used to skip walk enter on rapid re-entry.</summary> */
    public float TimeSinceExit => Time.time - _lastExitTime;

    public MoveState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking)    return host.GetState<AttackingState>();
            if (host.IsSliding)      return host.GetState<SlideState>();
            if (host.IsDodging)      return host.GetState<DashState>();
            if (host.IsQuickTurning) return host.GetState<QuickTurnState>();
            if (host.ShouldSprint)   return host.GetState<SprintState>();
            var gracePeriod = host.AnimationController?.Config?.WalkIdleGracePeriod ?? 0.15f;
            if (!host.HasDirectionalMoveInput && _noInputTimer >= gracePeriod)
                return host.GetState<IdleState>();
            return null;
        }));
    }

    protected override void OnEnter() => _noInputTimer = 0f;

    protected override void OnExit() => _lastExitTime = Time.time;

    protected override void OnUpdate(float dt)
    {
        if (!Host.HasDirectionalMoveInput)
            _noInputTimer += dt;
        else
            _noInputTimer = 0f;
    }
}
