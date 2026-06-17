using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BladeMode.Input
{
    // Reads right-stick input and L/H button presses during blade mode.
    // Fires OnCutRequested when the player executes a cut.
    // PlayerBladeModeAdapter calls SetActive(true/false) on enter/exit.
    [DisallowMultipleComponent]
    public sealed class BladeModeInputHandler : MonoBehaviour
    {
        [SerializeField] InputActionReference _lightAction;
        [SerializeField] InputActionReference _heavyAction;
        [SerializeField] InputActionReference _rightStickAction;

        public event Action<CutType> OnCutRequested;
        public Vector2 RightStickValue { get; private set; }

        // Reads the stick directly from the InputAction, bypassing the per-Update cache.
        // Use this in callbacks that fire before BladeModeInputHandler.Update() has run.
        public Vector2 ReadStickImmediate() =>
            _active && _rightStickAction != null
                ? _rightStickAction.action.ReadValue<Vector2>()
                : Vector2.zero;

        bool _active;

        public void SetActive(bool active)
        {
            _active = active;
            if (!active) RightStickValue = Vector2.zero;
        }

        void OnEnable()
        {
            if (_lightAction != null)
            {
                _lightAction.action.Enable();
                _lightAction.action.started += OnLight;
            }
            if (_heavyAction != null)
            {
                _heavyAction.action.Enable();
                _heavyAction.action.started += OnHeavy;
            }
            // Explicitly enable the stick action — it may not be enabled by any other system
            // if assigned to a standalone action rather than the Player action map.
            _rightStickAction?.action.Enable();
        }

        void OnDisable()
        {
            if (_lightAction != null) _lightAction.action.started -= OnLight;
            if (_heavyAction != null) _heavyAction.action.started -= OnHeavy;
        }

        void Update()
        {
            if (!_active) return;
            if (_rightStickAction != null)
                RightStickValue = _rightStickAction.action.ReadValue<Vector2>();
        }

        void OnLight(InputAction.CallbackContext _)
        {
            if (_active) OnCutRequested?.Invoke(CutType.Horizontal);
        }

        void OnHeavy(InputAction.CallbackContext _)
        {
            if (_active) OnCutRequested?.Invoke(CutType.Vertical);
        }
    }
}
