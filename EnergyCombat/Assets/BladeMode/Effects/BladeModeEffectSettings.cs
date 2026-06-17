using System.Collections.Generic;
using BladeMode.Slicing;
using UnityEngine;

namespace BladeMode.Effects
{
    /// <summary>
    /// ScriptableObject that holds blade mode effects as [SerializeReference] polymorphic
    /// instances. Create one via Assets > Create > BladeMode > Effect Settings, then
    /// assign it to BladeModeEffectCoordinator.
    ///
    /// Add effects with the [+] button on the Effects list — Unity's type dropdown will
    /// show every [Serializable] class implementing IBladeModeEffect.
    /// </summary>
    [CreateAssetMenu(menuName = "BladeMode/Effect Settings", fileName = "BladeModeEffectSettings")]
    public sealed class BladeModeEffectSettings : ScriptableObject
    {
        [SerializeReference]
        public List<IBladeModeEffect> Effects = new();

        public void Initialize(Transform playerTransform)
        {
            foreach (var e in Effects) e?.Initialize(playerTransform);
        }

        public void OnEnter()
        {
            foreach (var e in Effects) e?.OnEnterBladeMode();
        }

        public void OnExit()
        {
            foreach (var e in Effects) e?.OnExitBladeMode();
        }

        public void OnCutExecuted(SliceResult[] results, Vector3 planePoint, Vector3 planeNormal)
        {
            foreach (var e in Effects) e?.OnCutExecuted(results, planePoint, planeNormal);
        }
    }
}
