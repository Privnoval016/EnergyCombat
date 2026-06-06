namespace Combat
{
    /**
     * <summary>
     * An immutable snapshot of a single combat button interaction captured at a specific
     * point in time. Stored in <see cref="CombatInputBuffer"/> for combo resolution.
     * </summary>
     */
    public readonly struct CombatInputEvent
    {
        /** <summary>Which button was interacted with.</summary> */
        public readonly CombatInputButton Button;

        /** <summary>The phase of the button interaction.</summary> */
        public readonly CombatInputPhase Phase;

        /** <summary><c>Time.unscaledTime</c> when this event was recorded.</summary> */
        public readonly float Timestamp;

        /**
         * <summary>
         * Total seconds the button was held, measured from <c>Started</c> to <c>Canceled</c>.
         * Only meaningful on <see cref="CombatInputPhase.Canceled"/> events; 0 otherwise.
         * </summary>
         */
        public readonly float HoldDuration;

        /** <summary>Creates a new combat input event.</summary> */
        public CombatInputEvent(
            CombatInputButton button,
            CombatInputPhase phase,
            float timestamp,
            float holdDuration = 0f)
        {
            Button = button;
            Phase = phase;
            Timestamp = timestamp;
            HoldDuration = holdDuration;
        }
    }
}
