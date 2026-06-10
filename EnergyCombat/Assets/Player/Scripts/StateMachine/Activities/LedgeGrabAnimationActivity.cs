using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DynamicPhysics;
using StateMachine;

/**
 * <summary>
 * Two-phase animation activity for ledge grabbing.
 * Phase 1 — plays the hang clip immediately on activation (non-blocking).
 * Phase 2 — once the physics ability enters its climb phase, plays the climb animation to
 * completion. <see cref="DeactivateAsync"/> blocks until the climb finishes so the visual is
 * never cut off, but cancels immediately if the activity is deactivated before the climb starts.
 * </summary>
 *
 * <remarks>
 * Both clips are configured as <see cref="OneShotAnimDef"/> in the Inspector. Use non-looping
 * import settings for the climb clip — it must fire its <c>OnEnd</c> event to unblock
 * <see cref="DeactivateAsync"/>.
 * </remarks>
 */
public class LedgeGrabAnimationActivity : AnimationActivityBase
{
    private readonly OneShotAnimDef _hang;
    private readonly OneShotAnimDef _climb;
    private readonly LedgeGrabAbility _ability;

    private UniTaskCompletionSource<bool> _climbTcs;

    /**
     * <summary>
     * Constructs the activity with explicit hang and climb animation definitions.
     * </summary>
     */
    public LedgeGrabAnimationActivity(
        PlayerAnimationController ctrl,
        OneShotAnimDef hang,
        OneShotAnimDef climb,
        LedgeGrabAbility ability)
        : base(ctrl)
    {
        _hang   = hang;
        _climb  = climb;
        _ability = ability;
    }

    /** <inheritdoc />
     * <remarks>
     * Plays the hang clip immediately. The hang→climb transition is driven by
     * <see cref="DriveClimbPhaseAsync"/> running in the background so the
     * <c>TransitionSequencer</c> is never blocked during activation.
     * </remarks>
     */
    public override UniTask ActivateAsync(CancellationToken token)
    {
        if (Mode != ActivityMode.Inactive) return UniTask.CompletedTask;
        Mode = ActivityMode.Activating;

        _climbTcs = null;

        if (_hang?.IsValid == true)
            _ = Ctrl.GetLayer(0).Play(_hang.Clip, _hang.FadeDuration);

        Mode = ActivityMode.Active;

        DriveClimbPhaseAsync().Forget();

        return UniTask.CompletedTask;
    }

    /**
     * <summary>
     * Background task that waits for the physics ability to enter its climb phase, then plays the
     * climb animation to completion. Exits early if the activity is deactivated before the climb starts.
     * </summary>
     */
    private async UniTaskVoid DriveClimbPhaseAsync()
    {
        await UniTask.WaitUntil(() => (_ability?.IsInClimbPhase == true) || Mode != ActivityMode.Active);

        if (Mode != ActivityMode.Active) return;
        if (_climb?.IsValid != true) return;

        _climbTcs = new UniTaskCompletionSource<bool>();

        var state = Ctrl.GetLayer(0).Play(_climb.Clip, _climb.FadeDuration);
        if (state != null)
            state.OwnedEvents.OnEnd = () => _climbTcs.TrySetResult(true);
        else
            _climbTcs.TrySetResult(true); // no clip played; resolve immediately so DeactivateAsync is not blocked

        try
        {
            await _climbTcs.Task;
        }
        finally
        {
            if (state != null) state.OwnedEvents.OnEnd = null;
        }
    }

    /** <inheritdoc />
     * <remarks>
     * If the climb has started, awaits its completion before setting <c>Mode = Inactive</c>
     * so the visual is never interrupted mid-climb. If the climb has not yet started,
     * setting <c>Mode</c> causes <see cref="DriveClimbPhaseAsync"/> to exit on its next poll.
     * Token cancellation propagates if the caller is itself cancelled.
     * </remarks>
     */
    public override async UniTask DeactivateAsync(CancellationToken token)
    {
        if (Mode == ActivityMode.Inactive) return;
        Mode = ActivityMode.Deactivating;

        if (_climbTcs != null)
        {
            try
            {
                using var reg = token.Register(() => _climbTcs.TrySetCanceled());
                await _climbTcs.Task;
            }
            catch (OperationCanceledException) { }
        }

        Mode = ActivityMode.Inactive;
    }
}
