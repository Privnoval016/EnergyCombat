using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.Input
{
    public sealed class PlayerInputAdapter : IPlayerInputSource, PlayerInput.IPlayerActions, IDisposable
    {
        private readonly PlayerInput _input;
        private PlayerInputSnapshot _snapshot;
        private bool _enabled;
        private bool _disposed;

        public event Action<PlayerInputButtonEvent> ButtonEvent;

        public PlayerInputSnapshot Snapshot => _snapshot;

        public PlayerInputAdapter()
        {
            _input = new PlayerInput();
            _input.Player.SetCallbacks(this);
        }

        public void Enable()
        {
            if (_disposed || _enabled)
            {
                return;
            }

            _enabled = true;
            _input.Player.Enable();
        }

        public void Disable()
        {
            if (!_enabled)
            {
                return;
            }

            _enabled = false;
            _input.Player.Disable();
            _snapshot.Move = Vector2.zero;
            _snapshot.Look = Vector2.zero;
            _snapshot.JumpHeld = false;
            _snapshot.SprintToggled = false;
        }

        public void CancelSprintToggle()
        {
            _snapshot.SprintToggled = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Disable();
            _input.Player.SetCallbacks(null);
            _input.Dispose();
            _disposed = true;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _snapshot.Move = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            _snapshot.Look = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed)
            {
                Debug.Log("Jump input started/performed. Setting JumpHeld to true.");
                _snapshot.JumpHeld = true;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                Debug.Log("Jump input canceled. Setting JumpHeld to false.");
                _snapshot.JumpHeld = false;
            }

            EmitButtonEvent(PlayerInputButton.Jump, context.phase);
        }

        public void OnDodge(InputAction.CallbackContext context)
        {
            EmitButtonEvent(PlayerInputButton.Dodge, context.phase);
        }

        public void OnLightAttack(InputAction.CallbackContext context)
        {
            EmitButtonEvent(PlayerInputButton.LightAttack, context.phase);
        }

        public void OnHeavyAttack(InputAction.CallbackContext context)
        {
            EmitButtonEvent(PlayerInputButton.HeavyAttack, context.phase);
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            EmitButtonEvent(PlayerInputButton.Crouch, context.phase);
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                _snapshot.SprintToggled = !_snapshot.SprintToggled;
            }

            EmitButtonEvent(PlayerInputButton.Sprint, context.phase);
        }

        private void EmitButtonEvent(PlayerInputButton button, InputActionPhase phase)
        {
            if (phase == InputActionPhase.Waiting || phase == InputActionPhase.Disabled)
            {
                return;
            }

            ButtonEvent?.Invoke(new PlayerInputButtonEvent(button, ConvertPhase(phase)));
        }

        private static PlayerInputPhase ConvertPhase(InputActionPhase phase)
        {
            return phase switch
            {
                InputActionPhase.Started => PlayerInputPhase.Started,
                InputActionPhase.Performed => PlayerInputPhase.Performed,
                InputActionPhase.Canceled => PlayerInputPhase.Canceled,
                _ => PlayerInputPhase.Performed
            };
        }
    }
}
