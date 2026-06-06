using System.Threading;

namespace Combat
{
    /**
     * <summary>
     * Abstraction over the animation backend. Implement this interface to connect
     * the combat system to any animation framework (Animancer, legacy Animator, etc.).
     * </summary>
     *
     * <remarks>
     * The driver is the <em>only</em> place in the codebase that directly calls
     * animation APIs. Pipeline phases and the executor interact exclusively with
     * <see cref="AnimationHandle"/>, ensuring a clean separation between gameplay
     * timing and animation presentation.
     * </remarks>
     */
    public interface IAnimationDriver
    {
        /**
         * <summary>
         * Begins playing the animation described by <paramref name="request"/> and
         * returns a handle that allows pipeline phases to await events and completion.
         * </summary>
         * <returns>An <see cref="AnimationHandle"/>. Must never be null.</returns>
         */
        AnimationHandle Play(AnimationRequest request, CancellationToken token);

        /**
         * <summary>
         * Immediately stops any animation started by this driver. Called when an ability
         * is cancelled.
         * </summary>
         */
        void Stop();
    }
}
