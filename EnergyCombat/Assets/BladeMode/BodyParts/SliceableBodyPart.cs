using System;
using UnityEngine;

namespace BladeMode.BodyParts
{
    // Lightweight marker placed on a bone GameObject to give it semantic identity.
    // BodyPartRegistry discovers these on Awake and checks them after each cut.
    [DisallowMultipleComponent]
    public class SliceableBodyPart : MonoBehaviour
    {
        public BodyPartType PartType;
        public bool IsDetached { get; private set; }

        public event Action<SliceableBodyPart> OnDetached;

        internal void NotifyDetached()
        {
            if (IsDetached) return;
            IsDetached = true;
            OnDetached?.Invoke(this);
        }
    }
}
