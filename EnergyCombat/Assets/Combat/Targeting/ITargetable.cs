using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * Represents a single point in the world that the targeting system can select.
     * Attach <see cref="TargetCandidate"/> to any GameObject to implement this.
     * For multi-point entities (bosses, large enemies) each body part has its own
     * <c>ITargetable</c> and they all share a <see cref="ITargetableEntity"/> parent.
     * </summary>
     */
    public interface ITargetable
    {
        /** <summary>World-space position the camera ring and homing logic should aim at (e.g. chest height).</summary> */
        Vector3 TargetPosition { get; }

        /** <summary>The transform this target point is attached to. Used for smooth UI follow.</summary> */
        Transform TargetTransform { get; }

        /**
         * <summary>
         * Whether this point can currently be selected. Return <c>false</c> for dead enemies,
         * temporarily invulnerable phases, or disabled game objects.
         * </summary>
         */
        bool IsTargetable { get; }

        /**
         * <summary>
         * The logical entity that owns this target point, or <c>null</c> for standalone objects.
         * Used to group multiple points on a boss into one enemy.
         * </summary>
         */
        ITargetableEntity ParentEntity { get; }
    }
}
