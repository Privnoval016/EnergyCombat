namespace Combat
{
    /**
     * <summary>
     * Describes any object that can hold and query a set of <see cref="Tag"/> identifiers.
     * </summary>
     *
     * <remarks>
     * Both <see cref="DynamicPhysics.MotionContext"/> and <c>CombatContext</c> implement this
     * interface, unifying how tags are managed across the motion and combat systems.
     * Tag lookups are expected to be O(1) — implementors should back this with a <c>HashSet</c>.
     * </remarks>
     */
    public interface ITagContainer
    {
        /** <summary>Checks whether the specified tag is currently active.</summary> */
        bool HasTag(Tag tag);

        /** <summary>Adds (sets) a tag.</summary> */
        void SetTag(Tag tag);

        /** <summary>Removes a tag if it is present.</summary> */
        void RemoveTag(Tag tag);

        /** <summary>Removes all tags.</summary> */
        void ClearTags();
    }
}
