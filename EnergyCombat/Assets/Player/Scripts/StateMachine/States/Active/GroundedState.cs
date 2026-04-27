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
                Debug.Log("Jump requested");
                host.RequestJump();
                return host.GetState<JumpState>();
            }

            if (host.ConsumeDodgePressed())
            {
                if (host.ShouldSlideFromDodgeIntent())
                {
                    host.RequestSlide();
                    return host.GetState<SlideState>();
                }

                host.RequestDashFromMoveInput();
                return host.GetState<DodgeState>();
            }

            return null;
        }));
    }
}
