using System;
using BladeMode.Slicing;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BladeMode.Effects
{
    /// <summary>
    /// Spawns particle / VFX prefabs on blade mode events.
    ///
    /// Each slot (OnEnter, OnExit, OnCut, OnHit) is a VFXBundle: a prefab, an
    /// auto-destroy lifetime, and an optional flag to parent the instance to the player.
    ///
    /// OnCut spawns once at the cut plane origin. OnHit spawns at every mesh
    /// intersection point returned by the slicer (blood, sparks, decals, etc.).
    /// </summary>
    [Serializable]
    public sealed class ParticleEffect : IBladeModeEffect
    {
        [Header("On Enter Blade Mode")]
        [SerializeField] VFXBundle _onEnter;

        [Header("On Exit Blade Mode")]
        [SerializeField] VFXBundle _onExit;

        [Header("On Cut — at the cut plane origin")]
        [SerializeField] VFXBundle _onCut;

        [Header("On Hit — at each mesh intersection point")]
        [SerializeField] VFXBundle _onHit;

        Transform _playerRoot;

        public void Initialize(Transform playerTransform) => _playerRoot = playerTransform;

        public void OnEnterBladeMode() =>
            Spawn(_onEnter, PlayerPos(), Quaternion.identity);

        public void OnExitBladeMode() =>
            Spawn(_onExit, PlayerPos(), Quaternion.identity);

        public void OnCutExecuted(SliceResult[] results, Vector3 planePoint, Vector3 planeNormal)
        {
            Spawn(_onCut, planePoint, Quaternion.LookRotation(planeNormal));

            if (_onHit.Prefab == null) return;
            foreach (var r in results)
            {
                if (!r.Success || r.IntersectionPoints == null) continue;
                foreach (var pt in r.IntersectionPoints)
                    Spawn(_onHit, pt, Quaternion.identity);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        Vector3 PlayerPos() => _playerRoot != null ? _playerRoot.position : Vector3.zero;

        void Spawn(VFXBundle bundle, Vector3 worldPos, Quaternion rot)
        {
            if (bundle.Prefab == null) return;
            Transform parent = bundle.AttachToPlayer ? _playerRoot : null;
            var go = Object.Instantiate(bundle.Prefab, worldPos, rot, parent);
            if (bundle.Lifetime > 0f) Object.Destroy(go, bundle.Lifetime);
        }

        // ── Nested type ───────────────────────────────────────────────────────────

        [Serializable]
        public struct VFXBundle
        {
            [Tooltip("Prefab to spawn (ParticleSystem, VFX Graph, etc.).")]
            public GameObject Prefab;

            [Tooltip("Real-time seconds before auto-destroy. 0 = keep alive indefinitely.")]
            [Min(0f)] public float Lifetime;

            [Tooltip("Parent the spawned instance to the player so it follows movement. " +
                     "Useful for enter/exit auras; disable for world-space impacts.")]
            public bool AttachToPlayer;
        }
    }
}
