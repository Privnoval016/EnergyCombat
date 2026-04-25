using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Represents a discrete movement ability (jump, dash, wall run, etc.) that can
     * be activated, ticked, and deactivated. Abilities contribute velocity influences
     * additively to the <see cref="MotionContext"/> — they never directly set final velocity.
     * </summary>
     *
     * <remarks>
     * Abilities are registered with the <see cref="MotionOrchestrator"/> and persist across frames.
     * The orchestrator routes all <see cref="MotionRequest"/> objects through abilities:
     * <list type="number">
     *   <item>Active abilities receive requests via <see cref="TryConsumeRequest"/> first (e.g., wall run intercepts jump → wall jump).</item>
     *   <item>Unconsumed requests are passed to inactive abilities via <see cref="CanActivate"/>.</item>
     * </list>
     * This ensures all request handling lives in the ability layer, not the orchestrator.
     * </remarks>
     */
    public interface IMotionAbility
    {
        /** <summary>Whether this ability is currently active and contributing to motion.</summary> */
        bool IsActive { get; }

        /**
         * <summary>
         * Called on active abilities before <see cref="CanActivate"/>. Allows an active ability
         * to intercept and consume a request meant for another ability.
         * Return true to consume the request (it won't be passed further).
         * </summary>
         *
         * <example>
         * WallRunAbility intercepts Jump requests to perform a wall jump instead of a normal jump.
         * </example>
         *
         * <param name="context">Current motion state.</param>
         * <param name="request">The request to potentially consume.</param>
         * <returns>True if this ability consumed the request.</returns>
         */
        bool TryConsumeRequest(MotionContext context, MotionRequest request);

        /**
         * <summary>
         * Checks whether this ability can activate given the current motion state and pending requests.
         * Called every tick for inactive abilities with requests that were not consumed.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         * <param name="requests">Pending unconsumed motion requests.</param>
         * <param name="requestCount">Number of valid entries in the requests array.</param>
         * <returns>True if this ability should activate this frame.</returns>
         */
        bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount);

        /**
         * <summary>
         * Called when the ability transitions from inactive to active.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         */
        void Activate(MotionContext context);

        /**
         * <summary>
         * Called every fixed tick while this ability is active.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         * <param name="deltaTime">Fixed delta time for this tick.</param>
         */
        void Tick(MotionContext context, float deltaTime);

        /**
         * <summary>
         * Returns this ability's additive velocity contribution for the current frame.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         * <returns>Velocity to add. Return Vector3.zero for no contribution.</returns>
         */
        Vector3 GetVelocityInfluence(MotionContext context);

        /**
         * <summary>
         * Called when the ability transitions from active to inactive.
         * </summary>
         *
         * <param name="context">Current motion state.</param>
         */
        void Deactivate(MotionContext context);
    }
}
