using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Constraints
{
    /**
     * <summary>
     * Enforces a maximum distance from an anchor point, simulating pendulum / grapple physics.
     * When the character exceeds the rope length, velocity is projected to be tangent
     * to the constraint sphere, preventing outward movement while preserving tangential momentum.
     * </summary>
     *
     * <remarks>
     * The constraint uses a position-based correction: if the distance from the anchor exceeds
     * the rope length, the velocity component along the radial direction (away from anchor)
     * is removed. This produces natural pendulum behavior where gravity and input create
     * swinging motion while the rope prevents escape.
     *
     * Activate and deactivate this constraint via the <see cref="Abilities.RopeSwingAbility"/>
     * or directly through the <see cref="Orchestration.MotionOrchestrator"/>.
     * </remarks>
     */
    public class RopeConstraint : IMotionConstraint
    {
        /** <summary>Constraint priority. Runs after ground snap.</summary> */
        public int Priority => 50;

        /** <summary>World-space position of the rope anchor.</summary> */
        public Vector3 AnchorPoint { get; set; }

        /** <summary>Maximum allowed distance from the anchor.</summary> */
        public float RopeLength { get; set; }

        /** <summary>Optional minimum rope length for retractable ropes.</summary> */
        public float MinRopeLength { get; set; }

        /** <summary>Whether this constraint is currently active.</summary> */
        public bool IsActive { get; set; }

        public RopeConstraint()
        {
            MinRopeLength = 0f;
            IsActive = false;
        }

        /**
         * <summary>
         * Enforces the rope length constraint. If the character is beyond the rope length,
         * removes the outward velocity component and corrects the position prediction.
         * </summary>
         */
        public void Enforce(MotionContext context)
        {
            if (!IsActive) return;

            Vector3 toCharacter = context.Position - AnchorPoint;
            float distSqr = toCharacter.sqrMagnitude;
            float ropeLenSqr = RopeLength * RopeLength;

            if (distSqr <= ropeLenSqr) return;

            // Character is beyond rope length — enforce constraint
            float dist = Mathf.Sqrt(distSqr);
            Vector3 radial = toCharacter * (1f / dist); // normalized direction from anchor to character

            // Remove outward velocity component (keep tangential)
            float outwardSpeed = Vector3.Dot(context.Velocity, radial);
            if (outwardSpeed > 0f)
            {
                context.Velocity -= radial * outwardSpeed;
            }

            // Add centripetal correction to pull back toward rope length
            float overshoot = dist - RopeLength;
            context.Velocity -= radial * (overshoot / context.DeltaTime * 0.5f);
        }
    }
}
