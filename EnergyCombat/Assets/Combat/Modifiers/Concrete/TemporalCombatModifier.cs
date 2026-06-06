using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Wraps any <see cref="ICombatModifier"/> and adds a finite expiration time.
     * The inner modifier is active until the duration elapses, after which
     * <see cref="IsExpired"/> returns <c>true</c> and the container prunes it.
     * </summary>
     *
     * <remarks>
     * Mirrors the <c>TemporalModifier</c> pattern from <c>DynamicPhysics</c>.
     * Use this to apply time-limited effects without manual timer management in callers.
     * </remarks>
     */
    public class TemporalCombatModifier : ICombatModifier
    {
        private readonly ICombatModifier _inner;
        private readonly float _expirationTime;

        /**
         * <summary>Creates a temporal wrapper around an existing modifier.</summary>
         * <param name="inner">The modifier to wrap.</param>
         * <param name="duration">How many seconds before the modifier expires.</param>
         */
        public TemporalCombatModifier(ICombatModifier inner, float duration)
        {
            _inner = inner;
            _expirationTime = Time.time + duration;
        }

        /** <inheritdoc /> */
        public Tag[] Tags => _inner?.Tags;

        /** <summary>Returns <c>true</c> once the specified duration has elapsed.</summary> */
        public bool IsExpired => Time.time >= _expirationTime;

        /** <summary>Remaining seconds before expiry.</summary> */
        public float RemainingTime => Mathf.Max(0f, _expirationTime - Time.time);

        /** <inheritdoc /> */
        public string DebugLabel => _inner != null
            ? $"{_inner.DebugLabel} [{RemainingTime:F1}s]"
            : $"Temporal [{RemainingTime:F1}s]";

        /** <summary>The modifier this wrapper delegates to.</summary> */
        public ICombatModifier Inner => _inner;
    }
}
