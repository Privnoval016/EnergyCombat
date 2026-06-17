using UnityEngine;

namespace BladeMode.BodyParts
{
    public interface ISliceable
    {
        bool CanBeSliced { get; }
        Material CrossSectionMaterial { get; }
    }
}
