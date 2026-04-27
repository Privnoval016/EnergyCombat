using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Performs spherecast-based ground detection beneath the character.
     * Reports grounded state, surface normal, contact point, and slope angle.
     * </summary>
     *
     * <remarks>
     * Uses <see cref="Physics.SphereCast"/> for reliable detection on uneven terrain.
     * The spherecast originates slightly above the character's base to avoid starting
     * inside geometry. Results are cached per-call and should be queried once per tick.
     * </remarks>
     */
    [System.Serializable]
    public class GroundDetector
    {
        /** <summary>Half the height of the character. Used to position the detection spherecast.</summary> */
        [Tooltip("Half the height of the character.")]
        public float characterHalfHeight = 1f; 
        
        /** <summary>Radius of the detection sphere. Should roughly match the character's collider radius.</summary> */
        [Tooltip("Radius of the ground detection sphere.")]
        public float radius = 0.3f;

        /** <summary>Maximum distance below the character to check for ground.</summary> */
        [Tooltip("How far below the character to check for ground.")]
        public float distance = 0.3f;

        /** <summary>Vertical offset above the character's base to start the spherecast from.</summary> */
        [Tooltip("Offset above character base to start the cast.")]
        public float originOffset = 0.1f;

        /** <summary>Layer mask for ground surfaces.</summary> */
        [Tooltip("Layers considered as ground.")]
        public LayerMask groundLayers = ~0;

        /** <summary>Maximum angle (degrees) of a surface that is considered walkable.</summary> */
        [Tooltip("Max walkable slope angle in degrees.")]
        public float maxSlopeAngle = 45f;

        #region Cached Results

        /** <summary>Whether the last detection found walkable ground.</summary> */
        public bool IsGrounded { get; private set; }

        /** <summary>Normal of the detected ground surface. Zero if not grounded.</summary> */
        public Vector3 GroundNormal { get; private set; }

        /** <summary>World-space point of ground contact. Zero if not grounded.</summary> */
        public Vector3 GroundPoint { get; private set; }

        /** <summary>Angle of the detected surface relative to world up, in degrees.</summary> */
        public float GroundAngle { get; private set; }

        #endregion

        /**
         * <summary>
         * Performs the ground detection spherecast from the given position.
         * Updates all cached result properties.
         * </summary>
         *
         * <param name="position">World-space position of the character's center.</param>
         */
        public void Detect(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * (radius + originOffset - characterHalfHeight);

            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, distance + originOffset, groundLayers, QueryTriggerInteraction.Ignore))
            {
                GroundAngle = Vector3.Angle(hit.normal, Vector3.up);
                GroundNormal = hit.normal;
                GroundPoint = hit.point;
                IsGrounded = GroundAngle <= maxSlopeAngle;
            }
            else
            {
                IsGrounded = false;
                GroundNormal = Vector3.zero;
                GroundPoint = Vector3.zero;
                GroundAngle = 0f;
            }
        }
    }
}
