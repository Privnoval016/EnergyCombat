using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * The sole system that directly interacts with the Unity <see cref="Rigidbody"/>.
     * Receives the resolved velocity from the <see cref="MotionContext"/> and applies it.
     * Contains no gameplay logic, movement rules, or ability knowledge.
     * </summary>
     *
     * <remarks>
     * Unity's built-in gravity is disabled on initialization since gravity is handled
     * by the <see cref="Pipeline.Stages.GravityStage"/>. The motor configures the Rigidbody
     * for deterministic fixed-timestep integration with interpolation for smooth rendering.
     * </remarks>
     */
    public class PhysicsMotor
    {
        private Rigidbody _rigidbody;

        /** <summary>Whether the motor has been initialized with a valid Rigidbody.</summary> */
        public bool IsInitialized => _rigidbody != null;

        /**
         * <summary>
         * Initializes the motor with a Rigidbody. Configures it for pipeline-driven movement:
         * disables Unity gravity, sets interpolation mode, and freezes rotation.
         * </summary>
         *
         * <param name="rigidbody">The Rigidbody to control. Must not be null.</param>
         */
        public void Initialize(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;

            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.freezeRotation = true;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        /**
         * <summary>
         * Applies the resolved velocity from the motion context to the Rigidbody.
         * This is the final step of the motion pipeline each fixed tick.
         * </summary>
         *
         * <param name="context">The motion context containing the final resolved velocity.</param>
         */
        public void ApplyVelocity(MotionContext context)
        {
            _rigidbody.linearVelocity = context.Velocity;
        }

        /**
         * <summary>
         * Snapshots the current Rigidbody state into the motion context.
         * Called at the start of each tick before pipeline execution.
         * </summary>
         *
         * <param name="context">The motion context to populate.</param>
         */
        public void SnapshotState(MotionContext context)
        {
            context.Velocity = _rigidbody.linearVelocity;
            context.Position = _rigidbody.position;
            context.Mass = _rigidbody.mass;
            context.CharacterTransform = _rigidbody.transform;
        }
    }
}
