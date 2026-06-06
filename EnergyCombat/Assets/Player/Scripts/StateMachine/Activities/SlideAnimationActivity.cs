using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using StateMachine;

/// <summary>
/// Slide-specific animation activity: identical to LoopAnimActivity except DeactivateAsync
/// blocks until the Exit clip finishes before releasing the state machine transition.
/// </summary>
public class SlideAnimationActivity : LoopAnimActivity
{
    public SlideAnimationActivity(PlayerAnimationController ctrl, StateAnimSet set)
        : base(ctrl, set) { }

    public override async UniTask DeactivateAsync(CancellationToken token)
    {
        if (Mode == ActivityMode.Inactive) return;
        Mode = ActivityMode.Deactivating;
        Ctrl.ClearActiveMixer();
        var set = GetSet();
        try
        {
            if (set.HasExit)
                await PlayAndAwaitAsync(set.Exit, set.FadeDuration, token); // blocking
        }
        catch (OperationCanceledException) { }
        finally
        {
            Mode = ActivityMode.Inactive;
        }
    }
}
