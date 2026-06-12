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
         * Auto-expiry timeout in seconds. If an ability at this node finishes without the
         * next combo step firing, the chain resets after this many seconds.
         * <list type="bullet">
         *   <item><c>0</c> (default) — no expiry. The chain stays alive until the ability
         *     naturally completes or is interrupted.</item>
         *   <item><c>&gt; 0</c> — safety valve for stuns, knockbacks, or other cases where
         *     the ability never cleanly finishes.</item>
         * </list>
         * The actual input window (when the player <em>can</em> press) is controlled by
         * animation events or <see cref="RecoveryPhase.AllowComboCancel"/>, not by this value.
         * </summary>
         */
        [UnityEngine.Min(0f)]
        [UnityEngine.Tooltip("Auto-expiry timeout (seconds). 0 = no expiry. Controls combo reset on stun/interrupt, not the input window.")]
        public float ComboWindowDuration;
    }
}
