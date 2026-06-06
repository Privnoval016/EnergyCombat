using System;

namespace Combat
{
    /**
     * <summary>
     * A composable, data-driven query that tests an <see cref="ITagContainer"/> against
     * required, excluded, and optional tag sets.
     * </summary>
     *
     * <remarks>
     * A query passes when ALL of the following hold:
     * <list type="bullet">
     * <item><description>Every tag in <see cref="Required"/> is present.</description></item>
     * <item><description>No tag in <see cref="Excluded"/> is present.</description></item>
     * <item><description>At least one tag in <see cref="AnyOf"/> is present — or <see cref="AnyOf"/> is empty.</description></item>
     * </list>
     * </remarks>
     */
    [Serializable]
    public struct TagQuery
    {
        /** <summary>Tags that MUST all be present for the query to pass.</summary> */
        public Tag[] Required;

        /** <summary>Tags that MUST NOT be present for the query to pass.</summary> */
        public Tag[] Excluded;

        /** <summary>At least one of these tags must be present (ignored if empty).</summary> */
        public Tag[] AnyOf;

        /**
         * <summary>
         * Evaluates whether the given container satisfies all constraints in this query.
         * </summary>
         * <param name="container">The tag container to test. Returns <c>false</c> if null.</param>
         */
        public bool Matches(ITagContainer container)
        {
            if (container == null) return false;

            if (Required != null)
            {
                foreach (var tag in Required)
                    if (!container.HasTag(tag)) return false;
            }

            if (Excluded != null)
            {
                foreach (var tag in Excluded)
                    if (container.HasTag(tag)) return false;
            }

            if (AnyOf != null && AnyOf.Length > 0)
            {
                bool anyFound = false;
                foreach (var tag in AnyOf)
                {
                    if (container.HasTag(tag)) { anyFound = true; break; }
                }
                if (!anyFound) return false;
            }

            return true;
        }

        /** <summary>Creates a query that requires a single tag.</summary> */
        public static TagQuery Require(Tag tag) => new TagQuery { Required = new[] { tag } };

        /** <summary>Creates a query that excludes a single tag.</summary> */
        public static TagQuery Exclude(Tag tag) => new TagQuery { Excluded = new[] { tag } };

        /** <summary>A query with no constraints — always passes.</summary> */
        public static TagQuery Any => new TagQuery();
    }
}
