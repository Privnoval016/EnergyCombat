using System;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * A strongly-typed tag identifier that wraps a string value and provides value-based equality.
     * Tags are the universal classification primitive used across the motion, combat, and animation systems.
     * </summary>
     *
     * <remarks>
     * Tags replace raw string comparisons throughout the codebase while remaining fully interoperable
     * with string-based APIs via implicit conversion from <c>string</c>. The struct is immutable and
     * allocation-free when used in <c>HashSet&lt;Tag&gt;</c>.
     *
     * Tags are descriptive rather than behavioral — they describe what something <em>is</em>,
     * not what it <em>does</em>. Logic should query tags rather than encode behaviour into them.
     * </remarks>
     */
    [Serializable]
    public struct Tag : IEquatable<Tag>
    {
        [SerializeField] private string _value;

        /** <summary>Creates a new tag wrapping the specified string identifier.</summary> */
        public Tag(string value)
        {
            _value = value ?? string.Empty;
        }

        /** <summary>Implicit conversion from <c>string</c> for convenient inline use.</summary> */
        public static implicit operator Tag(string s) => new Tag(s);

        /** <summary>Explicit conversion to <c>string</c> for display.</summary> */
        public static explicit operator string(Tag t) => t._value ?? string.Empty;

        /** <inheritdoc /> */
        public bool Equals(Tag other) =>
            string.Equals(_value, other._value, StringComparison.Ordinal);

        /** <inheritdoc /> */
        public override bool Equals(object obj) => obj is Tag other && Equals(other);

        /** <inheritdoc /> */
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;

        /** <summary>Returns the underlying string identifier.</summary> */
        public override string ToString() => _value ?? string.Empty;

        /** <summary>Structural equality operator.</summary> */
        public static bool operator ==(Tag left, Tag right) => left.Equals(right);

        /** <summary>Structural inequality operator.</summary> */
        public static bool operator !=(Tag left, Tag right) => !left.Equals(right);
    }
}
