using System.Collections.Generic;
using Extensions.Timers;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Resolves a <see cref="CombatInputBuffer"/> into an <see cref="AbilityDefinition"/> by
     * walking the active combo sequence and falling back to all active loadouts in order.
     * </summary>
     *
     * <remarks>
     * Resolution rules (in order):
     * <list type="number">
     * <item>If a combo node is active and its window is open, evaluate its transitions first.</item>
     * <item>If a transition matches, follow it and return the target node's ability (consuming the input).</item>
     * <item>Terminal transitions reset the combo without consuming the input so it falls through to loadouts.</item>
     * <item>Scan all active loadouts in registration order — first match wins (consuming the input).</item>
     * <item>Return null if nothing matches.</item>
     * </list>
     *
     * Combo state is managed through three notifications from <c>AbilityExecutor</c>:
     * <list type="bullet">
     * <item><see cref="OnAbilityCompleted"/> — advances the combo pointer and opens the next window.</item>
     * <item><see cref="OnAbilityInterrupted"/> — resets the combo immediately.</item>
     * <item><see cref="OnAbilityComboPreserved"/> — increments the interrupt counter; resets combo
     *   only if <see cref="ComboDefinition.MaxConcurrentInterrupts"/> is exceeded.</item>
     * </list>
     * </remarks>
     */
    public class AbilitySelector
    {
        // ── Result type ───────────────────────────────────────────────────────

        /**
         * <summary>
         * Returned by <see cref="Resolve"/>. Pairs the resolved ability with a flag indicating
         * whether it was reached via a combo transition. Used by <c>CombatController</c> to
         * decide whether to preserve the active combo chain when the new ability interrupts.
         * </summary>
         */
        public readonly struct ResolveResult
        {
            /** <summary>The ability that was matched.</summary> */
            public readonly AbilityDefinition Ability;

            /**
             * <summary>
             * <c>true</c> when this ability was resolved via a non-terminal combo transition.
             * <c>false</c> when resolved from a fresh loadout lookup (starter ability).
             * </summary>
             */
            public readonly bool WasComboTransition;

            public ResolveResult(AbilityDefinition ability, bool wasComboTransition)
            {
                Ability           = ability;
                WasComboTransition = wasComboTransition;
            }
        }

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<AbilityLoadout> _loadouts  = new List<AbilityLoadout>();
        private ComboDefinition _currentDefinition;
        private int _currentNodeIndex = -1;
        private int _interruptCount;
        private float _lastInputTime;
        private readonly CountdownTimer _comboWindowTimer;
        private readonly CombatInputSettings _settings;

        /**
         * <summary>
         * Set by <see cref="PreOpenFollowUpCombo"/> during an ability's recovery window before
         * the ability has formally completed. Makes <see cref="IsInCombo"/> return <c>true</c>
         * without needing the countdown timer to be running, so <see cref="TryResolveCombo"/>
         * can fire during recovery-phase cancels.
         * Cleared by <see cref="OnAbilityCompleted"/> and <see cref="ResetCombo"/>.
         * </summary>
         */
        private bool _preOpened;

        /**
         * <summary>
         * Set by <see cref="NotifyComboTransitionPending"/> immediately before
         * <c>CombatController</c> cancels the current execution for a deliberate combo
         * chain advance. When the cancelled execution's <c>finally</c> subsequently
         * calls <see cref="OnAbilityInterrupted"/>, this flag suppresses <see cref="ResetCombo"/>
         * so the combo state that <see cref="TryResolveCombo"/> just established is preserved.
         * Cleared by every selector notification and by <see cref="ResetCombo"/>.
         * </summary>
         */
        private bool _comboTransitionPending;

        /**
         * <summary>
         * The <see cref="ComboNode.ComboWindowDuration"/> of the node that was last resolved
         * via <see cref="TryResolveCombo"/>, deferred until the executing ability completes.
         * <para>
         * Storing the duration here — and calling <see cref="OpenComboWindow"/> only in the
         * completion notification — ensures the expiry window starts <em>after</em> the ability
         * finishes, not at the moment the input was pressed. This prevents the timer from
         * counting down during a long animation and expiring before recovery opens.
         * </para>
         * <c>-1</c> when no deferred window is pending (sentinel).
         * Cleared by <see cref="ResetCombo"/> and the completion notifications.
         * </summary>
         */
        private float _pendingComboWindowDuration = -1f;

        // ── Construction ──────────────────────────────────────────────────────

        /**
         * <summary>Creates a selector with an optional initial loadout and input settings.</summary>
         *
         * <param name="settings">
         * Input settings asset. When null, grace windows fall back to buffer defaults.
         * </param>
         * <param name="initialLoadout">Optional first loadout.</param>
         */
        public AbilitySelector(CombatInputSettings settings = null, AbilityLoadout initialLoadout = null)
        {
            _settings         = settings;
            _comboWindowTimer = new CountdownTimer(0f);
            if (initialLoadout != null)
                _loadouts.Add(initialLoadout);
        }

        // ── Loadout management ────────────────────────────────────────────────

        /** <summary>Adds a loadout. Abilities in all active loadouts are eligible for resolution.</summary> */
        public void AddLoadout(AbilityLoadout loadout)
        {
            if (loadout != null && !_loadouts.Contains(loadout))
                _loadouts.Add(loadout);
        }

        /** <summary>Removes a loadout. Does not reset the combo.</summary> */
        public void RemoveLoadout(AbilityLoadout loadout)
        {
            _loadouts.Remove(loadout);
        }

        /** <summary>Replaces all loadouts at once. Resets the combo state.</summary> */
        public void SetLoadouts(IReadOnlyList<AbilityLoadout> loadouts)
        {
            _loadouts.Clear();
            foreach (var l in loadouts)
                if (l != null) _loadouts.Add(l);
            ResetCombo();
        }

        /**
         * <summary>
         * Replaces all loadouts with a single one. Convenience overload for single-weapon characters.
         * Resets the combo state.
         * </summary>
         */
        public void SetLoadout(AbilityLoadout loadout)
        {
            _loadouts.Clear();
            if (loadout != null) _loadouts.Add(loadout);
            ResetCombo();
        }

        // ── Combo state ───────────────────────────────────────────────────────

        /**
         * <summary>
         * True while a combo sequence is being tracked and has not expired.
         * The combo stays alive until one of:
         * <list type="bullet">
         *   <item><see cref="OnAbilityCompleted"/> with no follow-up — explicit reset.</item>
         *   <item><see cref="OnAbilityInterrupted"/> — explicit reset.</item>
         *   <item><see cref="ComboNode.ComboWindowDuration"/> expires — auto-reset safety valve
         *     for stuns, falls, or other cases where the ability never cleanly completes.
         *     A duration of <c>0</c> means no expiry.</item>
         * </list>
         * The combo does NOT reset when the countdown timer has not been started (pre-open state)
         * or when <c>ComboWindowDuration = 0</c> (no timer).
         * </summary>
         */
        public bool IsInCombo =>
            _currentDefinition != null &&
            _currentNodeIndex >= 0 &&
            (!_comboWindowTimer.IsRunning || !_comboWindowTimer.IsFinished);

        // ── Debug read-only properties ────────────────────────────────────────

        /** <summary>The active combo definition, or null if no combo is in progress.</summary> */
        public ComboDefinition CurrentComboDefinition => _currentDefinition;

        /** <summary>Current node index within the active combo, or -1.</summary> */
        public int CurrentComboNodeIndex => _currentNodeIndex;

        /** <summary>True while the pre-open flag is set (recovery-phase cancel window).</summary> */
        public bool IsPreOpened => _preOpened;

        /** <summary>True while the countdown combo-window timer is running and not expired.</summary> */
        public bool IsComboWindowActive => _comboWindowTimer.IsRunning && !_comboWindowTimer.IsFinished;

        // ── Resolution ────────────────────────────────────────────────────────

        /**
         * <summary>
         * Attempts to resolve an ability from the buffer given the current context.
         * Returns null when no ability matches.
         * </summary>
         */
        public ResolveResult? Resolve(CombatInputBuffer buffer, CombatContext context)
        {
            if (IsInCombo)
            {
                var comboResult = TryResolveCombo(buffer, context);
                if (comboResult.HasValue) return comboResult;
            }

            return TryResolveFromLoadouts(buffer, context);
        }

        // ── Executor notifications ────────────────────────────────────────────

        /**
         * <summary>
         * Pre-opens the follow-up combo tree for a starter ability during its recovery window,
         * before the ability formally completes. Sets <see cref="IsInCombo"/> to <c>true</c>
         * without starting the countdown timer so that <c>TryResolveCombo</c> can fire during
         * recovery-phase cancels. The timer starts in <see cref="OnAbilityCompleted"/>.
         * Does nothing if a combo is already active (avoids overriding a running chain).
         * </summary>
         * <param name="executed">The currently executing starter ability.</param>
         */
        public void PreOpenFollowUpCombo(AbilityDefinition executed)
        {
            if (IsInCombo) return;

            var followUp = executed?.FollowUpCombo;
            if (followUp?.RootNode == null) return;

            _currentDefinition = followUp;
            _currentNodeIndex  = 0;
            _preOpened         = true;
        }

        /**
         * <summary>
         * Called when an ability completes normally.
         * <list type="bullet">
         *   <item>If the completed ability has an explicit <see cref="AbilityDefinition.FollowUpCombo"/>,
         *     opens (or refreshes) the first node's countdown window.</item>
         *   <item>If we are mid-combo and the timer is still running (opened by a previous
         *     <see cref="TryResolveCombo"/> step), the existing window is preserved.</item>
         *   <item>Otherwise resets the combo.</item>
         * </list>
         * </summary>
         */
        public void OnAbilityCompleted(AbilityDefinition executed)
        {
            _comboTransitionPending = false;
            _interruptCount = 0;
            _preOpened      = false;

            if (executed?.FollowUpCombo?.RootNode != null)
            {
                _currentDefinition = executed.FollowUpCombo;
                _currentNodeIndex  = 0;
                OpenComboWindow(_currentDefinition.Nodes[0].ComboWindowDuration);
            }
            else if (_pendingComboWindowDuration >= 0f)
            {
                // Mid-combo node that completed on the preserveCombo=false path (pressed while
                // not executing). TryResolveCombo deferred the window; open it now so the
                // player has time to chain the next hit after the animation finishes.
                OpenComboWindow(_pendingComboWindowDuration);
                _pendingComboWindowDuration = -1f;
            }
            else if (_currentDefinition != null &&
                     _comboWindowTimer.IsRunning &&
                     !_comboWindowTimer.IsFinished)
            {
                // Window was already opened by an earlier OpenComboWindow call and hasn't
                // expired yet — preserve it (e.g. starter follow-up window still ticking).
            }
            else
            {
                ResetCombo();
            }
        }

        /**
         * <summary>
         * Signals that the currently executing ability is about to be cancelled deliberately
         * to advance a combo chain. Must be called before
         * <c>CombatController.CancelCurrentAbility()</c> so that the cancelled ability's
         * <see cref="OnAbilityInterrupted"/> does not undo the combo state that
         * <c>TryResolveCombo</c> just set up.
         * </summary>
         */
        public void NotifyComboTransitionPending() => _comboTransitionPending = true;

        /**
         * <summary>
         * Called when an execution is interrupted with <see cref="ComboInterruptBehavior.BreakCombo"/>.
         * Resets the combo immediately, unless <see cref="NotifyComboTransitionPending"/> was
         * called first — in that case the interruption is a deliberate combo advance and the
         * chain state is preserved.
         * </summary>
         */
        public void OnAbilityInterrupted()
        {
            bool wasTransition = _comboTransitionPending;
            _comboTransitionPending = false;
            if (!wasTransition)
                ResetCombo();
        }

        /**
         * <summary>
         * Called when an execution finishes with <see cref="ComboInterruptBehavior.PreserveCombo"/>.
         * Increments the interrupt counter and resets the combo only if
         * <see cref="ComboDefinition.MaxConcurrentInterrupts"/> is exceeded.
         * </summary>
         */
        public void OnAbilityComboPreserved()
        {
            // Read before clearing: true means this execution was cancelled to chain the next hit.
            bool cancelledForTransition = _comboTransitionPending;
            _comboTransitionPending = false;
            _interruptCount++;

            // Only open the post-execution window on natural completion.
            // If the ability was cancelled to advance the chain, the next TryResolveCombo
            // already stored a new _pendingComboWindowDuration for the incoming ability.
            if (!cancelledForTransition && _pendingComboWindowDuration >= 0f)
            {
                OpenComboWindow(_pendingComboWindowDuration);
                _pendingComboWindowDuration = -1f;
            }

            if (_currentDefinition != null &&
                _currentDefinition.MaxConcurrentInterrupts > 0 &&
                _interruptCount >= _currentDefinition.MaxConcurrentInterrupts)
            {
                ResetCombo();
            }
        }

        /** <summary>Resets the combo pointer, pre-open flag, counter, pending window, and stops the timer.</summary> */
        public void ResetCombo()
        {
            _currentDefinition           = null;
            _currentNodeIndex            = -1;
            _interruptCount              = 0;
            _preOpened                   = false;
            _comboTransitionPending      = false;
            _pendingComboWindowDuration  = -1f;
            _comboWindowTimer.Stop();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private ResolveResult? TryResolveCombo(CombatInputBuffer buffer, CombatContext context)
        {
            var node = _currentDefinition.GetNode(_currentNodeIndex);
            if (node?.Transitions == null) return null;

            float now = Time.unscaledTime;

            foreach (var transition in node.Transitions)
            {
                if (!IsTransitionInputSatisfied(transition, buffer)) continue;

                float pauseSince = now - _lastInputTime;
                if (transition.MinPauseDuration > 0f && pauseSince < transition.MinPauseDuration) continue;
                if (transition.MaxPauseDuration > 0f && pauseSince > transition.MaxPauseDuration) continue;

                var targetNode = _currentDefinition.GetNode(transition.TargetNodeIndex);
                if (targetNode?.Ability != null &&
                    !targetNode.Ability.AreConditionsMet(context ?? new CombatContext())) continue;

                _interruptCount   = 0;
                _currentNodeIndex = transition.TargetNodeIndex;
                _lastInputTime    = now;

                if (_currentNodeIndex >= 0 && targetNode != null)
                {
                    // Defer the window timer: store the duration and stop any running timer so
                    // IsInCombo stays true during execution. OpenComboWindow is called in
                    // OnAbilityComboPreserved / OnAbilityCompleted once the ability finishes.
                    _pendingComboWindowDuration = targetNode.ComboWindowDuration;
                    _comboWindowTimer.Stop();
                    buffer.ConsumeInput(transition.Button);
                    return new ResolveResult(targetNode.Ability, wasComboTransition: true);
                }
                else
                {
                    // Terminal transition (TargetNodeIndex = -1): reset the chain and let the
                    // input fall through to TryResolveFromLoadouts to restart from a starter.
                    ResetCombo();
                    return null;
                }
            }

            return null;
        }

        private ResolveResult? TryResolveFromLoadouts(CombatInputBuffer buffer, CombatContext context)
        {
            if (_loadouts.Count == 0) return null;
            float now = Time.unscaledTime;

            foreach (var loadout in _loadouts)
            {
                if (loadout?.ActiveAbilities == null) continue;
                foreach (var ability in loadout.ActiveAbilities)
                {
                    if (ability == null) continue;
                    if (!IsAbilityInputSatisfied(ability, buffer)) continue;
                    if (!ability.AreConditionsMet(context ?? new CombatContext())) continue;

                    _lastInputTime = now;

                    // Consume the appropriate input event so this match doesn't re-fire.
                    if (ability.RequireHold && !buffer.IsHeld(ability.PrimaryInput))
                        buffer.ConsumeRelease(ability.PrimaryInput);
                    else
                        buffer.ConsumeInput(ability.PrimaryInput);

                    return new ResolveResult(ability, wasComboTransition: false);
                }
            }

            return null;
        }

        private void OpenComboWindow(float duration)
        {
            if (duration > 0f)
            {
                _comboWindowTimer.Reset(duration);
                _comboWindowTimer.Start();
            }
            else
            {
                _comboWindowTimer.Stop();
            }
        }

        private bool IsTransitionInputSatisfied(ComboTransition transition, CombatInputBuffer buffer)
        {
            if (transition == null) return false;

            if (transition.RequireHold)
                return buffer.GetHoldDuration(transition.Button) > 0f ||
                       IsRecentHoldRelease(transition.Button, buffer);

            // Use ComboTransitionGraceWindow override when configured.
            float comboGrace = _settings?.ComboTransitionGraceWindow ?? 0f;
            return comboGrace > 0f
                ? buffer.HasRecentInput(transition.Button, comboGrace)
                : buffer.HasRecentInput(transition.Button);
        }

        private static bool IsAbilityInputSatisfied(AbilityDefinition ability, CombatInputBuffer buffer)
        {
            if (ability.RequireHold)
            {
                float held = buffer.GetHoldDuration(ability.PrimaryInput);
                if (held >= ability.HoldThreshold) return true;
                return IsRecentHoldRelease(ability.PrimaryInput, buffer, ability.HoldThreshold);
            }

            return buffer.HasRecentInput(ability.PrimaryInput);
        }

        private static bool IsRecentHoldRelease(
            CombatInputButton button,
            CombatInputBuffer buffer,
            float minHold = 0f)
        {
            var release = buffer.GetLatestRelease(button);
            return release.HasValue && release.Value.HoldDuration >= minHold;
        }
    }
}
