using Extensions.EventBus;

namespace Combat
{
    /**
     * <summary>
     * Marker interface for all events raised by the combat system.
     * Extends <see cref="IEvent"/> so combat events are fully compatible with
     * <see cref="Extensions.EventBus.EventBus{T}"/>.
     * </summary>
     */
    public interface ICombatEvent : IEvent
    {
    }
}
