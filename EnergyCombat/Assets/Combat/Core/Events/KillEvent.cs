using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Raised when a hit reduces a target's health to zero or below.
     * Broadcast by the target's health component after processing a <see cref="HitEvent"/>.
     * </summary>
     */
    public struct KillEvent : ICombatEvent
    {
        /** <summary>The <c>GameObject</c> that was killed.</summary> */
        public GameObject Target;

        /** <summary>The execution context of the killing blow.</summary> */
        public CombatContext Context;
    }
}
