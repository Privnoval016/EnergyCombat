using StateMachine;

/**
 * <summary>
 * Entered from <see cref="MoveState"/> and <see cref="SprintState"/> when the physics layer
 * detects a hard-stop momentum reversal (<see cref="MotionTag.QuickTurning"/>).
 * </summary>
 *
 * <remarks>
 * Animation selection (left vs. right turn) is determined at activity activation time by reading
 * <see cref="PlayerController.QuickTurnSign"/>: negative = left, positive = right.
 *
 * The state stays active as long as <see cref="PlayerController.IsQuickTurning"/> is true.
 * When the tag clears the state routes back to sprint, move, or idle depending on current input.
 * Priority exits (attacking, airborne, dodging, sliding) interrupt the animation at any time.
 * </remarks>
 */
public class QuickTurnState : State<PlayerController>
{
    public QuickTurnState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            // Priority exits
            if (host.IsAttacking) return host.GetState<AttackingState>();
            if (!host.IsGrounded)
            {
                return host.VerticalVelocity > 0.01f
                    ? host.GetState<JumpState>()
                    : host.GetState<FallState>();
            }
            if (host.IsDodging) return host.GetState<DashState>();
            if (host.IsSliding) return host.GetState<SlideState>();

            // Return to movement once the quick-turn tag expires
            if (!host.IsQuickTurning)
            {
                if (host.ShouldSprint) return host.GetState<SprintState>();
                if (host.HasDirectionalMoveInput) return host.GetState<MoveState>();
                return host.GetState<IdleState>();
            }

            return null;
        }));
    }
}
