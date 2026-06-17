using UnityEngine;

namespace BladeMode.Slicing
{
    public sealed class SliceRequest
    {
        public GameObject         Target               { get; internal set; }
        public Vector3            PlanePoint           { get; internal set; }
        public Vector3            PlaneNormal          { get; internal set; }
        public Material           CrossSectionMaterial  { get; internal set; }
        public SlicePhysicsSettings PhysicsSettings    { get; internal set; }
        // When false (default): disable the original's visual/physics components so scripts survive.
        // When true: destroy the original GameObject completely after slicing.
        public bool               DestroyOriginal       { get; internal set; } = false;
    }
}
