using System;
using EzySlice;
using UnityEngine;
using EzyPlane = EzySlice.Plane;

namespace BladeMode.BodyParts
{
    // Place alongside SliceableBody on the character root.
    // BrainBone: drag the head (or whichever bone must survive). If a cut sends that
    // bone to the severed side, the Animator is disabled and the character dies.
    [DisallowMultipleComponent]
    public class BodyPartRegistry : MonoBehaviour
    {
        [Tooltip("Bone that keeps the character alive. Severing it kills the character.")]
        public Transform BrainBone;
        [SerializeField] Animator _animator;

        public event Action<BodyPartType> OnPartDetached;
        public event Action OnBrainSevered;
        public bool IsBrainSevered { get; private set; }

        SliceableBodyPart[] _parts;

        void Awake() => _parts = GetComponentsInChildren<SliceableBodyPart>();

        public bool IsPartAttached(BodyPartType type) =>
            Array.Exists(_parts, p => p.PartType == type && !p.IsDetached);

        // Called by SlicerService after every successful cut.
        public void NotifyCut(Vector3 planePoint, Vector3 planeNormal)
        {
            var plane = new EzySlice.Plane();
            plane.Compute(planePoint, planeNormal);

            foreach (var part in _parts)
            {
                if (part.IsDetached) continue;
                if (plane.SideOf(part.transform.position) != SideOfPlane.DOWN) continue;
                part.NotifyDetached();
                OnPartDetached?.Invoke(part.PartType);
            }

            if (BrainBone == null || IsBrainSevered) return;
            if (plane.SideOf(BrainBone.position) != SideOfPlane.DOWN) return;

            IsBrainSevered = true;
            if (_animator != null) _animator.enabled = false;
            foreach (var p in _parts) p.NotifyDetached();
            OnBrainSevered?.Invoke();
        }
    }
}
