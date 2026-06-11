using System.Collections.Generic;

namespace Combat.Targeting
{
    /**
     * <summary>
     * Global static registry of all active <see cref="ITargetable"/> instances in the scene.
     * <see cref="TargetCandidate"/> registers itself on enable and unregisters on disable,
     * so the set always reflects the current live targetables with zero per-frame allocation.
     * </summary>
     *
     * <remarks>
     * The registry is application-lifetime; it does not reset between scenes. If a scene
     * unloads and destroys <see cref="TargetCandidate"/> GameObjects, their <c>OnDisable</c>
     * callbacks remove them automatically.
     * </remarks>
     */
    public static class TargetingRegistry
    {
        private static readonly HashSet<ITargetable> _candidates = new();

        /** <summary>Adds a target point to the registry. Called automatically by <see cref="TargetCandidate"/>.</summary> */
        public static void Register(ITargetable t) => _candidates.Add(t);

        /** <summary>Removes a target point from the registry. Called automatically by <see cref="TargetCandidate"/>.</summary> */
        public static void Unregister(ITargetable t) => _candidates.Remove(t);

        /** <summary>Read-only view of all currently active target points.</summary> */
        public static IReadOnlyCollection<ITargetable> All => _candidates;
    }
}
