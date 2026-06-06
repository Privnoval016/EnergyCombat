using StateMachine;

/**
 * <summary>
 * Active state entered whenever <see cref="PlayerController.IsAttacking"/> is true.
 * Sits as a sibling to <c>GroundedState</c> and <c>AirborneState</c> under <c>ActiveState</c>,
 * so it can be reached from any locomotion leaf state and returns to the appropriate
 * locomotion state when the ability finishes.
 * </summary>
 *
 * <remarks>
 * This state does NOT drive combat logic — that responsibility belongs entirely to
 * <c>CombatController</c>. It exists solely to anchor the player state machine to the
 * correct visual and input context while an ability is executing.
 *
 * On entry: locomotion transitions (sprint, slide, dash) are still evaluated by
 * <c>MotionOrchestrator</c> in <c>FixedUpdate</c> — movement physics continue normally.
 * On exit: transitions back to the grounded or airborne tree based on current context.
 * </remarks>
 */
public class AttackingState : State<PlayerController>
{
    public AttackingState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            /* Stay in this state while an ability is executing. */
            if (host.IsAttacking)
            {
                host.ConsumeJumpPressed(); // discard — no jump buffering during attacks
                return null;
            }

            /* Return to the appropriate locomotion branch. */
            if (host.IsGrounded)
            {
                if (host.IsSliding) return host.GetState<SlideState>();
                if (host.ShouldSprint) return host.GetState<SprintState>();
                if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
                return host.GetState<IdleState>();
            }

            return host.VerticalVelocity > 0f
                ? host.GetState<JumpState>()
                : host.GetState<FallState>();
        }));
    }
}
