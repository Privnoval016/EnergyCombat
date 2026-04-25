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

        /**
         * <summary>
         * Steering responsiveness at maximum speed. Lower values create more sliding/drifting at high speed.
         * Range: 0 (no control) to 1 (full control).
         * </summary>
         */
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
            LowSpeedControl = 1f
        };
    }
}
