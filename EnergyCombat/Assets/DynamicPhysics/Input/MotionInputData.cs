using UnityEngine;

namespace DynamicPhysics.Input
{
    /**
     * <summary>
     * Immutable per-frame snapshot of player input state.
     * Captured once at the start of each fixed tick and read by all pipeline stages.
     * </summary>
     *
     * <remarks>
     * This is a value type to avoid heap allocations. Keep it small — only include
     * data that the motion pipeline needs. Game-specific input (attack, interact, etc.)
     * should remain in the gameplay layer.
     * </remarks>
     */
    public struct MotionInputData
    {
        /** <summary>Normalized movement input from stick or WASD. X = right, Y = forward.</summary> */
        public Vector2 MoveInput;

        /** <summary>Whether the jump button is currently held. Used for variable jump height.</summary> */
        public bool JumpHeld;

        /** <summary>Camera forward direction projected onto the horizontal plane. Used for camera-relative movement.</summary> */
        public Vector3 CameraForward;

        /** <summary>Camera right direction projected onto the horizontal plane. Used for camera-relative movement.</summary> */
        public Vector3 CameraRight;
    }
}
