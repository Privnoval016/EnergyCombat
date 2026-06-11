using System.Collections.Generic;

namespace Combat.Targeting
{
    /**
     * <summary>
     * Groups one or more <see cref="ITargetable"/> points into a single logical entity.
     * Implement this on the root GameObject of enemies that have multiple targetable parts
     * (e.g. boss limbs, weak spots). Simple enemies do not need this interface.
     * </summary>
     *
     * <remarks>
     * The targeting system operates at the <see cref="ITargetable"/> level — it selects
     * the best individual point, not the best entity. <c>ITargetableEntity</c> provides
     * metadata and is optional; absence does not prevent an object from being targeted.
     * </remarks>
     */
    public interface ITargetableEntity
    {
        /** <summary>The default targeting point. Usually the first entry in <see cref="AllTargetPoints"/>.</summary> */
        ITargetable PrimaryTarget { get; }

        /** <summary>All targetable points on this entity, ordered by priority.</summary> */
        IReadOnlyList<ITargetable> AllTargetPoints { get; }

        /** <summary>False when the entity is dead or otherwise permanently untargetable.</summary> */
        bool IsAlive { get; }

        /** <summary>Human-readable label shown in debug overlays and UI.</summary> */
        string DisplayName { get; }
    }
}
