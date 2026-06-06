using System;
using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;
using StateMachine;

public class LoopAnimActivity : AnimationActivityBase
{
    protected readonly Func<StateAnimSet> GetSet;

    public LoopAnimActivity(PlayerAnimationController ctrl, StateAnimSet set)
        : this(ctrl, () => set) { }

    /// <summary>Dynamic overload: GetSet is evaluated at activation time (used for WallRun L/R).</summary>
    public LoopAnimActivity(PlayerAnimationController ctrl, Func<StateAnimSet> getSet)
        : base(ctrl)
    {
        GetSet = getSet;
    }

    public override async UniTask ActivateAsync(CancellationToken token)
    {
        if (Mode != ActivityMode.Inactive) return;
        Mode = ActivityMode.Activating;
        try
        {
            var set = GetSet();
            if (set.HasEnter) await PlayAndAwaitAsync(set.Enter, set.FadeDuration, token);
            token.ThrowIfCancellationRequested();
            if (set.HasLoop)
            {
                var state = set.Loop.Play(Ctrl.GetLayer(0), set.FadeDuration);
                Ctrl.SetActiveMixer(state as Vector2MixerState);
            }
            Mode = ActivityMode.Active;
        }
        catch (OperationCanceledException)
        {
            Mode = ActivityMode.Inactive;
        }
    }

    public override async UniTask DeactivateAsync(CancellationToken token)
    {
        if (Mode == ActivityMode.Inactive) return;
        Mode = ActivityMode.Deactivating;
        Ctrl.ClearActiveMixer();
        var set = GetSet();
        if (set.HasExit)
            _ = Ctrl.GetLayer(0).Play(set.Exit, set.FadeDuration); // non-blocking: Animancer crossfades
        await UniTask.CompletedTask;
        Mode = ActivityMode.Inactive;
    }
}
