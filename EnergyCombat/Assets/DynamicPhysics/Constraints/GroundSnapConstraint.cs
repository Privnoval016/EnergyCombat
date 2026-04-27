using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Projects velocity onto the ground plane to prevent floating on slopes
     * and applies a strong downward force to maintain ground contact.
     * 
     * When grounded with upward velocity, velocity is projected onto the ground plane.
     * When grounded with no upward velocity, a snap force is applied to maintain contact
     * and prevent floating when moving horizontally.
     * </summary>
     */
    public class GroundSnapConstraint : IMotionConstraint
    {
        public int Priority => 0;

        /** <summary>Downward force applied to maintain ground contact. Should be at least gravity magnitude.</summary> */
        public float SnapForce { get; set; }

        public GroundSnapConstraint(float snapForce = 20f)
        {
            SnapForce = snapForce;
        }

        public void Enforce(MotionContext context)
        {
            if (!context.HasTag(MotionTag.Grounded)) return;
            if (context.Velocity.y > 0.1f) return;  // Don't snap if jumping upward

            Vector3 normal = context.GroundNormal;
            if (normal.sqrMagnitude < 0.5f) normal = Vector3.up;

            Vector3 vel = context.Velocity;
            float verticalComponent = Vector3.Dot(vel, normal);

            if (verticalComponent < 0f)
            {
                // Project velocity onto ground plane (remove component going into ground)
                vel -= normal * verticalComponent;
            }
            else
            {
                // Apply strong downward snap force to prevent floating
                vel.y = -SnapForce * context.DeltaTime;
            }

            context.Velocity = vel;
        }
    }
}
