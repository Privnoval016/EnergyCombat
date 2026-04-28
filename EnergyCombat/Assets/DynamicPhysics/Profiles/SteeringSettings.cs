using System;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Serializable configuration for the input steering stage.
     * Controls how responsive movement feels — acceleration, deceleration,
     * turn rate, and speed-dependent control scaling.
     * </summary>
     */
    [Serializable]
    public struct SteeringSettings
    {
        /** <summary>Maximum horizontal movement speed in units per second.</summary> */
        [Header("Move Settings")]
        [Tooltip("Maximum horizontal movement speed in units per second.")]
        public float MaxSpeed;
        
        /** <summary>Rate of velocity increase toward desired speed, in units per second squared.</summary> */
        [Tooltip("Acceleration rate in units/s².")]
        public float Acceleration;

        /** <summary>Rate of velocity decrease when no input is applied, in units per second squared.</summary> */
        [Tooltip("Deceleration rate when releasing input, in units/s².")]
        public float Deceleration;

        /** <summary>Maximum angular reorientation speed in degrees per second.</summary> */
        [Tooltip("Maximum turn rate in degrees per second.")]
        public float MaxTurnRate;

        /**<summary>Maximum horizontal movement speed while sprinting.</summary> */
        [Header("Sprint Settings")] 
        [Tooltip("Maximum horizontal movement speed while sprinting.")]
        public float SprintSpeed;

        /**<summary>Acceleration rate while sprinting in units per second squared.</summary> */
        [Tooltip("Acceleration rate while sprinting in units/s².")]
        public float SprintAcceleration;

        /**
         * <summary>
         * Steering responsiveness at maximum speed. Lower values create more sliding/drifting at high speed.
         * Range: 0 (no control) to 1 (full control).
         * </summary>
         */
        [Header("Control Settings")]
        [Tooltip("Control factor at max speed. Lower = more drift.")]
        [Range(0f, 1f)]
        public float HighSpeedControl;

        /**
         * <summary>
         * Steering responsiveness at zero speed. Higher values produce tight, snappy turns at low speed.
         * Range: 0 (no control) to 1 (full control).
         * </summary>
         */
        [Tooltip("Control factor at zero speed. Higher = snappier turns.")]
        [Range(0f, 1f)]
        public float LowSpeedControl;

        /**
         * <summary>
         * Angle threshold in degrees for triggering hard stop drift.
         * When direction changes exceed this angle at high speed, perform a skidding hard stop.
         * Example: 110 = hard stop triggers when turning > 110 degrees.
         * </summary>
         */
        [Header("Hard Stop Drift")]
        [Tooltip("Angle threshold (degrees) for hard stop drift. Lower = more sensitive.")]
        [Range(30f, 180f)]
        public float HardStopDriftAngleThreshold;

        /**
         * <summary>
         * Minimum speed ratio (current speed / max speed) required to trigger hard stop drift.
         * Example: 0.7 = must be at least 70% of max speed.
         * </summary>
         */
        [Tooltip("Minimum speed ratio to trigger hard stop. 0.6 = 60% of max speed.")]
        [Range(0f, 1f)]
        public float HardStopDriftMinSpeedRatio;

        /**
         * <summary>
         * Deceleration factor applied when hard stopping to overcome current momentum.
         * Higher values = faster friction-based slowdown of opposing velocity.
         * Example: 100 = apply 100 units/s² of opposing friction.
         * </summary>
         */
        [Tooltip("Friction-based deceleration during hard stop. Higher = faster slowdown of current direction.")]
        [Range(0f, 200f)]
        public float HardStopDriftDeceleration;

        /**
         * <summary>
         * Acceleration multiplier applied when hard stopping to accelerate in new direction.
         * Multiplied by normal acceleration. Example: 1.5 = 150% of normal acceleration.
         * Higher values = more aggressive acceleration into new direction.
         * </summary>
         */
        [Tooltip("Acceleration multiplier when transitioning to new direction. 1.0 = normal, 2.0 = double speed.")]
        [Range(0f, 3f)]
        public float HardStopDriftReaccelerationMultiplier;

        /**
         * <summary>
         * Creates a default steering configuration suitable for responsive 3D character movement.
         * </summary>
         */
        public static SteeringSettings Default => new SteeringSettings
        {
            MaxSpeed = 12f,
            Acceleration = 60f,
            Deceleration = 50f,
            MaxTurnRate = 720f,
            HighSpeedControl = 0.35f,
            LowSpeedControl = 1f,
            SprintSpeed = 18f,
            SprintAcceleration = 100f,
            HardStopDriftAngleThreshold = 110f,
            HardStopDriftMinSpeedRatio = 0.7f,
            HardStopDriftDeceleration = 120f,
            HardStopDriftReaccelerationMultiplier = 1.5f,
        };
    }
}
