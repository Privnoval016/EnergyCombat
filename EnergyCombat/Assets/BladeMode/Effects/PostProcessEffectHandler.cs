using System;
using System.Threading;
using BladeMode.Slicing;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace BladeMode.Effects
{
    /// <summary>
    /// Drives a URP Global Volume weight in response to blade mode events.
    /// Assign a VolumeProfile asset — a Global Volume GO is created at runtime
    /// the first time blade mode is entered, then reused for subsequent activations.
    ///
    /// Uses unscaled time so effects are not slowed by the 0.15× time scale.
    /// On enter: fades weight up to EnterWeight.
    /// On exit: fades back to 0.
    /// On cut: spikes weight by PulseMagnitude and decays back to baseline.
    /// </summary>
    [Serializable]
    public sealed class PostProcessEffect : IBladeModeEffect
    {
        [Tooltip("Volume Profile driving the effect (ChromaticAberration, Vignette, etc.). " +
                 "A Global Volume is created at runtime — no scene setup required.")]
        [SerializeField] VolumeProfile _profile;

        [Header("Enter")]
        [Tooltip("Target volume weight while blade mode is active. 1 = full profile strength.")]
        [Range(0f, 1f)] [SerializeField] float _enterWeight = 1f;
        [Tooltip("Real-time seconds to fade the volume in on enter.")]
        [SerializeField] float _enterDuration = 0.25f;

        [Header("Exit")]
        [Tooltip("Real-time seconds to fade the volume back to zero on exit.")]
        [SerializeField] float _exitDuration = 0.4f;

        [Header("Cut Pulse")]
        [Tooltip("Extra weight spiked above baseline on each successful cut. Clamped to 1.")]
        [Range(0f, 1f)] [SerializeField] float _pulseMagnitude = 0.3f;
        [Tooltip("Real-time seconds for the pulse to decay back to the baseline weight.")]
        [SerializeField] float _pulseDuration = 0.12f;

        Volume _volume;
        float  _baselineWeight;
        CancellationTokenSource _fadeCts;
        CancellationTokenSource _pulseCts;

        public void Initialize(Transform playerTransform) { }

        public void OnEnterBladeMode()
        {
            EnsureVolume();
            _baselineWeight = _enterWeight;
            Fade(_enterWeight, _enterDuration);
        }

        public void OnExitBladeMode()
        {
            _baselineWeight = 0f;
            Fade(0f, _exitDuration);
        }

        public void OnCutExecuted(SliceResult[] results, Vector3 planePoint, Vector3 planeNormal)
        {
            if (_volume == null || _pulseMagnitude <= 0f) return;
            _pulseCts?.Cancel();
            _pulseCts = new CancellationTokenSource();
            PulseAsync(_pulseCts.Token).Forget();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        void EnsureVolume()
        {
            if (_profile == null || _volume != null) return;

            const string volName = "[BladeModePostProcess]";
            var existing = GameObject.Find(volName);
            if (existing != null) { _volume = existing.GetComponent<Volume>(); return; }

            var go = new GameObject(volName);
            _volume = go.AddComponent<Volume>();
            _volume.sharedProfile = _profile;
            _volume.isGlobal = true;
            _volume.weight = 0f;
        }

        void Fade(float target, float duration)
        {
            _fadeCts?.Cancel();
            _fadeCts = new CancellationTokenSource();
            FadeAsync(target, duration, _fadeCts.Token).Forget();
        }

        async UniTaskVoid FadeAsync(float target, float duration, CancellationToken token)
        {
            if (_volume == null) return;
            float start = _volume.weight, elapsed = 0f;
            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _volume.weight = Mathf.Lerp(start, target, elapsed / duration);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                }
                _volume.weight = target;
            }
            catch (OperationCanceledException) { }
        }

        async UniTaskVoid PulseAsync(CancellationToken token)
        {
            if (_volume == null) return;
            float peak = Mathf.Min(1f, _baselineWeight + _pulseMagnitude);
            _volume.weight = peak;
            float elapsed = 0f;
            try
            {
                while (elapsed < _pulseDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _volume.weight = Mathf.Lerp(peak, _baselineWeight, elapsed / _pulseDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                }
                _volume.weight = _baselineWeight;
            }
            catch (OperationCanceledException) { }
        }
    }
}
