
namespace DynamicPhysics
{
    /**
     * <summary>
     * Multiplies the motion context's gravity scale by a configurable factor.
     * Use to reduce gravity during apex hang, attacks, or to increase it for fast-fall.
     * </summary>
     */
    public class GravityScaleModifier : IMotionModifier
    {
        /** <summary>Execution order within the modifier stage.</summary> */
        public int Order { get; }

        /** <summary>Multiplier applied to the context's gravity scale.</summary> */
        public float ScaleMultiplier { get; set; }

        public GravityScaleModifier(float scaleMultiplier, int order = 0)
        {
            ScaleMultiplier = scaleMultiplier;
            Order = order;
        }

        public void Apply(MotionContext context)
        {
            context.GravityScale *= ScaleMultiplier;
        }
    }
}
