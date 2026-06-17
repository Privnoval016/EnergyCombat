using BladeMode.Core;
using UnityEngine;

namespace BladeMode.Visualization
{
    // Manages the cut-plane quad that shows where the next slice would land.
    // Assign a prefab built with a quad mesh + CutPlane.shader material.
    //
    // The quad's local Y axis is used as the plane normal (Unity Quad mesh convention).
    // LookRotation(tangent, normal) sets Y=normal and Z=tangent, which correctly orients it.
    public sealed class CutPlaneVisualizer
    {
        GameObject _instance;

        public void Show(Vector3 worldPos, Vector3 normal, BladeModeSettings settings)
        {
            if (_instance == null && settings.CutPlanePrefab != null)
                _instance = Object.Instantiate(settings.CutPlanePrefab);

            if (_instance == null) return;

            _instance.SetActive(true);
            _instance.transform.localScale = settings.CutPlaneScale;
            UpdateOrientation(worldPos, normal);
        }

        public void UpdateOrientation(Vector3 worldPos, Vector3 normal)
        {
            if (_instance == null || !_instance.activeSelf) return;

            _instance.transform.position = worldPos;

            if (normal.sqrMagnitude > 0.001f)
            {
                Vector3 n = normal.normalized;
                Vector3 upRef = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.99f
                    ? Vector3.forward : Vector3.up;
                Vector3 fwd = Vector3.Cross(n, upRef).normalized;
                _instance.transform.rotation = Quaternion.LookRotation(fwd, n);
            }
        }

        public void Hide()
        {
            if (_instance != null) _instance.SetActive(false);
        }

        public void Dispose()
        {
            if (_instance != null) Object.Destroy(_instance);
        }
    }
}
