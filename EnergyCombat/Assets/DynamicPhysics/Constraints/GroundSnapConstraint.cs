using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Projects velocity onto the ground plane to prevent floating on slopes
     * and applies a small downward force to maintain ground contact.
     * </summary>
     */
    public class GroundSnapConstraint : IMotionConstraint
    {
        public int Priority => 0;

        /** <summary>Downward force applied to maintain ground contact on slopes.</summary> */
        public float SnapForce { get; set; }

        public GroundSnapConstraint(float snapForce = 5f)
        {
            SnapForce = snapForce;
        }

        public void Enforce(MotionContext context)
        {
            if (!context.HasTag(MotionTag.Grounded)) return;
            if (context.Velocity.y > 0.1f) return;

            Vector3 normal = context.GroundNormal;
            if (normal.sqrMagnitude < 0.5f) normal = Vector3.up;

            Vector3 vel = context.Velocity;
            float verticalComponent = Vector3.Dot(vel, normal);

            if (verticalComponent < 0f)
            {
                vel -= normal * verticalComponent;
            }
            else
            {
                vel.y = -SnapForce * context.DeltaTime;
            }

            context.Velocity = vel;
        }
    }
}
