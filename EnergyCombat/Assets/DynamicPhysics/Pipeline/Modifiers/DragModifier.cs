using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Applies velocity damping to simulate air resistance or surface friction.
     * Velocity is multiplied by (1 - drag * dt) each tick, producing exponential decay.
     * </summary>
     */
    public class DragModifier : IMotionModifier
    {
        /** <summary>Execution order within the modifier stage.</summary> */
        public int Order { get; }

        /** <summary>Drag coefficient. Higher values produce stronger damping.</summary> */
        public float Drag { get; set; }

        /** <summary>If true, drag is only applied to horizontal velocity.</summary> */
        public bool HorizontalOnly { get; set; }

        public DragModifier(float drag, bool horizontalOnly = false, int order = 50)
        {
            Drag = drag;
            HorizontalOnly = horizontalOnly;
            Order = order;
        }

        public void Apply(MotionContext context)
        {
            float factor = Mathf.Max(0f, 1f - Drag * context.DeltaTime);

            if (HorizontalOnly)
            {
                context.Velocity.x *= factor;
                context.Velocity.z *= factor;
            }
            else
            {
                context.Velocity *= factor;
            }
        }
    }
}
