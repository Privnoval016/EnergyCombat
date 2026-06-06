using System;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * A strongly-typed, serializable identifier for a named stat.
     * Analogous to <see cref="Tag"/> but scoped exclusively to the stat system.
     * </summary>
     *
     * <remarks>
     * Stats are identified by name strings rather than enums so the stat system is
     * open for extension without modifying core code. Equality is ordinal string comparison.
     *
     * The struct is serializable so it displays as a text field in the Unity Inspector
     * on <see cref="AbilityStatEntry"/> and similar data classes.
     * </remarks>
     */
    [Serializable]
    public struct StatId : IEquatable<StatId>
    {
        [SerializeField] private string _name;

        /** <summary>Creates a new stat identifier.</summary> */
        public StatId(string name)
        {
            _name = name ?? string.Empty;
        }

        /** <summary>Implicit conversion from <c>string</c> for convenient inline use.</summary> */
        public static implicit operator StatId(string s) => new StatId(s);

        /** <inheritdoc /> */
        public bool Equals(StatId other) =>
            string.Equals(_name, other._name, StringComparison.Ordinal);

        /** <inheritdoc /> */
        public override bool Equals(object obj) => obj is StatId other && Equals(other);

        /** <inheritdoc /> */
        public override int GetHashCode() => _name?.GetHashCode() ?? 0;

        /** <summary>Returns the stat name.</summary> */
        public override string ToString() => _name ?? string.Empty;

        /** <summary>Structural equality operator.</summary> */
        public static bool operator ==(StatId left, StatId right) => left.Equals(right);

        /** <summary>Structural inequality operator.</summary> */
        public static bool operator !=(StatId left, StatId right) => !left.Equals(right);

        // ── Built-in stat identifiers ──────────────────────────────────────────────────

        /** <summary>Raw damage dealt on a successful hit.</summary> */
        public static readonly StatId Damage = new StatId("Damage");

        /** <summary>Multiplier on animation playback speed for attack phases.</summary> */
        public static readonly StatId AttackSpeed = new StatId("AttackSpeed");

        /** <summary>Maximum reach of this attack in world units.</summary> */
        public static readonly StatId Range = new StatId("Range");

        /** <summary>Impulse applied to the target on hit.</summary> */
        public static readonly StatId Knockback = new StatId("Knockback");

        /** <summary>Stamina cost to activate this ability.</summary> */
        public static readonly StatId StaminaCost = new StatId("StaminaCost");

        /** <summary>Frames (at 60 Hz) the target is locked in a hit reaction.</summary> */
        public static readonly StatId HitStun = new StatId("HitStun");

        /** <summary>Frames (at 60 Hz) an opponent is locked if this attack is blocked.</summary> */
        public static readonly StatId BlockStun = new StatId("BlockStun");
    }
}
