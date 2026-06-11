using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * Strategy interface for ranking targeting candidates.
     * Implement this (or subclass <see cref="TargetingScorer"/>) to define
     * any targeting feel — angle-biased, distance-only, threat-priority, etc.
     * </summary>
     *
     * <remarks>
     * <see cref="SoftTargetingSystem"/> calls <see cref="Score"/> on every registered
     * candidate each evaluation tick. Returning a negative value signals that the
     * candidate is outside the valid targeting cone/range and should be skipped.
     * </remarks>
     */
    public interface ITargetingScorer
    {
        /**
         * <summary>
         * Returns a score for the given candidate. Higher is better.
         * Return a value less than zero to exclude the candidate entirely.
         * </summary>
         *
         * <param name="candidate">The target point being evaluated.</param>
         * <param name="playerPos">World position of the player.</param>
         * <param name="playerForward">Forward direction of the player.</param>
         * <param name="currentTarget">The currently locked target, used to apply stickiness bias.</param>
         */
        float Score(ITargetable candidate, Vector3 playerPos, Vector3 playerForward, ITargetable currentTarget);

        /**
         * <summary>
         * Quick range pre-check. The system may call this before <see cref="Score"/>
         * to early-out without the full scoring math.
         * </summary>
         */
        bool IsInRange(ITargetable candidate, Vector3 playerPos);
    }
}
