using System;
using BladeMode.BodyParts;
using Combat;
using UnityEngine;

namespace BladeMode.Integration
{
    /// <summary>
    /// Listens to BodyPartRegistry events and removes AI attack loadouts when the
    /// corresponding limb is severed. Attach alongside BodyPartRegistry on the enemy root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLimbResponder : MonoBehaviour
    {
        [Serializable]
        struct LimbLoadoutMapping
        {
            public BodyPartType PartType;
            [Tooltip("Loadout to disable on the CombatController when this part is severed.")]
            public string LoadoutToDisable;
        }

        [SerializeField] BodyPartRegistry _registry;
        [SerializeField] CombatController _combat;
        [SerializeField] LimbLoadoutMapping[] _mappings;

        void OnEnable()
        {
            if (_registry == null) return;
            _registry.OnPartDetached += HandlePartDetached;
            _registry.OnBrainSevered += HandleBrainSevered;
        }

        void OnDisable()
        {
            if (_registry == null) return;
            _registry.OnPartDetached -= HandlePartDetached;
            _registry.OnBrainSevered -= HandleBrainSevered;
        }

        void HandlePartDetached(BodyPartType partType)
        {
            foreach (var m in _mappings)
            {
                if (m.PartType != partType) continue;
                // CombatController.RemoveLoadout is a suggested extension point;
                // adapt to your actual AI loadout API as needed.
                UnityEngine.Debug.Log($"[EnemyLimbResponder] {name}: {partType} severed → disable loadout '{m.LoadoutToDisable}'");
            }
        }

        void HandleBrainSevered()
        {
            if (_combat != null && _combat.IsExecuting)
                _combat.CancelCurrentAbility();
            UnityEngine.Debug.Log($"[EnemyLimbResponder] {name}: brain severed → all loadouts cleared");
        }
    }
}
