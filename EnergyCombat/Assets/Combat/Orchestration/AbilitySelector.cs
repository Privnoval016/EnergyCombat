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
     * <item>If a transition matches, follow it and return the target node's ability.</item>
     * <item>Otherwise scan all active loadouts in registration order — first match wins.</item>
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
        private readonly List<AbilityLoadout> _loadouts  = new List<AbilityLoadout>();
        private ComboDefinition _currentDefinition;
        private int _currentNodeIndex = -1;
        private int _interruptCount;
        private float _lastInputTime;
        private readonly CountdownTimer _comboWindowTimer;

        /** <summary>Creates a selector with an optional initial loadout.</summary> */
        public AbilitySelector(AbilityLoadout initialLoadout = null)
        {
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

        /** <summary>True when a combo sequence is active and the input window has not expired.</summary> */
        public bool IsInCombo =>
            _currentDefinition != null &&
            _currentNodeIndex >= 0 &&
            _comboWindowTimer.IsRunning &&
            !_comboWindowTimer.IsFinished;

        // ── Resolution ────────────────────────────────────────────────────────

        /** <summary>Attempts to resolve an ability from the buffer given the current context.</summary> */
        public AbilityDefinition Resolve(CombatInputBuffer buffer, CombatContext context)
        {
            if (IsInCombo)
            {
                var comboResult = TryResolveCombo(buffer, context);
                if (comboResult != null) return comboResult;
            }

            return TryResolveFromLoadouts(buffer, context);
        }

        // ── Executor notifications ────────────────────────────────────────────

        /**
         * <summary>
         * Called when an ability completes normally. Opens the next combo window if the
         * completed ability has a <see cref="AbilityDefinition.FollowUpCombo"/>.
         * </summary>
         */
        public void OnAbilityCompleted(AbilityDefinition executed)
        {
            _interruptCount = 0;

            if (executed?.FollowUpCombo?.RootNode != null)
            {
                _currentDefinition = executed.FollowUpCombo;
                _currentNodeIndex = 0;
                OpenComboWindow(_currentDefinition.Nodes[0].ComboWindowDuration);
            }
            else
            {
                ResetCombo();
            }
        }

        /** <summary>Called when an execution is interrupted with <see cref="ComboInterruptBehavior.BreakCombo"/>. Resets the combo immediately.</summary> */
        public void OnAbilityInterrupted() => ResetCombo();

        /**
         * <summary>
         * Called when an execution finishes with <see cref="ComboInterruptBehavior.PreserveCombo"/>.
         * Increments the interrupt counter and resets the combo only if
         * <see cref="ComboDefinition.MaxConcurrentInterrupts"/> is exceeded.
         * </summary>
         */
        public void OnAbilityComboPreserved()
        {
            _interruptCount++;
            if (_currentDefinition != null &&
                _currentDefinition.MaxConcurrentInterrupts > 0 &&
                _interruptCount >= _currentDefinition.MaxConcurrentInterrupts)
            {
                ResetCombo();
            }
        }

        /** <summary>Resets the combo pointer, counter, and stops the window timer.</summary> */
        public void ResetCombo()
        {
            _currentDefinition = null;
            _currentNodeIndex = -1;
            _interruptCount = 0;
            _comboWindowTimer.Stop();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private AbilityDefinition TryResolveCombo(CombatInputBuffer buffer, CombatContext context)
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

                _interruptCount = 0;
                _currentNodeIndex = transition.TargetNodeIndex;
                _lastInputTime = now;

                if (_currentNodeIndex >= 0 && targetNode != null)
                    OpenComboWindow(targetNode.ComboWindowDuration);
                else
                    ResetCombo();

                return targetNode?.Ability;
            }

            return null;
        }

        private AbilityDefinition TryResolveFromLoadouts(CombatInputBuffer buffer, CombatContext context)
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
                    return ability;
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

        private static bool IsTransitionInputSatisfied(ComboTransition transition, CombatInputBuffer buffer)
        {
            if (transition == null) return false;

            if (transition.RequireHold)
                return buffer.GetHoldDuration(transition.Button) > 0f ||
                       IsRecentHoldRelease(transition.Button, buffer, 0.3f);

            return buffer.HasRecentInput(transition.Button, 0.3f);
        }

        private static bool IsAbilityInputSatisfied(AbilityDefinition ability, CombatInputBuffer buffer)
        {
            const float inputGrace = 0.25f;

            if (ability.RequireHold)
            {
                float held = buffer.GetHoldDuration(ability.PrimaryInput);
                if (held >= ability.HoldThreshold) return true;
                return IsRecentHoldRelease(ability.PrimaryInput, buffer, inputGrace, ability.HoldThreshold);
            }

            return buffer.HasRecentInput(ability.PrimaryInput, inputGrace);
        }

        private static bool IsRecentHoldRelease(
            CombatInputButton button,
            CombatInputBuffer buffer,
            float withinSeconds,
            float minHold = 0f)
        {
            var release = buffer.GetLatestRelease(button, withinSeconds);
            return release.HasValue && release.Value.HoldDuration >= minHold;
        }
    }
}
