using System;
using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;
using StateMachine;

/**
 * <summary>
 * Animation activity for states with a looping locomotion cycle — optional enter/exit clips flanking
 * a persistent loop. Backed by <see cref="LoopAnimDef"/>.
 * </summary>
 *
 * <remarks>
 * <b>Enter → Loop handoff:</b> the <c>OnEnd</c> callback is registered on the live
 * <see cref="AnimancerState"/> returned by <c>layer.Play()</c>, not on the shared
 * <see cref="ClipTransition"/> template. This is necessary because Animancer reuses the same
 * <c>AnimancerState</c> instance when the same clip replays, and does not re-copy events from the
 * template onto the reused live state.
 *
 * <b>Exit blocking:</b> <see cref="DeactivateAsync"/> awaits the Exit clip to completion so
 * deceleration/stop animations play in full before the next state begins. The
 * <see cref="TransitionSequencer{T}"/> runs <c>InternalTick</c> during Phase 1, so a new input
 * (jump, dodge) will interrupt the exit mid-clip and cancel this task via the token.
 * </remarks>
 */
public class LoopAnimActivity : AnimationActivityBase
{
    protected readonly Func<LoopAnimDef> GetDef;

    private AnimancerState _enterState;

    /** <summary>Static overload: always uses the same def.</summary> */
    public LoopAnimActivity(PlayerAnimationController ctrl, LoopAnimDef def)
        : this(ctrl, () => def) { }

    /** <summary>Dynamic overload: def is evaluated at activation time (e.g. WallRun L/R selection).</summary> */
    public LoopAnimActivity(PlayerAnimationController ctrl, Func<LoopAnimDef> getDef)
        : base(ctrl)
    {
        GetDef = getDef;
    }

    /** <inheritdoc />
     * <remarks>
     * Non-blocking. If the def has both Enter and Loop, an <c>OnEnd</c> callback is registered on
     * the live <see cref="AnimancerState"/> (not the template) so Animancer state reuse does not
     * silently drop the callback. The callback starts the Loop clip when Enter finishes.
     * </remarks>
     */
    public override async UniTask ActivateAsync(CancellationToken token)
    {
        if (Mode != ActivityMode.Inactive) return;
        Mode = ActivityMode.Activating;

        var def = GetDef();
        if (def == null) { Mode = ActivityMode.Active; return; }

        var layer = Ctrl.GetLayer(0);

        if (def.HasEnter && def.HasLoop)
        {
            _enterState = layer.Play(def.Enter, def.FadeDuration);
            if (_enterState != null)
            {
                _enterState.OwnedEvents.OnEnd = () =>
                {
                    _enterState.OwnedEvents.OnEnd = null;
                    if (Mode == ActivityMode.Active)
                    {
                        var loopState = def.Loop.Play(layer, def.FadeDuration);
                        Ctrl.SetActiveMixer(loopState as Vector2MixerState);
                    }
                };
            }
        }
        else if (def.HasEnter)
        {
            _enterState = layer.Play(def.Enter, def.FadeDuration);
        }
        else if (def.HasLoop)
        {
            var loopState = def.Loop.Play(layer, def.FadeDuration);
            Ctrl.SetActiveMixer(loopState as Vector2MixerState);
        }

        Mode = ActivityMode.Active;
        await UniTask.CompletedTask;
    }

    /** <inheritdoc />
     * <remarks>
     * Clears any pending Enter→Loop callback on the live state, then blocks on the Exit clip if
     * configured. Catching <see cref="OperationCanceledException"/> allows the Phase 1 interrupt
     * mechanism in <see cref="TransitionSequencer{T}"/> to cancel this task immediately when a
     * higher-priority input arrives.
     * </remarks>
     */
    public override async UniTask DeactivateAsync(CancellationToken token)
    {
        if (Mode == ActivityMode.Inactive) return;
        Mode = ActivityMode.Deactivating;

        if (_enterState != null)
        {
            _enterState.OwnedEvents.OnEnd = null;
            _enterState = null;
        }

        var def = GetDef();
        Ctrl.ClearActiveMixer();

        try
        {
            if (def?.HasExit == true)
                await PlayAndAwaitAsync(def.Exit, def.FadeDuration, token);
        }
        catch (OperationCanceledException) { }

        Mode = ActivityMode.Inactive;
    }
}
