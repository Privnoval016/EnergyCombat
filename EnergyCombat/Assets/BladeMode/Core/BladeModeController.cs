using System;
using System.Collections.Generic;
using System.Threading;
using BladeMode.Animation;
using BladeMode.BodyParts;
using BladeMode.Effects;
using BladeMode.Input;
using BladeMode.Slicing;
using BladeMode.Visualization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BladeMode.Core
{
    /// <summary>
    /// Orchestrates blade mode: time dilation, arm IK, cut-plane visualization, and cut execution.
    /// Does NOT own any state machine state — it is a non-exclusive overlay attached to the player.
    /// Player locomotion continues uninterrupted; only the arms and L/H input routing change.
    /// </summary>
    public sealed class BladeModeController : MonoBehaviour
    {
        [SerializeField] BladeModeSettings          _settings;
        [SerializeField] BladeModeArmDriver         _armDriver;
        [SerializeField] BladeModeInputHandler      _input;
        [SerializeField] BladeModeEffectCoordinator _effects;
        [Tooltip("Camera or character transform used for stick-relative cut directions. Falls back to this transform if unassigned.")]
        [SerializeField] Transform                  _directionReference;

        public event Action<BladeModeState> OnStateChanged;
        public BladeModeState State { get; private set; } = BladeModeState.Inactive;
        public Vector3 CurrentCutNormal { get; private set; }
        public bool IsActive => State != BladeModeState.Inactive;
        public BladeModeSettings Settings => _settings;

        readonly SlicerService       _slicer  = new();
        readonly CutPlaneVisualizer  _planeViz = new();
        CutDirectionResolver         _directionResolver;
        CancellationTokenSource      _modeCts;

        void Awake() =>
            _directionResolver = new CutDirectionResolver(
                _directionReference != null ? _directionReference : transform,
                _settings != null && _settings.InvertStickRotation);

        void OnDestroy() => _planeViz.Dispose();

        // ── Entry / Exit (called by PlayerBladeModeAdapter) ───────────────────────

        public async UniTask EnterAsync(CancellationToken externalToken)
        {
            if (IsActive) return;
            _modeCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalToken, destroyCancellationToken);
            var token = _modeCts.Token;

            SetState(BladeModeState.Entering);

            await UniTask.WhenAll(
                LerpTimeScaleAsync(_settings.TimeScale, _settings.EnterDuration, token),
                _armDriver.AnimateWeightAsync(1f, _settings.EnterDuration, token));

            if (token.IsCancellationRequested) { CleanupEntry(); return; }

            // Default to a horizontal plane (normal = up), matching the Light cut.
            CurrentCutNormal = Vector3.up;
            _planeViz.Show(CutPlaneOrigin, CurrentCutNormal, _settings);
            _armDriver.SetCutNormal(CurrentCutNormal);

            _input.SetActive(true);
            _input.OnCutRequested += HandleCutRequest;

            _effects?.OnEnter();
            SetState(BladeModeState.Active);

            // Per-frame update loop: track right stick, rotate cut plane.
            // PreLateUpdate runs after all MonoBehaviour.Update() calls, so RightStickValue
            // in BladeModeInputHandler is always current (not one frame stale).
            // Loop runs through both Active and Executing states — only the _modeCts token
            // (cancelled by ExitAsync) should stop it. Stopping on Executing would kill the
            // loop permanently the moment the first cut fires.
            while (!token.IsCancellationRequested)
            {
                UpdateCutDirection();
                await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, token);
            }
        }

        public async UniTask ExitAsync()
        {
            if (!IsActive) return;
            SetState(BladeModeState.Exiting);

            _input.SetActive(false);
            _input.OnCutRequested -= HandleCutRequest;
            _planeViz.Hide();
            _effects?.OnExit();

            _modeCts?.Cancel();

            await UniTask.WhenAll(
                LerpTimeScaleAsync(1f, _settings.ExitDuration, destroyCancellationToken),
                _armDriver.AnimateWeightAsync(0f, _settings.ExitDuration, destroyCancellationToken));

            if (this == null) return;
            SetState(BladeModeState.Inactive);
        }

        // ── Cut execution ─────────────────────────────────────────────────────────

        // Called by PlayerBladeModeAdapter when L/H fires through PlayerInputAdapter.
        public void RequestCut(CutType type) => HandleCutRequest(type);

        void HandleCutRequest(CutType type)
        {
            if (State != BladeModeState.Active) return;
            // Read directly from the action — RightStickValue is 1-frame stale here because
            // this callback fires during InputSystem processing, before BladeModeInputHandler.Update().
            float deadzoneSq = _settings.JoystickDeadzone * _settings.JoystickDeadzone;
            if (_input.ReadStickImmediate().sqrMagnitude > deadzoneSq)
                type = CutType.Joystick;
            ExecuteCutAsync(type, destroyCancellationToken).Forget();
        }

        async UniTask ExecuteCutAsync(CutType type, CancellationToken token)
        {
            SetState(BladeModeState.Executing);

            Vector3 normal;
            Vector3 origin;
            if (type == CutType.Joystick)
            {
                // Use the direction already shown on the plane — avoids re-resolving from a
                // potentially stale stick value and guarantees cut matches visual exactly.
                normal = CurrentCutNormal;
                origin = CutPlaneOrigin;
            }
            else
            {
                // Cardinal (L/H, no stick): resolve the fixed axis and apply the angle tilt.
                normal = TiltCutNormal(_directionResolver.Resolve(Vector2.zero, type), type);
                CurrentCutNormal = normal;
                origin = CutPlaneOrigin;
            }

            _planeViz.UpdateOrientation(origin, normal);
            _armDriver.SetCutNormal(normal);
            // Arm sweeps from the start pose through neutral to the opposite pose.
            // Fire-and-forget: the wait below matches the animation duration.
            _armDriver.AnimateSliceAsync(normal, _settings.SliceDuration, token).Forget();

            var candidates = Physics.OverlapSphere(
                transform.position, _settings.DetectionRadius, _settings.SliceableLayers);

            // Angle guard: optional cone filter in front of the camera (horizontal plane only).
            // Flatten camera forward; if DetectionAngle < 180 we skip objects outside the cone.
            Vector3 guardFwd = _directionReference != null
                ? _directionReference.forward : transform.forward;
            guardFwd.y = 0f;
            bool useAngleGuard = _settings.DetectionAngle < 180f && guardFwd.sqrMagnitude > 0.001f;
            if (useAngleGuard) guardFwd = guardFwd.normalized;

            var results = new List<SliceResult>();
            foreach (var col in candidates)
            {
                if (col == null) continue;

                if (useAngleGuard)
                {
                    Vector3 toTarget = col.bounds.center - transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.001f &&
                        Vector3.Angle(guardFwd, toTarget) > _settings.DetectionAngle)
                        continue;
                }
                var sliceable = col.GetComponentInParent<ISliceable>();
                if (sliceable == null || !sliceable.CanBeSliced) continue;

                var sliceableGO = (sliceable as MonoBehaviour)?.gameObject ?? col.gameObject;
                if (!_slicer.WouldIntersect(sliceableGO, origin, normal)) continue;

                var mat = sliceable.CrossSectionMaterial
                          ?? _settings.DefaultCrossSectionMaterial;

                var req = new SliceRequestBuilder(sliceableGO)
                    .AtPlane(origin, normal)
                    .WithCrossSectionMaterial(mat)
                    .WithPhysics(_settings.PhysicsSettings)
                    .Build();

                var result = _slicer.Slice(req);
                if (result.Success) results.Add(result);
            }

            if (results.Count > 0)
                _effects?.OnCutExecuted(results.ToArray(), origin, normal);

            await UniTask.Delay(Mathf.RoundToInt(_settings.SliceDuration * 1000),
                DelayType.UnscaledDeltaTime, cancellationToken: token);

            if (!token.IsCancellationRequested && State == BladeModeState.Executing)
                SetState(BladeModeState.Active);
        }

        // ── Per-frame ─────────────────────────────────────────────────────────────

        void UpdateCutDirection()
        {
            Vector2 stick = _input.RightStickValue;
            float deadzoneSq = _settings.JoystickDeadzone * _settings.JoystickDeadzone;

            if (stick.sqrMagnitude > deadzoneSq)
            {
                CurrentCutNormal = _directionResolver.Resolve(stick, CutType.Joystick);
                _planeViz.UpdateOrientation(CutPlaneOrigin, CurrentCutNormal);
                _armDriver.SetCutNormal(CurrentCutNormal);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        // World-space point the cut plane passes through. Elevated by CutPlaneHeight so horizontal
        // cuts hit the waist/torso of standing enemies rather than their feet.
        // All cut types use this origin so the visual plane is always visible at player level.
        Vector3 CutPlaneOrigin => transform.position + Vector3.up * _settings.CutPlaneHeight;

        // Tilts a cardinal cut normal by CutAngleOffset so L/H cuts are never perfectly flat/upright.
        // Horizontal (Light): tilt around player's right axis (forward lean).
        // Vertical   (Heavy): tilt around player's forward axis (side lean).
        // Joystick cuts are left as-is since the player already chose an arbitrary angle.
        Vector3 TiltCutNormal(Vector3 normal, CutType type)
        {
            if (type == CutType.Joystick || _settings.CutAngleOffset <= 0f) return normal;
            Vector3 tiltAxis = type == CutType.Horizontal ? transform.right : transform.forward;
            float angle = _settings.CutAngleOffset
                          + UnityEngine.Random.Range(-_settings.CutAngleVariance, _settings.CutAngleVariance);
            return Quaternion.AngleAxis(angle, tiltAxis) * normal;
        }

        void SetState(BladeModeState next)
        {
            State = next;
            OnStateChanged?.Invoke(next);
        }

        void CleanupEntry()
        {
            Time.timeScale = 1f;
            if (this != null) SetState(BladeModeState.Inactive);
        }

        static async UniTask LerpTimeScaleAsync(float target, float duration, CancellationToken token)
        {
            // Scale fixedDeltaTime alongside timeScale so FixedUpdate keeps running at its
            // normal real-time frequency (~50 Hz) even at 0.15x timeScale. Without this,
            // FixedUpdate drops to ~7.5 Hz real-time, which causes choppy physics and camera.
            const float BaseFixed = 0.02f;
            float start = Time.timeScale, elapsed = 0f;
            while (elapsed < duration && !token.IsCancellationRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(start, target, elapsed / duration);
                Time.fixedDeltaTime = BaseFixed * Time.timeScale;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            if (!token.IsCancellationRequested)
            {
                Time.timeScale = target;
                Time.fixedDeltaTime = BaseFixed * target;
            }
        }
    }
}
