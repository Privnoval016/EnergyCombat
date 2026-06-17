using System.Threading;
using BladeMode.Core;
using BladeMode.Input;
using Combat;
using Cysharp.Threading.Tasks;
using Systems.Input;
using UnityEngine;

namespace BladeMode.Integration
{
    /// <summary>
    /// Bridges PlayerInputAdapter ↔ BladeModeController.
    /// Handles entry/exit gate (combat input suppression, Animancer layer) without
    /// touching the motion state machine — blade mode is a non-exclusive overlay.
    /// Requires a PlayerController on this or a parent GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerBladeModeAdapter : MonoBehaviour
    {
        [SerializeField] BladeModeController _bladeMode;
        [SerializeField] AvatarMask          _upperBodyMask;
        [SerializeField] CameraController    _cameraController;

        PlayerController          _player;
        PlayerInputAdapter        _inputAdapter;
        CombatController          _combat;
        PlayerAnimationController _anim;

        CancellationTokenSource _cts;
        bool _bladeCameraSwitched;

        // Start (not Awake) so all Awakes — including PlayerController.InitializeInputAndOrchestration —
        // have run before we try to read InputAdapter.
        void Start()
        {
            _player       = GetComponentInParent<PlayerController>();
            _inputAdapter = _player?.InputAdapter;
            _combat       = _player?.CombatController;
            _anim         = _player?.AnimationController;
            if (_cameraController == null)
                _cameraController = _player?.CameraController;

            if (_inputAdapter == null)
            {
                UnityEngine.Debug.LogWarning("[PlayerBladeModeAdapter] Could not find PlayerInputAdapter. Blade mode input will not work.", this);
                return;
            }

            _inputAdapter.OnBladeModeStarted  += HandleEnter;
            _inputAdapter.OnBladeModeEnded    += HandleExit;
            // Route L/H through PlayerInputAdapter so BladeModeInputHandler inspector
            // assignments are not required — PlayerInput already has these actions wired.
            _inputAdapter.OnBladeLightAttack  += OnBladeLightCut;
            _inputAdapter.OnBladeHeavyAttack  += OnBladeHeavyCut;
        }

        void OnDestroy()
        {
            if (_inputAdapter == null) return;
            _inputAdapter.OnBladeModeStarted  -= HandleEnter;
            _inputAdapter.OnBladeModeEnded    -= HandleExit;
            _inputAdapter.OnBladeLightAttack  -= OnBladeLightCut;
            _inputAdapter.OnBladeHeavyAttack  -= OnBladeHeavyCut;
        }

        void OnBladeLightCut()  => _bladeMode?.RequestCut(CutType.Horizontal);
        void OnBladeHeavyCut() => _bladeMode?.RequestCut(CutType.Vertical);

        void HandleEnter()
        {
            if (_bladeMode.IsActive) return;

            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            // Cancel any in-progress attack — locomotion (jump/sprint/slide/wall-run) continues.
            if (_combat != null && _combat.IsExecuting)
                _combat.CancelCurrentAbility();

            // Suppress L/H combat routing.
            _inputAdapter?.SetCombatAttackInputEnabled(false);

            // Zero out movement input so the player can't steer while aiming the cut plane.
            // Existing velocity (jump arc, sprint momentum, slide, wall-run) carries and
            // decelerates naturally under the slowed time scale — no abrupt momentum kill.
            _inputAdapter?.SetLocomotionEnabled(false);

            // Reduce camera sensitivity so aiming the cut plane is deliberate, not twitchy.
            _inputAdapter?.SetLookMultiplier(_bladeMode?.Settings?.BladeCameraLookMultiplier ?? 0.35f);

            // Camera runs at real-time speed so it doesn't lag during time dilation.
            if (_cameraController?.brain != null)
                _cameraController.brain.IgnoreTimeScale = true;

            // Track whether we switched so ExitAsync only resumes if we actually pushed a camera.
            _bladeCameraSwitched = _cameraController != null;
            _cameraController?.SwitchCamera(CamMode.BladeMode);

            // Layer 1 upper-body override with avatar mask.
            if (_anim != null && _upperBodyMask != null)
            {
                var layer1 = _anim.GetLayer(1);
                layer1.Mask = _upperBodyMask;
            }

            _bladeMode.EnterAsync(_cts.Token).Forget();
        }

        void HandleExit()
        {
            _cts?.Cancel();
            ExitAsync().Forget();
        }

        async UniTaskVoid ExitAsync()
        {
            if (_bladeMode == null) return;
            await _bladeMode.ExitAsync();
            if (this == null) return;

            _inputAdapter?.SetCombatAttackInputEnabled(true);
            _inputAdapter?.SetLocomotionEnabled(true);
            _inputAdapter?.SetLookMultiplier(1f);

            // Only resume the camera stack if we actually pushed a blade mode camera on entry.
            if (_bladeCameraSwitched)
            {
                _cameraController?.ResumeLastCamera();
                _bladeCameraSwitched = false;
            }

            if (_cameraController?.brain != null)
                _cameraController.brain.IgnoreTimeScale = false;

            // Fade out Layer 1 — Layer 0 locomotion was unaffected throughout.
            _anim?.GetLayer(1).StartFade(0f, 0.25f);
        }
    }
}
