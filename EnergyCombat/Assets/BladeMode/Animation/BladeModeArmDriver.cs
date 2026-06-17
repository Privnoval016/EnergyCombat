using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BladeMode.Animation
{
    /// <summary>
    /// Drives blade-mode arm animations via Animancer.
    ///
    /// Aim: a DirectionalMixerState (2D polar-gradient blend tree, identical to
    ///      Unity's "2D Freeform Directional" blend type) fed by a DirectionalAnimationSet8.
    ///      Set _aimSet to a DirectionalAnimationSet8 asset with all 8 directional aim poses.
    ///
    /// Slice: a DirectionalClipTransition picks the one-shot slice clip closest to the
    ///        cut normal and plays it with the transition's configured fade duration.
    ///        Assign a DirectionalAnimationSet8 asset inside _sliceTransition's Animation Set field.
    ///
    /// All fields are optional. If nothing is assigned the methods are no-ops.
    /// The Animancer layer weight is driven externally by AnimateWeightAsync (called by BladeModeController).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BladeModeArmDriver : MonoBehaviour
    {
        [Header("Animancer")]
        [Tooltip("Player's AnimancerComponent.")]
        [SerializeField] AnimancerComponent _animancer;
        [Tooltip("Layer index for blade-mode arms. Must have an upper-body AvatarMask set in PlayerBladeModeAdapter.")]
        [SerializeField] int _layerIndex = 1;

        [Header("Aim Blend Tree")]
        [Tooltip("DirectionalAnimationSet8 asset with 8 aim-pose clips (Up/Right/Down/Left + diagonals). " +
                 "The mixer blends them using polar-gradient interpolation based on the current cut-plane orientation.")]
        [SerializeField] DirectionalAnimationSet8 _aimSet;


        AnimancerLayer _layer;
        DirectionalMixerState _aimMixer;
        bool _initialized;

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        void Awake() => TryInit();

        void TryInit()
        {
            if (_initialized || _animancer == null) return;

            _layer = _animancer.Layers[_layerIndex];

            if (_aimSet != null)
            {
                _aimMixer = new DirectionalMixerState();
                for (int i = 0; i < _aimSet.DirectionCount; i++)
                {
                    var clip = _aimSet.Get(i);
                    if (clip != null)
                        _aimMixer.Add(clip, _aimSet.GetDirection(i));
                }

                if (_aimMixer.ChildCount > 0)
                {
                    // Don't let Play() auto-raise the layer weight —
                    // BladeModeController fades it in manually via AnimateWeightAsync.
                    _layer.SetLayerWeightOnPlay = false;
                    _layer.Play(_aimMixer);
                    _layer.SetLayerWeightOnPlay = true;
                    _layer.SetWeight(0);
                }
            }

            _initialized = true;
        }

        // ── Called every frame while blade mode is Active ─────────────────────────

        /// <summary>
        /// Update the aim blend tree to match the current cut plane normal.
        /// The normal is projected into the player's local XY plane to produce a 2D blend parameter:
        ///   local.up    → (0, 1) → Up threshold   → aim-pose for a horizontal cut
        ///   local.right → (1, 0) → Right threshold → aim-pose for a left-right vertical cut
        /// </summary>
        public void SetCutNormal(Vector3 worldNormal)
        {
            if (_aimMixer == null) return;

            var local = transform.InverseTransformDirection(worldNormal);
            _aimMixer.Parameter = new Vector2(local.x, local.y);
        }

        // ── Called once per cut ────────────────────────────────────────────────────

        /// <summary>
        /// Animate the arm through a cut: sweeps the blend-tree parameter from the pose
        /// that represents <paramref name="worldNormal"/> to its opposite (-startDir),
        /// passing through the centre (0,0). With static single-frame poses this produces
        /// a smooth swing in the cut direction — no separate slice clip needed.
        /// Uses unscaled time so the animation runs at full speed during slow-mo.
        /// </summary>
        public async UniTask AnimateSliceAsync(Vector3 worldNormal, float duration, CancellationToken token)
        {
            if (_aimMixer == null) return;

            var local = transform.InverseTransformDirection(worldNormal);
            Vector2 startDir = new Vector2(local.x, local.y);
            if (startDir.sqrMagnitude < 0.001f) startDir = Vector2.up;
            startDir = startDir.normalized;
            Vector2 endDir = -startDir;

            float elapsed = 0f;
            while (elapsed < duration && !token.IsCancellationRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                _aimMixer.Parameter = Vector2.LerpUnclamped(startDir, endDir, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (!token.IsCancellationRequested)
                _aimMixer.Parameter = endDir;
        }

        // ── Entry / Exit blend ────────────────────────────────────────────────────

        /// <summary>
        /// Fade the arm animation layer weight in (to=1) or out (to=0).
        /// Returns immediately if no AnimancerComponent is assigned.
        /// Uses unscaledDeltaTime so it runs correctly while timeScale is 0.15.
        /// </summary>
        public async UniTask AnimateWeightAsync(float to, float duration, CancellationToken token)
        {
            if (_layer == null)
            {
                TryInit();
                if (_layer == null) return;
            }

            float from    = _layer.Weight;
            float elapsed = 0f;

            while (elapsed < duration && !token.IsCancellationRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                _layer.SetWeight(Mathf.Lerp(from, to, elapsed / duration));
                await UniTask.Yield(cancellationToken: token);
            }

            if (!token.IsCancellationRequested)
                _layer.SetWeight(to);
        }
    }
}
