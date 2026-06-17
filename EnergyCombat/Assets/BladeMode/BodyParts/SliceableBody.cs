using UnityEngine;

namespace BladeMode.BodyParts
{
    // Place on a character root. Assigns the SkinnedMeshRenderer that represents the full body
    // and the material used for freshly-cut cross-section faces.
    [DisallowMultipleComponent]
    public class SliceableBody : MonoBehaviour, ISliceable
    {
        [SerializeField] public SkinnedMeshRenderer BodyRenderer;
        [SerializeField] Material _crossSectionMaterial;

        public bool CanBeSliced => BodyRenderer != null && BodyRenderer.enabled;
        public Material CrossSectionMaterial => _crossSectionMaterial;
    }
}
