using Extensions.EntityComponent;

namespace Combat
{
    /**
     * <summary>
     * Marker interface for components that can be registered on a <c>CombatController</c>
     * via its <c>Entity&lt;ICombatComponent&gt;</c> component bus.
     * </summary>
     *
     * <remarks>
     * Implement this alongside <see cref="Extensions.EntityComponent.IComponent"/> to
     * register custom combat subsystems (e.g. stamina, status effects, special meter)
     * on a controller without modifying <c>CombatController</c> itself.
     * </remarks>
     */
    public interface ICombatComponent : IComponent
    {
    }
}
