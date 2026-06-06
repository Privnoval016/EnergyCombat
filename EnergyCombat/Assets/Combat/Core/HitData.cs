using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Describes a single registered hit between an attacker and a target.
     * Created by <c>WeaponHitboxController</c> and populated with computed
     * damage and tags before being distributed to listeners.
     * </summary>
     */
    public class HitData
    {
        /** <summary>The <c>GameObject</c> that caused the hit (the attacker).</summary> */
        public GameObject Source;

        /** <summary>The <c>GameObject</c> that received the hit (the target).</summary> */
        public GameObject Target;

        /** <summary>World-space contact point.</summary> */
        public Vector3 Point;

        /** <summary>Surface normal at the contact point, pointing away from the target.</summary> */
        public Vector3 Normal;

        /** <summary>Final computed damage after stat evaluation and any override modifiers.</summary> */
        public float Damage;

        /**
         * <summary>
         * Tags present on the attack at the moment of hit. Includes both the ability's
         * static tags and any tags dynamically added during execution.
         * </summary>
         */
        public Tag[] AttackTags;

        /** <summary>The execution context in which this hit was registered.</summary> */
        public CombatContext Context;
    }
}
