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
        var state = Ctrl.GetLayer(0).Play(clip, fade);

        // If Animancer returned null (e.g. invalid clip or controller in bad state) there is no
        // AnimancerState to attach OnEnd to, so the TCS would never be resolved — hanging Phase 1
        // indefinitely. Return immediately so the exit step completes without blocking.
        if (state == null) return;

        var tcs = new UniTaskCompletionSource<bool>();
        state.OwnedEvents.OnEnd = () => tcs.TrySetResult(true);
        try
        {
            using var reg = token.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        }
        finally
        {
            state.OwnedEvents.OnEnd = null;
        }
    }
}
