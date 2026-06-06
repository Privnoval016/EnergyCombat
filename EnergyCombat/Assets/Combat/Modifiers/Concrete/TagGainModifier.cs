using System;

namespace Combat
{
    /**
     * <summary>
     * A dynamic modifier that adds one or more <see cref="Tag"/> values to the
     * <see cref="CombatContext"/> when a specified combat event type fires.
     * </summary>
     *
     * <remarks>
     * Useful for dynamically classifying attacks based on runtime conditions.
     * For example, adding <c>CombatTag.Crit</c> to the context when a
     * <see cref="HitEvent"/> fires and a crit roll succeeds.
     * </remarks>
     */
    public class TagGainModifier<TEvent> : IDynamicModifier
        where TEvent : ICombatEvent
    {
        private readonly Tag[] _tagsToAdd;

        /** <summary>Creates a tag-gain modifier.</summary> */
        public TagGainModifier(Tag[] tagsToAdd)
        {
            _tagsToAdd = tagsToAdd ?? Array.Empty<Tag>();
        }

        /** <inheritdoc /> */
        public Tag[] Tags => Array.Empty<Tag>();

        /** <inheritdoc /> */
        public bool IsExpired => false;

        /** <inheritdoc /> */
        public string DebugLabel => $"TagGain<{typeof(TEvent).Name}>";

        /** <inheritdoc /> */
        public void OnCombatEvent(ICombatEvent evt, CombatContext context)
        {
            if (context == null || evt is not TEvent) return;
            foreach (var tag in _tagsToAdd)
                context.SetTag(tag);
        }
    }
}
