using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Integrates accumulated external forces (knockback, explosions, wind) into velocity
     * using impulse-based integration: Δv = F/m. Clears the force accumulator after integration.
     * </summary>
     *
     * <remarks>
     * Forces are treated as impulses (force * dt has already been applied by the caller, or
     * the force is integrated here). External systems add forces via
     * <see cref="Orchestration.MotionOrchestrator.AddForce"/>.
     * </remarks>
     */
    public class ExternalForceStage : IMotionStage
    {
        /** <summary>Execution priority. Runs after abilities.</summary> */
        public int Priority => InfluencePriority.ExternalForce;

        /**
         * <summary>
         * Integrates accumulated forces and clears the accumulator.
         * Force integration: Δv = (F * dt) / m.
         * </summary>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            if (context.AccumulatedForce.sqrMagnitude < 0.0001f) return;

            float inverseMass = context.Mass > 0.001f ? 1f / context.Mass : 1f;
            context.Velocity += context.AccumulatedForce * (inverseMass * context.DeltaTime);
            context.AccumulatedForce = Vector3.zero;
        }
    }
}
