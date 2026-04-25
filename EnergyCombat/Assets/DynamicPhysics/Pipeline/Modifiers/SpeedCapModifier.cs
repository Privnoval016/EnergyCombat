using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Clamps horizontal speed to a maximum value. Applied after all velocity contributions
     * to enforce hard speed limits without preventing vertical velocity changes.
     * </summary>
     */
    public class SpeedCapModifier : IMotionModifier
    {
        /** <summary>Execution order within the modifier stage.</summary> */
        public int Order { get; }

        /** <summary>Maximum allowed horizontal speed in units per second.</summary> */
        public float MaxSpeed { get; set; }

        public SpeedCapModifier(float maxSpeed, int order = 100)
        {
            MaxSpeed = maxSpeed;
            Order = order;
        }

        public void Apply(MotionContext context)
        {
            Vector3 horizontal = context.Velocity;
            horizontal.y = 0f;

            float sqrMag = horizontal.sqrMagnitude;
            float maxSqr = MaxSpeed * MaxSpeed;

            if (sqrMag > maxSqr)
            {
                float scale = MaxSpeed / Mathf.Sqrt(sqrMag);
                horizontal *= scale;
            }

            context.Velocity.x = horizontal.x;
            context.Velocity.z = horizontal.z;
        }
    }
}
