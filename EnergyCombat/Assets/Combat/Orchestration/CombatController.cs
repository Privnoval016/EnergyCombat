using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions.EntityComponent;
using Extensions.EventBus;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * The primary MonoBehaviour for the combat system. Owns all combat subsystems
     * (input buffer, selector, executor, modifiers, animation driver, hitbox registry)
     * and coordinates their interaction.
     * </summary>
     *
     * <remarks>
     * <c>CombatController</c> is the single public API surface for external systems:
     * <list type="bullet">
     * <item><see cref="PushInput"/> — route input events from <c>PlayerController</c>.</item>
     * <item><see cref="AddModifier"/> / <see cref="RemoveModifier"/> — manage runtime modifiers.</item>
     * <item><see cref="CancelCurrentAbility"/> — interrupt execution (e.g. on dodge cancel).</item>
     * <item><see cref="EquipLoadout"/> — replace all loadouts with one (single-weapon convenience).</item>
     * <item><see cref="AddLoadout"/> / <see cref="RemoveLoadout"/> — dual-wield / multi-loadout management.</item>
     * <item><see cref="GetHitboxController"/> — look up a named hitbox for per-phase activation.</item>
     * </list>
     *
     * <b>Hitbox setup:</b> place one or more <see cref="WeaponHitboxController"/> components
     * anywhere in the character hierarchy. They are auto-discovered at Awake by their
     * <c>HitboxId</c> string. No manual wiring needed.
     * </remarks>
     */
    [AddComponentMenu("Combat/Combat Controller")]
    public class CombatController : MonoBehaviour
    {
        #region Inspector

        /**
         * <summary>
         * Default set of ability loadouts active at startup.
         * For single-weapon characters one entry is enough.
         * For dual-wield, add one entry per weapon. All loadouts are searched for matching
         * abilities on each input event — first match wins.
         * Swap at runtime via <see cref="AddLoadout"/> / <see cref="RemoveLoadout"/> or
         * <see cref="EquipLoadout"/> (replaces all).
         * </summary>
         */
        [Header("Abilities")]
        [SerializeField] private AbilityLoadout[] _defaultLoadouts;

        /**
         * <summary>
         * Animation driver implementation. Assign an <c>AnimancerAnimationDriver</c>
         * (or any <see cref="IAnimationDriver"/> MonoBehaviour) in the Inspector.
         * If null, a <see cref="NullAnimationDriver"/> is used automatically.
         * </summary>
         */
        [Header("Animation")]
        [Tooltip("MonoBehaviour implementing IAnimationDriver. Leave null to use NullAnimationDriver.")]
        [SerializeField] private MonoBehaviour _animationDriverSource;

        #endregion

        #region Subsystems

        private IAnimationDriver _animationDriver;
        private ModifierContainer _modifiers;
        private AbilitySelector _selector;
        private AbilityExecutor _executor;

        /** <summary>Hitbox controllers keyed by <see cref="IHitboxController.HitboxId"/>.</summary> */
        private readonly Dictionary<string, IHitboxController> _hitboxRegistry = new();

        /**
         * <summary>
         * Extensible component bus for additional combat subsystems.
         * Use <c>Components.AddComponent&lt;T&gt;()</c> to register custom systems
         * (stamina, status effects, etc.) without modifying this class.
         * </summary>
         */
        public Entity<ICombatComponent> Components { get; } = new Entity<ICombatComponent>();

        #endregion

        #region Execution State

        private CancellationTokenSource _cts;
        private CombatContext _activeContext;

        /** <summary>The currently active input buffer.</summary> */
        public CombatInputBuffer InputBuffer { get; private set; }

        /**
         * <summary>
         * <c>true</c> while an ability pipeline is actively running.
         * Used by the player state machine to enter and remain in <c>AttackingState</c>.
         * </summary>
         */
        public bool IsExecuting { get; private set; }

        /** <summary>The execution context of the currently active ability, or null.</summary> */
        public CombatContext ActiveContext => _activeContext;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InputBuffer = new CombatInputBuffer(16);

            _animationDriver = _animationDriverSource as IAnimationDriver
                               ?? new NullAnimationDriver();

            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is IHitboxController ctrl && !string.IsNullOrEmpty(ctrl.HitboxId))
                    _hitboxRegistry[ctrl.HitboxId] = ctrl;
            }

            _modifiers = new ModifierContainer();
            _selector = new AbilitySelector();

            if (_defaultLoadouts != null)
                foreach (var l in _defaultLoadouts)
                    _selector.AddLoadout(l);

            _executor = new AbilityExecutor(_modifiers, _animationDriver, _selector);
        }

        private void Update()
        {
            _modifiers.RemoveExpired();
            InputBuffer.PruneOlderThan(2f);
        }

        private void OnDestroy()
        {
            CancelCurrentAbility();
        }

        #endregion

        #region Public API — Loadouts

        /**
         * <summary>
         * Replaces all active loadouts with a single one. Resets the current combo state.
         * Convenience overload for single-weapon characters or simple weapon swaps.
         * </summary>
         */
        public void EquipLoadout(AbilityLoadout loadout)
        {
            _selector.SetLoadout(loadout);
        }

        /**
         * <summary>
         * Adds a loadout to the active pool without disturbing existing ones or the combo state.
         * Use when the player picks up a second weapon.
         * </summary>
         */
        public void AddLoadout(AbilityLoadout loadout) => _selector.AddLoadout(loadout);

        /**
         * <summary>
         * Removes a loadout from the active pool. Does not reset the combo state.
         * Use when the player drops a weapon.
         * </summary>
         */
        public void RemoveLoadout(AbilityLoadout loadout) => _selector.RemoveLoadout(loadout);

        /**
         * <summary>
         * Replaces all active loadouts at once. Resets the combo state.
         * Use for full stance / character kit swaps.
         * </summary>
         */
        public void SetLoadouts(AbilityLoadout[] loadouts) => _selector.SetLoadouts(loadouts);

        #endregion

        #region Public API — Input & Execution

        /**
         * <summary>
         * Routes a combat input event from <c>PlayerController</c> into the buffer
         * and attempts to trigger or advance an ability.
         * </summary>
         */
        public void PushInput(CombatInputEvent evt)
        {
            InputBuffer.Push(evt);
            TryTriggerAbility();
        }

        /**
         * <summary>
         * Immediately cancels the currently executing ability. The pipeline and any
         * active hitboxes or animation are cleaned up via the cancellation token.
         * </summary>
         */
        public void CancelCurrentAbility()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                _cts.Cancel();
        }

        #endregion

        #region Public API — Hitboxes

        /**
         * <summary>
         * Returns the <see cref="IHitboxController"/> registered under <paramref name="hitboxId"/>,
         * or <c>null</c> if none was found. Called by <see cref="ActivePhase"/> at runtime.
         * </summary>
         */
        public IHitboxController GetHitboxController(string hitboxId)
        {
            if (string.IsNullOrEmpty(hitboxId)) return null;
            _hitboxRegistry.TryGetValue(hitboxId, out var ctrl);
            return ctrl;
        }

        /**
         * <summary>
         * Manually registers a hitbox controller. Useful for dynamically spawned weapons
         * not present at Awake time.
         * </summary>
         */
        public void RegisterHitboxController(string hitboxId, IHitboxController ctrl)
        {
            if (string.IsNullOrEmpty(hitboxId) || ctrl == null) return;
            _hitboxRegistry[hitboxId] = ctrl;
        }

        /** <summary>Removes a hitbox controller from the registry by id.</summary> */
        public void UnregisterHitboxController(string hitboxId)
        {
            if (!string.IsNullOrEmpty(hitboxId))
                _hitboxRegistry.Remove(hitboxId);
        }

        #endregion

        #region Public API — Modifiers

        /** <summary>Adds a modifier to the active modifier pool.</summary> */
        public void AddModifier(ICombatModifier modifier) => _modifiers.Add(modifier);

        /** <summary>Removes a specific modifier instance. Does nothing if not found.</summary> */
        public void RemoveModifier(ICombatModifier modifier) => _modifiers.Remove(modifier);

        /** <summary>Read-only access to the modifier container for debug display.</summary> */
        public ModifierContainer Modifiers => _modifiers;

        #endregion

        #region Internal Execution

        private void TryTriggerAbility()
        {
            bool comboWindowOpen = _activeContext != null &&
                                   _activeContext.HasTag(CombatTag.ComboWindowOpen);

            if (IsExecuting && !comboWindowOpen) return;

            var ability = _selector.Resolve(InputBuffer, _activeContext);
            if (ability == null) return;
            if (!ability.AreConditionsMet(_activeContext ?? new CombatContext())) return;

            bool preserveCombo = IsExecuting &&
                                 ability.InterruptBehavior == ComboInterruptBehavior.PreserveCombo;

            if (IsExecuting) CancelCurrentAbility();

            LaunchExecution(ability, preserveCombo);
        }

        private void LaunchExecution(AbilityDefinition ability, bool preserveCombo)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            IsExecuting = true;
            _activeContext = null;

            RunExecutionAsync(ability, preserveCombo, _cts.Token).Forget();
        }

        private async UniTaskVoid RunExecutionAsync(
            AbilityDefinition ability,
            bool preserveCombo,
            CancellationToken token)
        {
            try
            {
                _activeContext = await _executor.ExecuteAsync(ability, this, preserveCombo, token);
            }
            finally
            {
                IsExecuting = false;
                _activeContext = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        #endregion
    }
}
