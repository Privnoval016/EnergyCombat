using System;
using Extensions.Logic;

namespace Combat
{
    /**
     * <summary>
     * Condition that passes only when the <see cref="CombatContext"/> has a specific tag.
     * Use to gate abilities on runtime state: e.g. only allow a "charged" finisher if
     * the context has the <c>CombatTag.Charged</c> tag (set by a preceding phase or modifier).
     * </summary>
     */
    [Serializable]
    public class HasTagCondition : ICondition<CombatContext>
    {
        /**
         * <summary>The tag that must be present on the context for this condition to pass.</summary>
         */
        public Tag RequiredTag;

        /** <inheritdoc /> */
        public bool Evaluate(CombatContext context)
        {
            return context?.HasTag(RequiredTag) ?? false;
        }
    }
}
