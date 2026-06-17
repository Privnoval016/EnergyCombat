using UnityEngine;

namespace BladeMode.BodyParts
{
    // Marker placed on any prop that can be cut (requires MeshFilter on the same GO).
    [RequireComponent(typeof(MeshFilter))]
    [DisallowMultipleComponent]
    public class SliceableSurface : MonoBehaviour, ISliceable
    {
        [SerializeField] Material _crossSectionMaterial;

        public bool CanBeSliced => isActiveAndEnabled;
        public Material CrossSectionMaterial => _crossSectionMaterial;

        // Called by SlicerService when stamping this component onto a freshly-created hull.
        public void SetCrossSectionMaterial(Material mat) => _crossSectionMaterial = mat;
    }
}
