using BladeMode.Slicing;
using UnityEngine;

namespace BladeMode.Effects
{
    /// <summary>
    /// Thin MonoBehaviour bridge that owns a BladeModeEffectSettings asset and
    /// forwards blade mode events to it.
    ///
    /// All effect configuration lives in the Settings asset — drag a
    /// BladeModeEffectSettings ScriptableObject into the Settings field.
    /// Assign this coordinator to BladeModeController._effects in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BladeModeEffectCoordinator : MonoBehaviour
    {
        [Tooltip("Asset that contains all serialized effect instances for this configuration.")]
        [SerializeField] BladeModeEffectSettings _settings;

        // Start (not Awake) so all MonoBehaviour Awakes have already run before Initialize fires.
        void Start() => _settings?.Initialize(transform);

        public void OnEnter() => _settings?.OnEnter();
        public void OnExit()  => _settings?.OnExit();

        public void OnCutExecuted(SliceResult[] results, Vector3 planePoint, Vector3 planeNormal)
            => _settings?.OnCutExecuted(results, planePoint, planeNormal);
    }
}
