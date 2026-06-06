namespace Combat
{
    /**
     * <summary>
     * Implemented by any entity that can receive and process a hit from the combat system.
     * </summary>
     *
     * <remarks>
     * Components implementing this interface are called by <c>WeaponHitboxController</c>
     * when a physics overlap detects a valid target. Implementations apply damage,
     * trigger hit reactions, and broadcast <c>KillEvent</c> if health reaches zero.
     * </remarks>
     */
    public interface IHittable
    {
        /**
         * <summary>
         * Processes an incoming hit. Called at most once per ability execution per target.
         * </summary>
         * <param name="hit">Fully populated hit data including computed damage and attack tags.</param>
         */
        void ReceiveHit(HitData hit);
    }
}
