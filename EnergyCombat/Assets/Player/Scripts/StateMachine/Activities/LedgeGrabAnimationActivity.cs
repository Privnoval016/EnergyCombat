using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DynamicPhysics;
using StateMachine;

/// <summary>
/// Two-phase animation: hang loop while waiting for the physics to enter Climb phase,
/// then plays the climb animation to completion before yielding Active.
/// </summary>
public class LedgeGrabAnimationActivity : AnimationActivityBase
{
    private readonly PlayerAnimationConfig _cfg;
    private readonly LedgeGrabAbility _ability;

    public LedgeGrabAnimationActivity(
        PlayerAnimationController ctrl, PlayerAnimationConfig cfg, LedgeGrabAbility ability)
        : base(ctrl)
    {
        _cfg = cfg;
        _ability = ability;
    }

    public override async UniTask ActivateAsync(CancellationToken token)
    {
        if (Mode != ActivityMode.Inactive) return;
        Mode = ActivityMode.Activating;
        try
        {
            // Phase 1 — hang loop until physics transitions to Climb
            if (_cfg.LedgeGrab.HasLoop)
                _ = _cfg.LedgeGrab.Loop.Play(Ctrl.GetLayer(0), _cfg.LedgeGrab.FadeDuration);

            await UniTask.WaitUntil(
                () => _ability == null || _ability.IsInClimbPhase || !_ability.IsActive,
                cancellationToken: token);

            token.ThrowIfCancellationRequested();

            // Phase 2 — climb animation; await its end so the visual completes cleanly
            if (_cfg.LedgeClimb.HasLoop)
            {
                var state = _cfg.LedgeClimb.Loop.Play(Ctrl.GetLayer(0), 0.1f);
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
        await UniTask.CompletedTask;
        Mode = ActivityMode.Inactive;
    }
}
