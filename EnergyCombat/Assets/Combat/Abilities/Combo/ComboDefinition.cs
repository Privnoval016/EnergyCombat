using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * ScriptableObject asset describing a full combo sequence as a flat node list.
     * All <see cref="ComboNode"/> entries live here; <see cref="ComboTransition.TargetNodeIndex"/>
     * references them by position rather than by object reference, avoiding Unity's
     * inline serializer recursion limit.
     * </summary>
     *
     * <remarks>
     * Index 0 is always the root (first follow-up ability). A <c>TargetNodeIndex</c> of
     * <c>-1</c> on a transition means the combo ends after that step.
     *
     * Example — three-hit light combo:
     * <code>
     * Nodes[0]: Ability=LightSlash2, Transitions[0]: Button=Light → TargetNodeIndex=1
     * Nodes[1]: Ability=LightSlash3, Transitions[0]: Button=Heavy → TargetNodeIndex=2
     * Nodes[2]: Ability=HeavyFinisher, Transitions=[]
     * MaxConcurrentInterrupts = 2
     * </code>
     * </remarks>
     */
    [CreateAssetMenu(fileName = "New Combo Definition", menuName = "Combat/Combo Definition")]
    public class ComboDefinition : ScriptableObject
    {
        /**
         * <summary>
         * Flat list of all nodes in this combo. Index 0 is the root.
         * Add entries via the Inspector list; order matters — transitions reference by index.
         * </summary>
         */
        [Tooltip("All combo steps in order. Index 0 is the first follow-up after the starter. Add nodes here, then wire them together using TargetNodeIndex on each transition. Order is permanent once transitions are set — insert new nodes at the end.")]
        public List<ComboNode> Nodes;

        /**
         * <summary>
         * How many consecutive <see cref="ComboInterruptBehavior.PreserveCombo"/> ability
         * executions are tolerated before the combo resets automatically.
         * <c>0</c> means unlimited — the combo window timer is the only reset condition.
         * </summary>
         */
        [Tooltip("How many PreserveCombo abilities (dodges, off-hand hits) can fire before the combo chain auto-resets. 0 = unlimited — only the expiry timer resets the chain. Increase this to cap how many times the player can weave off-hand moves without the combo counting it against them.")]
        public int MaxConcurrentInterrupts = 0;

        /** <summary>Returns the root node, or <c>null</c> if the list is empty.</summary> */
        public ComboNode RootNode => Nodes is { Count: > 0 } ? Nodes[0] : null;

        /**
         * <summary>
         * Returns the node at <paramref name="index"/>, or <c>null</c> if the index
         * is out of range or is the sentinel value <c>-1</c>.
         * </summary>
         */
        public ComboNode GetNode(int index) =>
            index >= 0 && index < Nodes.Count ? Nodes[index] : null;
    }
}
