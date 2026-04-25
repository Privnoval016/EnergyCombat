using UnityEngine;

namespace DynamicPhysics.Abilities
{
    /**
     * <summary>
     * World-space anchor component placed on grapple points, swing bars, or rope endpoints.
     * Provides an anchor position and rope length for the <see cref="RopeSwingAbility"/>
     * and <see cref="Constraints.RopeConstraint"/>.
     * </summary>
     *
     * <remarks>
     * Attach this to any GameObject that should serve as a swing anchor. The rope swing
     * ability uses this component to configure the rope constraint when the player attaches.
     * The anchor position defaults to this transform's position but can be offset.
     * </remarks>
     */
    public class RopeEffector : MonoBehaviour
    {
        /** <summary>Maximum rope length from this anchor.</summary> */
        [Tooltip("Maximum rope length from this anchor.")]
        public float MaxRopeLength = 15f;

        /** <summary>Optional minimum rope length for retractable ropes.</summary> */
        [Tooltip("Minimum rope length (0 for fixed-length ropes).")]
        public float MinRopeLength = 0f;

        /** <summary>Local-space offset from this transform for the anchor point.</summary> */
        [Tooltip("Offset from transform position for the anchor point.")]
        public Vector3 AnchorOffset = Vector3.zero;

        /** <summary>Whether this effector allows the player to reel in/out the rope.</summary> */
        [Tooltip("If true, the player can adjust rope length.")]
        public bool AllowLengthAdjustment = false;

        /** <summary>Speed at which the rope retracts when reeling in.</summary> */
        [Tooltip("Reel-in speed in units per second.")]
        public float ReelSpeed = 5f;

        /** <summary>Gets the world-space anchor position including offset.</summary> */
        public Vector3 AnchorPosition => transform.TransformPoint(AnchorOffset);

        /**
         * <summary>
         * Calculates the rope length to use based on the character's current distance.
         * Returns the lesser of the distance or MaxRopeLength, clamped to MinRopeLength.
         * </summary>
         *
         * <param name="characterPosition">World-space position of the character.</param>
         * <returns>The rope length to use.</returns>
         */
        public float CalculateRopeLength(Vector3 characterPosition)
        {
            float distance = Vector3.Distance(AnchorPosition, characterPosition);
            return Mathf.Clamp(distance, MinRopeLength, MaxRopeLength);
        }
    }
}
