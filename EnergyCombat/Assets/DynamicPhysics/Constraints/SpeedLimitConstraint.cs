using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Constraints
{
    /**
     * <summary>
     * Hard cap on total velocity magnitude. Unlike <see cref="Pipeline.Modifiers.SpeedCapModifier"/>
     * which only clamps horizontal speed, this constraint limits the entire velocity vector
     * including vertical components.
     * </summary>
     */
    public class SpeedLimitConstraint : IMotionConstraint
    {
        /** <summary>Constraint priority. Runs last among constraints.</summary> */
        public int Priority => 100;

        /** <summary>Maximum allowed total velocity magnitude.</summary> */
        public float MaxSpeed { get; set; }

        public SpeedLimitConstraint(float maxSpeed = 50f)
        {
            MaxSpeed = maxSpeed;
        }

        /**
         * <summary>
         * Clamps total velocity magnitude to the configured limit.
         * Uses sqrMagnitude for the comparison to avoid unnecessary sqrt.
         * </summary>
         */
        public void Enforce(MotionContext context)
        {
            float sqrMag = context.Velocity.sqrMagnitude;
            float maxSqr = MaxSpeed * MaxSpeed;

            if (sqrMag > maxSqr)
            {
                context.Velocity *= MaxSpeed / Mathf.Sqrt(sqrMag);
            }
        }
    }
}
