using System.Collections.Generic;
using UnityEngine;

namespace DynamicPhysics
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
     * Tags replace a hardcoded flags enum — any system can set arbitrary string tags.
     * </remarks>
     */
    public class MotionContext
    {
        #region Velocity State

        /** <summary>Current velocity being accumulated through the pipeline.</summary> */
        public Vector3 Velocity;

        /** <summary>Target velocity computed from input steering.</summary> */
        public Vector3 DesiredVelocity;

        /** <summary>External forces accumulated this frame. Integrated by ExternalForceStage.</summary> */
        public Vector3 AccumulatedForce;

        #endregion

        #region Transform State

        /** <summary>Character's world position at the start of this tick.</summary> */
        public Vector3 Position;

        /** <summary>Reference to the character's transform. Used by abilities for raycasts.</summary> */
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

        /** <summary>Multiplier applied to gravity this frame. Modified by modifiers and abilities.</summary> */
        public float GravityScale;

        /** <summary>Cached Rigidbody mass for force-to-acceleration conversion.</summary> */
        public float Mass;

        /** <summary>Fixed delta time for this tick.</summary> */
        public float DeltaTime;

        #endregion

        #region Tags

        private readonly HashSet<string> _tags = new(16);

        /**
         * <summary>Checks whether a tag is currently active.</summary>
         * <param name="tag">The tag to check. Use <see cref="MotionTag"/> constants or custom strings.</param>
         * <returns>True if the tag is present.</returns>
         */
        public bool HasTag(string tag) => _tags.Contains(tag);

        /**
         * <summary>Sets (adds) a tag on this context.</summary>
         * <param name="tag">The tag to set.</param>
         */
        public void SetTag(string tag) => _tags.Add(tag);

        /**
         * <summary>Removes a tag from this context.</summary>
         * <param name="tag">The tag to remove.</param>
         */
        public void RemoveTag(string tag) => _tags.Remove(tag);

        /** <summary>Removes all tags.</summary> */
        public void ClearTags() => _tags.Clear();

        #endregion

        #region Context State

        /** <summary>Per-frame input snapshot from the input provider.</summary> */
        public MotionInputData Input;

        /**
         * <summary>
         * Multiplier on steering responsiveness for this frame.
         * Default is 1.0. Values below 1.0 reduce control.
         * </summary>
         */
        public float SteeringMultiplier;

        /**
         * <summary>
         * Control factor based on current speed and air state.
         * Combines speed-dependent control, air control, and contextual modifiers.
         * Set by InputSteeringStage, used by PlayerRotationStage for dampened rotation.
         * </summary>
         */
        public float ControlFactor;

        /** <summary>Direction the character should face. Set by PlayerRotationStage, used by rotation application.</summary> */
        public Vector3 DesiredFacingDirection;

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
            ControlFactor = 1f;
            DesiredFacingDirection = Vector3.zero;
            _tags.Clear();
        }
    }
}
