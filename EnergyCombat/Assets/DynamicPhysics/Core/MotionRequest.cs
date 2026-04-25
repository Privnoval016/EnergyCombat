using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Represents a discrete intent from the gameplay layer.
     * Requests are queued by the orchestrator and routed to abilities for consumption.
     * They never directly modify physics state.
     * </summary>
     */
    public struct MotionRequest
    {
        /** <summary>The type of motion being requested. Use <see cref="MotionRequestType"/> constants or custom strings.</summary> */
        public string Type;

        /** <summary>Desired direction for directional requests. May be zero for non-directional requests.</summary> */
        public Vector3 Direction;

        /** <summary>Strength or speed parameter. Interpretation depends on the request type.</summary> */
        public float Magnitude;

        /** <summary>Time.time when the request was created. Used for input buffering windows.</summary> */
        public float Timestamp;

        /** <summary>Optional object reference for requests that target a specific object.</summary> */
        public Object Target;

        public MotionRequest(string type)
        {
            Type = type;
            Direction = Vector3.zero;
            Magnitude = 0f;
            Timestamp = Time.time;
            Target = null;
        }
    }

    /**
     * <summary>
     * Built-in string constants for common motion request types.
     * Users can define and use any arbitrary string type for custom abilities.
     * </summary>
     */
    public static class MotionRequestType
    {
        public const string Jump = "Jump";
        public const string Dash = "Dash";
        public const string Slide = "Slide";
        public const string RopeAttach = "RopeAttach";
        public const string RopeDetach = "RopeDetach";
    }
}
