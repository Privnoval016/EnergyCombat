using System;
using BladeMode.Slicing;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BladeMode.Effects
{
    /// <summary>
    /// Spawns per-cut VFX: a slash effect at the cut plane and impact effects at
    /// each mesh intersection point returned by the slicer (blood, sparks, decals).
    /// Enter/exit events are intentional no-ops — use ParticleEffect for those.
    /// </summary>
    [Serializable]
    public sealed class SliceVFXEffect : IBladeModeEffect
    {
        [Tooltip("Spawned once at the cut plane origin, oriented to the plane normal. " +
                 "Good for slash trails, energy waves, sword arcs.")]
        [SerializeField] GameObject _slashVFXPrefab;

        [Tooltip("Spawned at each individual mesh intersection point. " +
                 "Good for blood bursts, sparks, or wound decals.")]
        [SerializeField] GameObject _hitImpactPrefab;

        [Tooltip("Real-time seconds before auto-destroying each spawned VFX instance.")]
        [SerializeField, Min(0f)] float _vfxLifetime = 2f;

        public void Initialize(Transform playerTransform) { }
        public void OnEnterBladeMode() { }
        public void OnExitBladeMode()  { }

        public void OnCutExecuted(SliceResult[] results, Vector3 planePoint, Vector3 planeNormal)
        {
            foreach (var result in results)
            {
                if (!result.Success) continue;

                if (_slashVFXPrefab != null)
                    SpawnAt(_slashVFXPrefab, planePoint, Quaternion.LookRotation(planeNormal));

                if (_hitImpactPrefab != null && result.IntersectionPoints != null)
                    foreach (var pt in result.IntersectionPoints)
                        SpawnAt(_hitImpactPrefab, pt, Quaternion.identity);
            }
        }

        void SpawnAt(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var go = Object.Instantiate(prefab, pos, rot);
            if (_vfxLifetime > 0f) Object.Destroy(go, _vfxLifetime);
        }
    }
}
