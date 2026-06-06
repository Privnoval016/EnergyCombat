using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * A fixed-capacity circular buffer that stores recent <see cref="CombatInputEvent"/>
     * records with timestamps, enabling combo resolution and hold-detection queries.
     * </summary>
     *
     * <remarks>
     * Each button's in-flight press time is tracked separately so that
     * <see cref="GetHoldDuration"/> can be queried while a button is still held.
     * The buffer uses a plain array with a head pointer — no heap allocation after construction.
     * </remarks>
     */
    public class CombatInputBuffer
    {
        private readonly CombatInputEvent[] _buffer;
        private int _head;
        private int _count;

        private readonly Dictionary<CombatInputButton, float> _pressStartTimes = new();

        /** <summary>Creates a new input buffer with the specified capacity.</summary> */
        public CombatInputBuffer(int capacity = 16)
        {
            _buffer = new CombatInputEvent[capacity];
        }

        /** <summary>Number of events currently in the buffer.</summary> */
        public int Count => _count;

        /**
         * <summary>Pushes a new event into the buffer, overwriting oldest if full.</summary>
         */
        public void Push(CombatInputEvent evt)
        {
            if (evt.Phase == CombatInputPhase.Started)
                _pressStartTimes[evt.Button] = evt.Timestamp;

            CombatInputEvent toStore = evt;

            if (evt.Phase == CombatInputPhase.Canceled &&
                _pressStartTimes.TryGetValue(evt.Button, out float startTime))
            {
                float duration = evt.Timestamp - startTime;
                toStore = new CombatInputEvent(evt.Button, evt.Phase, evt.Timestamp, duration);
                _pressStartTimes.Remove(evt.Button);
            }

            _buffer[_head] = toStore;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
        }

        /** <summary>Returns the most recently pushed event, or null if the buffer is empty.</summary> */
        public CombatInputEvent? PeekLatest()
        {
            if (_count == 0) return null;
            int index = (_head - 1 + _buffer.Length) % _buffer.Length;
            return _buffer[index];
        }

        /**
         * <summary>
         * Checks whether the buffer contains a <c>Performed</c> or <c>Started</c> event for
         * the specified button within the last <paramref name="withinSeconds"/> seconds.
         * </summary>
         */
        public bool HasRecentInput(CombatInputButton button, float withinSeconds)
        {
            float cutoff = Time.unscaledTime - withinSeconds;
            for (int i = 0; i < _count; i++)
            {
                int index = (_head - 1 - i + _buffer.Length * 2) % _buffer.Length;
                var evt = _buffer[index];
                if (evt.Timestamp < cutoff) break;
                if (evt.Button == button &&
                    (evt.Phase == CombatInputPhase.Performed || evt.Phase == CombatInputPhase.Started))
                    return true;
            }
            return false;
        }

        /**
         * <summary>Returns the most recent <c>Canceled</c> event for the button within the window.</summary>
         */
        public CombatInputEvent? GetLatestRelease(CombatInputButton button, float withinSeconds)
        {
            float cutoff = Time.unscaledTime - withinSeconds;
            for (int i = 0; i < _count; i++)
            {
                int index = (_head - 1 - i + _buffer.Length * 2) % _buffer.Length;
                var evt = _buffer[index];
                if (evt.Timestamp < cutoff) break;
                if (evt.Button == button && evt.Phase == CombatInputPhase.Canceled)
                    return evt;
            }
            return null;
        }

        /**
         * <summary>Returns how long the button has been held since last press. 0 if not held.</summary>
         */
        public float GetHoldDuration(CombatInputButton button)
        {
            if (_pressStartTimes.TryGetValue(button, out float startTime))
                return Time.unscaledTime - startTime;
            return 0f;
        }

        /** <summary>Returns <c>true</c> if the specified button is currently pressed.</summary> */
        public bool IsHeld(CombatInputButton button) => _pressStartTimes.ContainsKey(button);

        /** <summary>Removes all events older than <paramref name="maxAge"/> seconds.</summary> */
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

        /** <summary>Removes all events from the buffer.</summary> */
        public void Clear()
        {
            _count = 0;
            _head = 0;
            _pressStartTimes.Clear();
        }

        /**
         * <summary>Returns a snapshot of the last <paramref name="n"/> events, newest-first.</summary>
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
