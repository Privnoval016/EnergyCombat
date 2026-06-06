using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;
using StateMachine;

public abstract class AnimationActivityBase : Activity
{
    protected readonly PlayerAnimationController Ctrl;

    protected AnimationActivityBase(PlayerAnimationController ctrl)
    {
        Ctrl = ctrl;
    }

    /// <summary>
    /// Plays a ClipTransition, awaits its OnEnd event, and always cleans up the callback.
    /// Cancellation via token causes OperationCanceledException to propagate to the caller.
    /// </summary>
    protected async UniTask PlayAndAwaitAsync(ClipTransition clip, float fade, CancellationToken token)
    {
        var tcs = new UniTaskCompletionSource<bool>();
        clip.Events.OnEnd = () => tcs.TrySetResult(true);
        _ = Ctrl.GetLayer(0).Play(clip, fade);
        try
        {
            using var reg = token.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        }
        finally
        {
            clip.Events.OnEnd = null;
        }
    }
}
