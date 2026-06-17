using UnityEngine;

namespace BladeMode.Slicing
{
    public sealed class SliceRequestBuilder
    {
        readonly SliceRequest _req = new();

        public SliceRequestBuilder(GameObject target) => _req.Target = target;

        public SliceRequestBuilder AtPlane(Vector3 point, Vector3 normal)
        {
            _req.PlanePoint  = point;
            _req.PlaneNormal = normal.normalized;
            return this;
        }

        public SliceRequestBuilder WithCrossSectionMaterial(Material mat)
        {
            _req.CrossSectionMaterial = mat;
            return this;
        }

        public SliceRequestBuilder WithPhysics(SlicePhysicsSettings settings)
        {
            _req.PhysicsSettings = settings;
            return this;
        }

        public SliceRequestBuilder KeepOriginal()
        {
            _req.DestroyOriginal = false;
            return this;
        }

        public SliceRequest Build() => _req;
    }
}
