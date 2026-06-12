using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Tracks recent combat input using a per-button state model inspired by
     * action games (DMC, NieR, MGR): one slot per button, last press wins,
     * explicit consumption when an ability fires.
     * </summary>
     *
     * <remarks>
     * Each button maintains independent state — <see cref="HasRecentInput"/> returns
     * <c>true</c> only until <see cref="ConsumeInput"/> is called, preventing the same
     * press from triggering multiple abilities across frames.
     *
     * A small ring buffer is kept in parallel solely for <see cref="GetRecentEvents"/>
     * debug display; it is not used for resolution logic.
     *
     * Grace windows default to <see cref="CombatInputSettings.GraceWindow"/> /
     * <see cref="CombatInputSettings.HoldReleaseGraceWindow"/> when settings are provided,
     * or to hard-coded fallbacks when the settings asset is null.
     * </remarks>
     */
    public class CombatInputBuffer
    {
        // ── Per-button state ──────────────────────────────────────────────────

        private struct ButtonState
        {
            /** unscaledTime of the most recent Started or Performed event. */
            public float LastPressTime;
            /** Set to LastPressTime when this press is consumed; prevents re-fire. */
            public float ConsumedTime;
            /** Timestamp when the button was first pressed (for hold duration). */
            public float PressStartTime;
            /** True while the button is still physically held. */
            public bool IsHeld;
            /** unscaledTime of the most recent release. */
            public float LastReleaseTime;
            /** Hold duration recorded at release time. */
            public float LastHoldDuration;
            /** True once the most recent release has been consumed. */
            public bool ReleaseConsumed;
        }

        private readonly Dictionary<CombatInputButton, ButtonState> _states = new();
        private readonly CombatInputSettings _settings;

        // ── Debug ring buffer ─────────────────────────────────────────────────

        private readonly CombatInputEvent[] _buffer;
        private int _head;
        private int _count;

        // ── Construction ──────────────────────────────────────────────────────

        /**
         * <summary>
         * Creates a new input buffer.
         * </summary>
         *
         * <param name="capacity">Maximum number of events stored in the debug ring.</param>
         * <param name="settings">
         * Optional settings asset. When null, grace windows fall back to 0.5 s.
         * </param>
         */
        public CombatInputBuffer(int capacity = 16, CombatInputSettings settings = null)
        {
            _buffer   = new CombatInputEvent[capacity];
            _settings = settings;
        }

        /** <summary>Number of events currently in the debug ring.</summary> */
        public int Count => _count;

        // ── Grace window accessors ────────────────────────────────────────────

        private float GraceWindow           => _settings != null ? _settings.GraceWindow           : 0.5f;
        private float HoldReleaseGraceWindow => _settings != null ? _settings.HoldReleaseGraceWindow : 0.5f;

        // ── Input push ────────────────────────────────────────────────────────

        /**
         * <summary>
         * Records an incoming input event. Updates per-button state and appends to the debug ring.
         * </summary>
         */
        public void Push(CombatInputEvent evt)
        {
            float now = evt.Timestamp;

            _states.TryGetValue(evt.Button, out ButtonState state);

            if (evt.Phase == CombatInputPhase.Started)
            {
                if (!state.IsHeld)
                {
                    // Fresh press — button was not held before this event.
                    state.PressStartTime = now;
                    state.IsHeld         = true;
                    state.LastPressTime  = now;
                    state.ConsumedTime   = float.NegativeInfinity;
                }
                // If IsHeld is already true, a prior Started already registered this press.
                // A duplicate Started without an intervening Canceled is a hardware bounce
                // or an Input System re-fire — ignore it so consumption is not reset.
            }
            else if (evt.Phase == CombatInputPhase.Performed)
            {
                if (!state.IsHeld)
                {
                    // Performed arrived without a prior Started (some devices skip Started).
                    // Treat it as the initial press.
                    state.PressStartTime = now;
                    state.IsHeld         = true;
                    state.LastPressTime  = now;
                    state.ConsumedTime   = float.NegativeInfinity;
                }
                // If IsHeld is already true a Started already registered this press.
                // Performed is a hold-continuation in that case — do not reset consumption
                // or the same physical click would appear as two separate presses.
            }

            if (evt.Phase == CombatInputPhase.Canceled)
            {
                float holdDuration = state.IsHeld ? now - state.PressStartTime : 0f;
                state.LastReleaseTime    = now;
                state.LastHoldDuration   = holdDuration;
                state.ReleaseConsumed    = false;
                state.IsHeld             = false;
            }

            _states[evt.Button] = state;

            // Append to debug ring (always store the enriched hold-duration version).
            CombatInputEvent toStore = evt;
            if (evt.Phase == CombatInputPhase.Canceled && state.LastHoldDuration > 0f)
                toStore = new CombatInputEvent(evt.Button, evt.Phase, evt.Timestamp, state.LastHoldDuration);

            _buffer[_head] = toStore;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
        }

        // ── Tap press queries ─────────────────────────────────────────────────

        /**
         * <summary>
         * Returns <c>true</c> if a press exists for <paramref name="button"/>, it has not
         * yet been consumed, and it occurred within <paramref name="withinSeconds"/> seconds.
         * Passing <c>-1</c> for <paramref name="withinSeconds"/> uses the configured
         * <see cref="CombatInputSettings.GraceWindow"/>.
         * </summary>
         */
        public bool HasRecentInput(CombatInputButton button, float withinSeconds = -1f)
        {
            float window = withinSeconds >= 0f ? withinSeconds : GraceWindow;
            if (!_states.TryGetValue(button, out ButtonState state)) return false;
            if (state.LastPressTime <= state.ConsumedTime) return false;
            return Time.unscaledTime - state.LastPressTime < window;
        }

        /**
         * <summary>
         * Marks the most recent press for <paramref name="button"/> as consumed.
         * Subsequent calls to <see cref="HasRecentInput"/> return <c>false</c> until
         * the button is pressed again.
         * </summary>
         */
        public void ConsumeInput(CombatInputButton button)
        {
            if (!_states.TryGetValue(button, out ButtonState state)) return;
            state.ConsumedTime = state.LastPressTime;
            _states[button] = state;
        }

        // ── Hold / release queries ────────────────────────────────────────────

        /**
         * <summary>
         * Returns how long <paramref name="button"/> has been held since the last press,
         * or <c>0</c> if the button is not currently held.
         * </summary>
         */
        public float GetHoldDuration(CombatInputButton button)
        {
            if (!_states.TryGetValue(button, out ButtonState state)) return 0f;
            if (!state.IsHeld) return 0f;
            return Time.unscaledTime - state.PressStartTime;
        }

        /** <summary>Returns <c>true</c> if <paramref name="button"/> is currently held.</summary> */
        public bool IsHeld(CombatInputButton button)
        {
            return _states.TryGetValue(button, out ButtonState state) && state.IsHeld;
        }

        /**
         * <summary>
         * Returns the most recent release event for <paramref name="button"/> if it
         * has not been consumed and occurred within <paramref name="withinSeconds"/> seconds.
         * Passing <c>-1</c> uses the configured <see cref="CombatInputSettings.HoldReleaseGraceWindow"/>.
         * </summary>
         */
        public CombatInputEvent? GetLatestRelease(CombatInputButton button, float withinSeconds = -1f)
        {
            float window = withinSeconds >= 0f ? withinSeconds : HoldReleaseGraceWindow;
            if (!_states.TryGetValue(button, out ButtonState state)) return null;
            if (state.IsHeld) return null;
            if (state.ReleaseConsumed) return null;
            if (Time.unscaledTime - state.LastReleaseTime >= window) return null;
            return new CombatInputEvent(
                button,
                CombatInputPhase.Canceled,
                state.LastReleaseTime,
                state.LastHoldDuration);
        }

        /**
         * <summary>
         * Marks the most recent release for <paramref name="button"/> as consumed.
         * Subsequent calls to <see cref="GetLatestRelease"/> return <c>null</c> until
         * the button is pressed and released again.
         * </summary>
         */
        public void ConsumeRelease(CombatInputButton button)
        {
            if (!_states.TryGetValue(button, out ButtonState state)) return;
            state.ReleaseConsumed = true;
            _states[button] = state;
        }

        // ── Debug ring helpers ────────────────────────────────────────────────

        /** <summary>Returns the most recently pushed event, or null if empty.</summary> */
        public CombatInputEvent? PeekLatest()
        {
            if (_count == 0) return null;
            int index = (_head - 1 + _buffer.Length) % _buffer.Length;
            return _buffer[index];
        }

        /**
         * <summary>
         * Removes entries from the debug ring that are older than <paramref name="maxAge"/> seconds.
         * Per-button state is not affected.
         * </summary>
         */
        public void PruneOlderThan(float maxAge)
        {
            float cutoff = Time.unscaledTime - maxAge;
            while (_count > 0)
            {
                int oldest = (_head - _count + _buffer.Length * 2) % _buffer.Length;
                if (_buffer[oldest].Timestamp >= cutoff) break;
                _count--;
            }
        }

        /** <summary>Clears all per-button state and the debug ring.</summary> */
        public void Clear()
        {
            _states.Clear();
            _count = 0;
            _head  = 0;
        }

        /**
         * <summary>
         * Returns a snapshot of the last <paramref name="n"/> events from the debug ring,
         * newest first. Does not affect resolution state.
         * </summary>
         */
        public List<CombatInputEvent> GetRecentEvents(int n)
        {
            var result = new List<CombatInputEvent>(Mathf.Min(n, _count));
            for (int i = 0; i < Mathf.Min(n, _count); i++)
            {
                int index = (_head - 1 - i + _buffer.Length * 2) % _buffer.Length;
                result.Add(_buffer[index]);
            }
            return result;
        }
    }
}
