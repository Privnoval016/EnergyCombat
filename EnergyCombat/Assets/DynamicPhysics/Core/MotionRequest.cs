using UnityEngine;

namespace DynamicPhysics.Core
{
    /**
     * <summary>
     * Represents a discrete intent from the gameplay layer.
     * Requests are queued by the orchestrator and consumed by abilities during pipeline execution.
     * They never directly modify physics state.
     * </summary>
     */
    public struct MotionRequest
    {
        /** <summary>The type of motion being requested.</summary> */
        public MotionRequestType Type;

        /** <summary>Desired direction for directional requests (dash, grapple). May be zero for non-directional requests.</summary> */
        public Vector3 Direction;

        /** <summary>Strength or speed parameter. Interpretation depends on the request type.</summary> */
        public float Magnitude;

        /** <summary>Time.time when the request was created. Used for input buffering windows.</summary> */
        public float Timestamp;

        /** <summary>Optional object reference for requests that target a specific object (e.g., rope effector).</summary> */
        public Object Target;

        public MotionRequest(MotionRequestType type)
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
     * Categorizes the type of a motion request. Abilities use this to filter
     * which requests they should consume.
     * </summary>
     */
    public enum MotionRequestType
    {
        Jump,
        Dash,
        Slide,
        RopeAttach,
        RopeDetach,
        AbilityOverride
    }
}
