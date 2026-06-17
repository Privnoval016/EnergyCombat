using System.Linq;
using BladeMode.BodyParts;
using BladeMode.Slicing;
using UnityEngine;

namespace BladeMode.Debug
{
    /// <summary>
    /// Drop a plane GameObject into PlaneTransform. Move and rotate it in the Scene view to
    /// aim the cut — its position is the plane point, its Y-axis (up) is the plane normal.
    /// Click "Execute Cut" in the Inspector to fire through SlicerService.
    /// Works in Edit Mode and Play Mode.
    /// </summary>
    public sealed class SliceDebugger : MonoBehaviour
    {
        [Tooltip("Any GameObject. Its position = cut point; its Up axis = plane normal.")]
        public Transform PlaneTransform;

        [Tooltip("Leave null to auto-detect all SliceableBody / SliceableSurface objects within DetectionRadius.")]
        public GameObject ManualTarget;
        public float      DetectionRadius = 4f;

        public Material             CrossSectionMaterial;
        public SlicePhysicsSettings PhysicsSettings;

        public void ExecuteCut()
        {
            if (PlaneTransform == null)
            {
                UnityEngine.Debug.LogWarning("[SliceDebugger] Assign a PlaneTransform first.");
                return;
            }

            Vector3 point  = PlaneTransform.position;
            Vector3 normal = PlaneTransform.up;
            var slicer = new SlicerService();

            var candidates = ManualTarget != null
                ? new[] { ManualTarget }
                : FindObjectsByType<SliceableSurface>()
                      .Select(s => s.gameObject)
                      .Concat(FindObjectsByType<SliceableBody>()
                                  .Select(b => b.gameObject))
                      .Where(go => Vector3.Distance(go.transform.position, point) <= DetectionRadius)
                      .ToArray();

            foreach (var target in candidates)
            {
                if (!slicer.WouldIntersect(target, point, normal)) continue;

                var req = new SliceRequestBuilder(target)
                    .AtPlane(point, normal)
                    .WithCrossSectionMaterial(CrossSectionMaterial)
                    .WithPhysics(PhysicsSettings)
                    .Build();

                var result = slicer.Slice(req);
                UnityEngine.Debug.Log(
                    $"[SliceDebugger] {target.name}: {(result.Success ? "OK" : result.FailureReason)}");
            }
        }
    }
}
