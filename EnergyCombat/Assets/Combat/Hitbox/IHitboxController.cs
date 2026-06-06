using System;

namespace Combat
{
    /**
     * <summary>
     * Manages the lifecycle of a hitbox during an ability's active phase.
     * </summary>
     *
     * <remarks>
     * The hitbox controller is the <em>only</em> place that performs physics overlap
     * queries and calls <see cref="IHittable.ReceiveHit"/>.
     * Pipeline phases interact with it exclusively through this interface, keeping
     * hit detection decoupled from execution logic.
     *
     * Each <see cref="WeaponHitboxController"/> has a <see cref="HitboxId"/> string that
     * lets <see cref="ActivePhase"/> look it up by name via <c>CombatController.GetHitboxController</c>.
     * This supports multiple distinct hitboxes per character (e.g. sword tip, guard, kick).
     * </remarks>
     */
    public interface IHitboxController
    {
        /**
         * <summary>
         * Unique identifier for this hitbox, matched against <see cref="HitboxActivation.HitboxId"/>
         * in <see cref="ActivePhase"/>. Typically the weapon or body-part name: "Sword", "Kick", "Shield".
         * </summary>
         */
        string HitboxId { get; }

        /**
         * <summary>
         * Activates the hitbox with the specified configuration and execution context.
         * The controller begins performing overlap detection each physics tick.
         * </summary>
         */
        void Activate(HitboxConfig config, CombatContext context);

        /**
         * <summary>
         * Deactivates the hitbox and stops all overlap detection.
         * Safe to call multiple times or when not active.
         * </summary>
         */
        void Deactivate();

        /** <summary>Fired when a valid hit is registered.</summary> */
        event Action<HitData> OnHit;

        /** <summary><c>true</c> while the hitbox is actively checking for targets.</summary> */
        bool IsActive { get; }
    }
}
