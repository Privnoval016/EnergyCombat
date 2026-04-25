using DynamicPhysics.Input;
using UnityEngine;

namespace DynamicPhysics.Core
{
    /**
     * <summary>
     * Shared mutable state object that flows through the entire motion pipeline each fixed tick.
     * Every stage reads and writes into this context additively or transformationally.
     * No stage should destructively overwrite another stage's output.
     * </summary>
     *
     * <remarks>
     * Allocated once by the orchestrator and reused every frame to avoid GC pressure.
     * Fields are public for direct access in performance-critical pipeline stages.
     * The orchestrator snapshots current physics state into this context at the start
     * of each tick, and the motor reads the final velocity at the end.
     * </remarks>
     */
    public class MotionContext
    {
        #region Velocity State

        /** <summary>Current velocity being accumulated through the pipeline. Starts as the Rigidbody's velocity.</summary> */
        public Vector3 Velocity;

        /** <summary>Target velocity computed from input steering. Stages may reference this to blend toward.</summary> */
        public Vector3 DesiredVelocity;

        /** <summary>External forces accumulated this frame (knockback, explosions, wind). Integrated by ExternalForceStage.</summary> */
        public Vector3 AccumulatedForce;

        #endregion

        #region Transform State

        /** <summary>Character's world position at the start of this tick.</summary> */
        public Vector3 Position;

        /** <summary>Reference to the character's transform. Used by abilities for raycasts and spatial queries.</summary> */
        public Transform CharacterTransform;

        #endregion

        #region Ground State

        /** <summary>Surface normal directly below the character. Zero if not grounded.</summary> */
        public Vector3 GroundNormal;

        /** <summary>World-space point where the ground was detected.</summary> */
        public Vector3 GroundPoint;

        /** <summary>Angle of the ground surface relative to world up, in degrees.</summary> */
        public float GroundAngle;

        #endregion

        #region Physics Parameters

        /** <summary>Multiplier applied to gravity this frame. Modified by modifiers and abilities (e.g., apex hang).</summary> */
        public float GravityScale;

        /** <summary>Cached Rigidbody mass for force-to-acceleration conversion.</summary> */
        public float Mass;

        /** <summary>Fixed delta time for this tick. Cached to avoid repeated property access.</summary> */
        public float DeltaTime;

        #endregion

        #region Context State

        /** <summary>Bitfield of active contextual flags (grounded, dashing, wall contact, etc.).</summary> */
        public MotionFlags Flags;

        /** <summary>Per-frame input snapshot from the input provider.</summary> */
        public MotionInputData Input;

        #endregion

        #region Steering Overrides

        /**
         * <summary>
         * Multiplier on steering responsiveness for this frame. Set by modifiers to reduce or increase control.
         * Default is 1.0. Values below 1.0 reduce control (e.g., during attacks), above 1.0 increase it.
         * </summary>
         */
        public float SteeringMultiplier;

        #endregion

        /**
         * <summary>
         * Resets all frame-transient fields to their defaults. Called by the orchestrator
         * at the start of each fixed tick before snapshotting new state.
         * </summary>
         */
        public void Reset()
        {
            Velocity = Vector3.zero;
            DesiredVelocity = Vector3.zero;
            AccumulatedForce = Vector3.zero;
            GroundNormal = Vector3.zero;
            GroundPoint = Vector3.zero;
            GroundAngle = 0f;
            GravityScale = 1f;
            SteeringMultiplier = 1f;
            Flags = MotionFlags.None;
        }
    }
}
