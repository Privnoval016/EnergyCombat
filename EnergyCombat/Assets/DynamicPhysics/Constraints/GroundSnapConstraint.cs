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

        // Live reference to the per-frame config so SnapForce always reflects the active profile.
        private readonly RuntimeMotionConfig _config;

        public GroundSnapConstraint(RuntimeMotionConfig config)
        {
            _config = config;
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
                vel.y = -_config.SnapForce * context.DeltaTime;
            }

            context.Velocity = vel;
        }
    }
}
