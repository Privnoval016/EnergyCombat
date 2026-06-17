using BladeMode.Slicing;
using UnityEngine;

namespace BladeMode.Effects
{
    /// <summary>
    /// Plain C# interface for blade mode events. Implement on any [Serializable]
    /// class and add instances to a BladeModeEffectSettings asset.
    ///
    /// Built-in implementations: ParticleEffect, PostProcessEffect, AudioEffect, SliceVFXEffect.
    ///
    /// To add a custom effect: create a [Serializable] class implementing this interface,
    /// then use the [+] button on the BladeModeEffectSettings Effects list.
    /// </summary>
    public interface IBladeModeEffect
    {
        /// Called once during coordinator Start — cache the player transform here,
        /// or create any runtime objects (Volumes, sources) that need a scene anchor.
        void Initialize(Transform playerTransform);
        void OnEnterBladeMode();
        void OnExitBladeMode();
        void OnCutExecuted(SliceResult[] results, Vector3 planePoint, Vector3 planeNormal);
    }
}
