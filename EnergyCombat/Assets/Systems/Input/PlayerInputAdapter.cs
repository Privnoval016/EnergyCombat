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
        public event Action OnBladeModeStarted;
        public event Action OnBladeModeEnded;
        // Fires on L/H press specifically when combat is suppressed (i.e. blade mode is active).
        public event Action OnBladeLightAttack;
        public event Action OnBladeHeavyAttack;

        public bool CombatAttackInputEnabled { get; private set; } = true;
        public void SetCombatAttackInputEnabled(bool value) => CombatAttackInputEnabled = value;

        public bool LocomotionEnabled { get; private set; } = true;
        public void SetLocomotionEnabled(bool value)
        {
            LocomotionEnabled = value;
            if (!value)
            {
                _snapshot.Move = Vector2.zero;
                _snapshot.JumpHeld = false;
            }
        }

        public float LookMultiplier { get; private set; } = 1f;
        public void SetLookMultiplier(float value) => LookMultiplier = value;

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
            if (!LocomotionEnabled) { _snapshot.Move = Vector2.zero; return; }
            _snapshot.Move = context.ReadValue<Vector2>().normalized;
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            _snapshot.Look = context.ReadValue<Vector2>().normalized * LookMultiplier;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!LocomotionEnabled) { _snapshot.JumpHeld = false; return; }
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
            if (!LocomotionEnabled) return;
            EmitButtonEvent(PlayerInputButton.Dodge, context.phase);
        }

        public void OnLightAttack(InputAction.CallbackContext context)
        {
            if (CombatAttackInputEnabled)
                EmitButtonEvent(PlayerInputButton.LightAttack, context.phase);
            else if (context.phase == InputActionPhase.Started)
                OnBladeLightAttack?.Invoke();
        }

        public void OnHeavyAttack(InputAction.CallbackContext context)
        {
            if (CombatAttackInputEnabled)
                EmitButtonEvent(PlayerInputButton.HeavyAttack, context.phase);
            else if (context.phase == InputActionPhase.Started)
                OnBladeHeavyAttack?.Invoke();
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (!LocomotionEnabled) return;
            // if (context.phase == InputActionPhase.Performed)
            // {
            //     _snapshot.SprintToggled = !_snapshot.SprintToggled;
            // }

            EmitButtonEvent(PlayerInputButton.Sprint, context.phase);
        }

        public void OnBladeMode(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)  OnBladeModeStarted?.Invoke();
            if (context.phase == InputActionPhase.Canceled) OnBladeModeEnded?.Invoke();
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
