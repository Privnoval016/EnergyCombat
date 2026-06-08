using StateMachine;
using UnityEngine;

/**
 * <summary>
 * Entered from <see cref="JumpState"/> and <see cref="FallState"/> whenever the character touches down.
 * Holds for <see cref="PlayerAnimationConfig.LandHoldDuration"/> seconds to let the landing animation
 * play before routing back to normal movement states.
 * </summary>
 *
 * <remarks>
 * Animation selection is state-based, not velocity-based:
 * <list type="bullet">
 *   <item><b>Walk landing</b> — no clip plays; routing to <see cref="MoveState"/> is an immediate
 *     priority exit (not gated by the hold timer).</item>
 *   <item><b>Idle landing</b> — <see cref="PlayerAnimationConfig.LandIdle"/> plays; hold timer runs.</item>
 *   <item><b>Sprint landing</b> — <see cref="PlayerAnimationConfig.LandMotion"/> plays; hold timer runs.</item>
 * </list>
 *
 * Other priority exits (boost, airborne again, attacking, dodging, sliding) always interrupt the hold.
 * A boost landing consumes the pending flag and skips the hold entirely.
 *
 * After the hold, sprint is restored when <see cref="PlayerController.ShouldSprint"/> is true on
 * landing (sprint toggle was on before the jump).
 * </remarks>
 */
public class LandState : State<PlayerController>
{
    private readonly float _holdDuration;
    private float _timer;

    /** <summary>Creates a new LandState with the specified animation hold duration in seconds.</summary> */
    public LandState(float holdDuration = 0f)
    {
        _holdDuration = holdDuration;

        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            // Boost landing: consume the flag immediately and skip the hold
            if (host.ConsumeLandingBoostPending() && host.ShouldApplyPostBoost())
            {
                host.RequestSprint();
                return host.GetState<MoveState>();
            }

            // Priority exits — always interrupt the hold
            if (host.IsAttacking) return host.GetState<AttackingState>();
            if (!host.IsGrounded)
            {
                return host.VerticalVelocity > 0.01f
                    ? host.GetState<JumpState>()
                    : host.GetState<FallState>();
            }
            if (host.IsDodging) return host.GetState<DashState>();
            if (host.IsSliding) return host.GetState<SlideState>();

            // Walking on landing: no custom clip needed, skip the hold and go straight to walk
            if (host.HasDirectionalMoveInput && !host.ShouldSprint) return host.GetState<MoveState>();

            // Idle and sprint landings are gated behind the hold timer
            if (_timer > 0f) return null;

            if (host.ShouldSprint)
            {
                host.RequestSprint();
                return host.GetState<SprintState>();
            }
            if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
            return host.GetState<IdleState>();
        }));
    }

    protected override void OnEnter() => _timer = _holdDuration;

    protected override void OnUpdate(float deltaTime)
    {
        if (_timer > 0f)
            _timer -= deltaTime;
    }
}
