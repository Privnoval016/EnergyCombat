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
 * Animation selection (idle vs. motion landing) is determined at activity activation time by reading
 * the horizontal velocity magnitude and comparing it to
 * <see cref="PlayerAnimationConfig.LandMotionSpeedThreshold"/>.
 *
 * Priority exits (landing boost, airborne again, attacking, dodging, sliding) are never blocked
 * by the hold timer. A boost landing consumes the pending flag and skips the hold entirely.
 *
 * After the hold, sprint is restored when the player was sprinting before the jump — even though
 * <c>IsSprinting</c> is cleared while airborne, the full-throttle check + <see cref="PlayerController.RequestSprint"/>
 * re-arms the sprint toggle so <see cref="SprintState"/> activates immediately.
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

            // Normal routing is gated behind the hold timer
            if (_timer > 0f) return null;

            // Re-arm sprint if the player was sprinting or is still pushing full throttle —
            // IsSprinting is cleared while airborne so we must call RequestSprint() explicitly here.
            if (host.ShouldSprint || host.IsFullThrottleMove)
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
