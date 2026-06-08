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
 *
 * <b>Enter-phase callbacks:</b> optional <c>onEnterStart</c> and <c>onEnterEnd</c> actions fire
 * at the beginning and end of the enter clip. Use these to set/clear motion tags (e.g.
 * <c>MotionTag.WalkStarting</c>) that suppress physics acceleration while the enter animation plays.
 * Both callbacks are no-ops when not provided, and <c>onEnterEnd</c> is idempotent-safe to call
 * multiple times (e.g. once via <c>OnEnd</c> and once via <see cref="DeactivateAsync"/> on interrupt).
 *
 * <b>Rapid re-entry guard:</b> optional <c>skipEnter</c> predicate. When it returns <c>true</c>
 * at activation time, the enter clip is bypassed and the loop starts immediately. Neither
 * <c>onEnterStart</c> nor <c>onEnterEnd</c> fires when the enter is skipped. Intended for
 * suppressing the walk-start animation when the player re-enters the walk state shortly after
 * leaving it (e.g. rapid idle↔walk toggling).
 * </remarks>
 */
public class LoopAnimActivity : AnimationActivityBase
{
    protected readonly Func<LoopAnimDef> GetDef;

    private AnimancerState _enterState;

    private readonly Action _onEnterStart;
    private readonly Action _onEnterEnd;
    private readonly Func<bool> _skipEnter;

    /** <summary>Static overload: always uses the same def.</summary> */
    public LoopAnimActivity(PlayerAnimationController ctrl, LoopAnimDef def,
        Action onEnterStart = null, Action onEnterEnd = null, Func<bool> skipEnter = null)
        : this(ctrl, () => def, onEnterStart, onEnterEnd, skipEnter) { }

    /** <summary>Dynamic overload: def is evaluated at activation time (e.g. WallRun L/R selection).</summary> */
    public LoopAnimActivity(PlayerAnimationController ctrl, Func<LoopAnimDef> getDef,
        Action onEnterStart = null, Action onEnterEnd = null, Func<bool> skipEnter = null)
        : base(ctrl)
    {
        GetDef        = getDef;
        _onEnterStart = onEnterStart;
        _onEnterEnd   = onEnterEnd;
        _skipEnter    = skipEnter;
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
            if (_skipEnter?.Invoke() == true)
            {
                // Rapid re-entry: bypass enter clip and start the loop immediately
                var loopState = def.Loop.Play(layer, def.FadeDuration);
                Ctrl.SetActiveMixer(loopState as Vector2MixerState);
            }
            else
            {
                _onEnterStart?.Invoke();
                _enterState = layer.Play(def.Enter, def.FadeDuration);
                if (_enterState != null)
                {
                    _enterState.OwnedEvents.OnEnd = () =>
                    {
                        _enterState.OwnedEvents.OnEnd = null;
                        _onEnterEnd?.Invoke();
                        if (Mode == ActivityMode.Active)
                        {
                            var loopState = def.Loop.Play(layer, def.FadeDuration);
                            Ctrl.SetActiveMixer(loopState as Vector2MixerState);
                        }
                    };
                }
                else
                {
                    _onEnterEnd?.Invoke(); // Play() returned null; clear tag immediately
                }
            }
        }
        else if (def.HasEnter)
        {
            if (_skipEnter?.Invoke() != true)
            {
                _onEnterStart?.Invoke();
                _enterState = layer.Play(def.Enter, def.FadeDuration);
                if (_enterState != null)
                {
                    _enterState.OwnedEvents.OnEnd = () =>
                    {
                        _enterState.OwnedEvents.OnEnd = null;
                        _onEnterEnd?.Invoke();
                    };
                }
                else
                {
                    _onEnterEnd?.Invoke();
                }
            }
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
            _onEnterEnd?.Invoke(); // clears motion tag if enter clip was cut short; idempotent
        }

        var def = GetDef();
        Ctrl.ClearActiveMixer();

        if (!token.IsCancellationRequested && def?.HasExit == true)
        {
            try { await PlayAndAwaitAsync(def.Exit, def.FadeDuration, token); }
            catch (OperationCanceledException) { }
        }

        Mode = ActivityMode.Inactive;
    }
}
