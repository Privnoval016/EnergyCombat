using System.Threading;

namespace Combat
{
    /**
     * <summary>
     * A no-operation <see cref="IAnimationDriver"/> that immediately completes every
     * animation request without touching the Animator or any animation system.
     * </summary>
     *
     * <remarks>
     * Use this driver when prototyping ability data before animation assets are ready,
     * or when running headless tests. Because <see cref="AnimationHandle.NotifyComplete"/>
     * is called immediately, all pipeline phases that await completion will proceed without delay.
     * Phases awaiting named events will block forever unless triggered manually.
     * </remarks>
     */
    public class NullAnimationDriver : IAnimationDriver
    {
        /** <inheritdoc /> */
        public AnimationHandle Play(AnimationRequest request, CancellationToken token)
        {
            var handle = new AnimationHandle();
            handle.NotifyComplete();
            return handle;
        }

        /** <inheritdoc /> */
        public void Stop() { }
    }
}
