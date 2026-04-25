using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Pipeline.Modifiers
{
    /**
     * <summary>
     * Wraps any <see cref="IMotionModifier"/> with a time-limited duration.
     * Automatically expires after the configured duration, at which point the
     * <see cref="ModifierStage"/> skips it and the orchestrator cleans it up.
     * </summary>
     *
     * <remarks>
     * Essential for combat mechanics: hit reactions, attack lunges, dash windows,
     * and temporary gravity reduction during aerial attacks. Avoids state explosion
     * by using timed modifiers instead of state transitions.
     * </remarks>
     */
    public class TemporalModifier : IMotionModifier
    {
        /** <summary>Execution order — delegates to the wrapped modifier.</summary> */
        public int Order => _inner.Order;

        /** <summary>Whether this modifier has exceeded its duration.</summary> */
        public bool IsExpired => Time.time >= _expirationTime;

        /** <summary>Remaining duration in seconds. Zero or negative if expired.</summary> */
        public float RemainingTime => Mathf.Max(0f, _expirationTime - Time.time);

        private readonly IMotionModifier _inner;
        private readonly float _expirationTime;

        /**
         * <summary>
         * Creates a time-limited wrapper around a modifier.
         * </summary>
         *
         * <param name="inner">The modifier to wrap.</param>
         * <param name="duration">Duration in seconds before expiration.</param>
         */
        public TemporalModifier(IMotionModifier inner, float duration)
        {
            _inner = inner;
            _expirationTime = Time.time + duration;
        }

        /**
         * <summary>
         * Delegates to the wrapped modifier's Apply method.
         * The <see cref="ModifierStage"/> checks <see cref="IsExpired"/> before calling this.
         * </summary>
         */
        public void Apply(MotionContext context)
        {
            _inner.Apply(context);
        }
    }
}
