using System;

namespace Combat
{
    /**
     * <summary>
     * Defines the conditions under which a <see cref="ComboNode"/> transitions to
     * a subsequent node. Each transition maps an input gesture to a target node.
     * </summary>
     *
     * <remarks>
     * Pause timing allows for expressive combos like:
     * <c>Light → Light → [pause 0.3–0.8s] → Light(hold) → Heavy</c>.
     * A <see cref="MaxPauseDuration"/> of <c>0</c> means there is no upper bound.
     * </remarks>
     */
    [Serializable]
    public class ComboTransition
    {
        /** <summary>The button that must be pressed to follow this transition.</summary> */
        public CombatInputButton Button;

        /**
         * <summary>
         * If <c>true</c>, the button must be held beyond the ability's hold threshold.
         * If <c>false</c>, a tap suffices.
         * </summary>
         */
        public bool RequireHold;

        /**
         * <summary>
         * Minimum pause (seconds) between the previous input and this one.
         * 0 means no minimum pause is required.
         * </summary>
         */
        public float MinPauseDuration;

        /**
         * <summary>
         * Maximum pause (seconds) allowed before the combo window closes.
         * 0 means this transition does not impose an upper bound.
         * </summary>
         */
        public float MaxPauseDuration;

        /**
         * <summary>
         * Index into <see cref="ComboDefinition.Nodes"/> for the next step.
         * <c>-1</c> means the combo ends after this transition is taken.
         * </summary>
         */
        public int TargetNodeIndex = -1;
    }
}
