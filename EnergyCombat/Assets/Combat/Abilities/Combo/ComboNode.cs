using System;

namespace Combat
{
    /**
     * <summary>
     * Represents a single step in a combo sequence. All nodes live in a flat list on
     * <see cref="ComboDefinition.Nodes"/>. Transitions reference siblings by index rather
     * than by object reference, which avoids Unity's inline-serializer recursion limit.
     * </summary>
     */
    [Serializable]
    public class ComboNode
    {
        /** <summary>The ability that executes when this node is reached.</summary> */
        public AbilityDefinition Ability;

        /**
         * <summary>
         * Edges leading to subsequent combo steps.
         * <see cref="AbilitySelector"/> evaluates these in order — the first matching
         * transition is taken.
         * </summary>
         */
        public ComboTransition[] Transitions;

        /**
         * <summary>
         * How many seconds after this node's combo-window opens the player has
         * to input the next step before the combo sequence resets.
         * 0 means the combo does not continue from this node.
         * </summary>
         */
        public float ComboWindowDuration;
    }
}
