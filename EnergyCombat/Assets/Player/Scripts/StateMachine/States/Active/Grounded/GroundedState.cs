using StateMachine;
using UnityEngine;

public class GroundedState : State<PlayerController>
{
    public GroundedState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (!host.IsGrounded)
            {
                return host.VerticalVelocity > 0.01f
                    ? host.GetState<JumpState>()
                    : host.GetState<FallState>();
            }

            if (host.ConsumeJumpPressed())
            {
                host.RequestJump();
                return host.GetState<JumpState>();
            }

            // Only consume the sprint press when the player is already moving; otherwise leave it
            // unconsumed so it fires automatically the moment directional input is detected.
            if (host.HasDirectionalMoveInput && host.ConsumeSprintPressed())
            {
                host.RequestSprint();
                return host.GetState<SprintState>();
            }

            if (host.ConsumeDodgePressed())
            {
                if (host.ShouldSlideFromDodgeIntent())
                {
                    host.RequestSlide();
                    return host.GetState<SlideState>();
                }

                // add functionality for combat dodge later
            }

            return null;
        }));
    }
}
