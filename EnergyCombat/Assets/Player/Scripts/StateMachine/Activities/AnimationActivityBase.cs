using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;
using StateMachine;

/**
 * <summary>
 * Base class for all animation activities. Provides <see cref="PlayerAnimationController"/>
 * access and a shared <see cref="PlayAndAwaitAsync"/> helper.
 * </summary>
 */
public abstract class AnimationActivityBase : Activity
{
    protected readonly PlayerAnimationController Ctrl;

    protected AnimationActivityBase(PlayerAnimationController ctrl)
    {
        Ctrl = ctrl;
    }

    /**
     * <summary>
     * Plays a <see cref="ClipTransition"/> and awaits its <c>OnEnd</c> event on the live
     * <see cref="AnimancerState"/> (not the shared template). Cleans up the callback in all
     * cases — completion, cancellation, or exception.
     * </summary>
     * <remarks>
     * Setting <c>OnEnd</c> on the live state rather than the <see cref="ClipTransition"/> template
     * is required because Animancer may reuse an existing <c>AnimancerState</c> when the same clip
     * replays, in which case events are not re-copied from the template to the live state.
     * </remarks>
     */
    protected async UniTask PlayAndAwaitAsync(ClipTransition clip, float fade, CancellationToken token)
    {
        var tcs = new UniTaskCompletionSource<bool>();
        var state = Ctrl.GetLayer(0).Play(clip, fade);
        if (state != null)
            state.OwnedEvents.OnEnd = () => tcs.TrySetResult(true);
        try
        {
            using var reg = token.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        }
        finally
        {
            if (state != null) state.OwnedEvents.OnEnd = null;
        }
    }
}
