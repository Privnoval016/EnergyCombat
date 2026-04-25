using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Constraints
{
    /**
     * <summary>
     * Projects velocity onto the ground plane to prevent floating on slopes
     * and applies a small downward force to maintain ground contact.
     * Only active when the character is grounded.
     * </summary>
     *
     * <remarks>
     * When moving downhill, velocity is projected onto the slope surface so the character
     * follows the terrain instead of launching off edges. The snap force prevents
     * micro-bouncing on uneven surfaces.
     * </remarks>
     */
    public class GroundSnapConstraint : IMotionConstraint
    {
        /** <summary>Constraint priority. Runs early to establish ground relationship.</summary> */
        public int Priority => 0;

        /** <summary>Downward force applied to maintain ground contact on slopes.</summary> */
        public float SnapForce { get; set; }

        public GroundSnapConstraint(float snapForce = 5f)
        {
            SnapForce = snapForce;
        }

        /**
         * <summary>
         * Projects velocity onto the ground plane when grounded and moving downhill.
         * Zeroes vertical velocity when grounded and not jumping.
         * </summary>
         */
        public void Enforce(MotionContext context)
        {
            if ((context.Flags & MotionFlags.Grounded) == 0) return;

            // If moving upward (jumping), don't snap
            if (context.Velocity.y > 0.1f) return;

            Vector3 normal = context.GroundNormal;
            if (normal.sqrMagnitude < 0.5f) normal = Vector3.up;

            // Project horizontal velocity onto slope surface
            Vector3 vel = context.Velocity;
            float verticalComponent = Vector3.Dot(vel, normal);

            // If velocity has a component into the ground, remove it
            if (verticalComponent < 0f)
            {
                vel -= normal * verticalComponent;
            }
            else
            {
                // On flat ground, just zero vertical velocity
                vel.y = -SnapForce * context.DeltaTime;
            }

            context.Velocity = vel;
        }
    }
}
