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
        [UnityEngine.Tooltip("Which button the player must press to take this branch. To create branching combos, add multiple transitions with different buttons on the same node.")]
        public CombatInputButton Button;

        /**
         * <summary>
         * If <c>true</c>, the button must be held beyond the ability's hold threshold.
         * If <c>false</c>, a tap suffices.
         * </summary>
         */
        [UnityEngine.Tooltip("Require the player to hold the button rather than tap it. Pairs with the target ability's Hold Threshold setting.")]
        public bool RequireHold;

        /**
         * <summary>
         * Minimum pause (seconds) between the previous input and this one.
         * 0 means no minimum pause is required.
         * </summary>
         */
        [UnityEngine.Tooltip("Minimum seconds the player must wait after the previous input before this transition is valid. 0 = no minimum. Use to force deliberate timing (e.g. a rhythm-based finisher).")]
        [UnityEngine.Min(0f)]
        public float MinPauseDuration;

        /**
         * <summary>
         * Maximum pause (seconds) allowed before the combo window closes.
         * 0 means this transition does not impose an upper bound.
         * </summary>
         */
        [UnityEngine.Tooltip("Maximum seconds after the previous input before this transition expires. 0 = no upper bound (window stays open until the node's Combo Window Duration expires). Use to enforce strict timing on a specific branch.")]
        [UnityEngine.Min(0f)]
        public float MaxPauseDuration;

        /**
         * <summary>
         * Index into <see cref="ComboDefinition.Nodes"/> for the next step.
         * <c>-1</c> means the combo ends after this transition is taken.
         * </summary>
         */
        [UnityEngine.Tooltip("Index of the target node in the parent ComboDefinition.Nodes list. -1 = terminal (combo ends here, next press fires a starter from the loadout).")]
        public int TargetNodeIndex = -1;
    }
}
