using System;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Data describing the shape, position, and targeting rules for a hitbox.
     * Used by <see cref="IHitboxController.Activate"/> during an active phase.
     * </summary>
     *
     * <remarks>
     * All shape parameters are in the local space of the hitbox controller's
     * <c>Transform</c>. The controller applies the transform at query time, allowing
     * the hitbox to follow the weapon bone naturally.
     * </remarks>
     */
    [Serializable]
    public class HitboxConfig
    {
        /** <summary>The geometric shape used for overlap detection.</summary> */
        [Header("Shape")]
        public HitboxShape Shape = HitboxShape.Sphere;

        /** <summary>Local-space offset from the controller's origin to the hitbox center.</summary> */
        [Tooltip("Local-space offset to the hitbox center.")]
        public Vector3 Offset = Vector3.zero;

        /** <summary>Radius used for Sphere and Capsule shapes.</summary> */
        [Tooltip("Sphere radius, or Capsule cross-section radius.")]
        public float Radius = 0.3f;

        /** <summary>Half-extent along the Capsule or Box long axis in local space.</summary> */
        [Tooltip("Capsule half-height, or Box half-extents Y.")]
        public float HalfHeight = 0.5f;

        /**
         * <summary>
         * Second endpoint offset for Capsule shape, relative to the controller origin.
         * Ignored for Sphere and Box.
         * </summary>
         */
        [Tooltip("Second capsule endpoint in local space. Used only for Capsule shape.")]
        public Vector3 CapsuleEndOffset = new Vector3(0f, 0.5f, 0f);

        /** <summary>Box half-extents in local space. Used only for Box shape.</summary> */
        [Tooltip("Box half-extents in local space. Used only for Box shape.")]
        public Vector3 BoxHalfExtents = new Vector3(0.3f, 0.5f, 0.1f);

        /** <summary>Layers considered valid targets.</summary> */
        [Header("Targeting")]
        [Tooltip("Physics layers to check for targets.")]
        public LayerMask TargetLayers = ~0;

        /**
         * <summary>
         * Optional tag filter applied to any <see cref="ICombatTarget"/> found by the overlap.
         * Hits that don't satisfy this query are discarded.
         * </summary>
         */
        [Tooltip("Tag query filter on ICombatTarget. Empty query = hit everything.")]
        public TagQuery TargetFilter;

        /**
         * <summary>
         * Maximum number of distinct targets that can be hit per activation.
         * 0 means unlimited.
         * </summary>
         */
        [Tooltip("Max targets per activation. 0 = unlimited.")]
        public int MaxHitsPerSwing;
    }

    /**
     * <summary>
     * Links a named <see cref="IHitboxController"/> to a <see cref="HitboxConfig"/> for
     * activation during an <see cref="ActivePhase"/>. One entry per hitbox per phase.
     * </summary>
     *
     * <remarks>
     * Set <see cref="HitboxId"/> to match the <c>WeaponHitboxController.HitboxId</c> value
     * on the corresponding GameObject in the character prefab.
     * Multiple <c>HitboxActivation</c> entries on one <see cref="ActivePhase"/> activate
     * all named hitboxes simultaneously (e.g. both fists for a two-hit strike).
     * </remarks>
     */
    [Serializable]
    public class HitboxActivation
    {
        /**
         * <summary>
         * Must match <c>WeaponHitboxController.HitboxId</c> on the target controller.
         * Examples: "Sword", "LeftFist", "Shield".
         * </summary>
         */
        [Tooltip("Must match WeaponHitboxController.HitboxId on the weapon bone.")]
        public string HitboxId = "Default";

        /** <summary>Shape and targeting config for this hitbox during the active phase.</summary> */
        public HitboxConfig Config;
    }

    /** <summary>Geometric shape used for hitbox overlap detection.</summary> */
    public enum HitboxShape
    {
        Sphere,
        Capsule,
        Box
    }
}
