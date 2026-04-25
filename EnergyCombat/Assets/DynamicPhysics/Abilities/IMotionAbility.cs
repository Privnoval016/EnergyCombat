using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Abilities
{
    /**
     * <summary>
     * Represents a discrete movement ability (jump, dash, wall run, etc.) that can
     * be activated, ticked, and deactivated. Abilities contribute velocity influences
     * additively to the <see cref="MotionContext"/> — they never directly set final velocity.
     * </summary>
     *
     * <remarks>
     * Abilities are registered with the <see cref="Orchestration.MotionOrchestrator"/>
     * and persist across frames. The orchestrator manages activation/deactivation
     * based on incoming <see cref="MotionRequest"/> objects and ability conditions.
     * </remarks>
     */
    public interface IMotionAbility
    {
        /** <summary>Whether this ability is currently active and contributing to motion.</summary> */
        bool IsActive { get; }

        /**
         * <summary>
         * Checks whether this ability can activate given the current motion state and pending requests.
         * Called every tick for inactive abilities.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         * <param name="requests">Pending motion requests from the gameplay layer.</param>
         * <returns>True if this ability should activate this frame.</returns>
         */
        bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount);

        /**
         * <summary>
         * Called when the ability transitions from inactive to active.
         * Use to set initial state, flags, or one-time velocity impulses.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         */
        void Activate(MotionContext context);

        /**
         * <summary>
         * Called every fixed tick while this ability is active.
         * Use to update internal timers, check exit conditions, and prepare velocity influences.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         * <param name="deltaTime">Fixed delta time for this tick.</param>
         */
        void Tick(MotionContext context, float deltaTime);

        /**
         * <summary>
         * Returns this ability's additive velocity contribution for the current frame.
         * Called by the AbilityInfluenceStage after Tick.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         * <returns>Velocity to add to the context. Return Vector3.zero for no contribution.</returns>
         */
        Vector3 GetVelocityInfluence(MotionContext context);

        /**
         * <summary>
         * Called when the ability transitions from active to inactive.
         * Use to clean up flags, remove temporary modifiers, or restore state.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         */
        void Deactivate(MotionContext context);
    }
}
