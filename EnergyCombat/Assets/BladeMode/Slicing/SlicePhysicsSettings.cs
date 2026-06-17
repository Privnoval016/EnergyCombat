using UnityEngine;

namespace BladeMode.Slicing
{
    [CreateAssetMenu(menuName = "BladeMode/Slice Physics Settings", fileName = "SlicePhysicsSettings")]
    public sealed class SlicePhysicsSettings : ScriptableObject
    {
        [Header("Launch")]
        [Tooltip("Base speed pieces fly away from the cut.")]
        public float LaunchForce = 6f;

        [Tooltip("How much the cut plane's normal blends into the launch direction. " +
                 "0 = purely away from cut point, 1 = purely along plane normal.")]
        [Range(0f, 1f)]
        public float NormalBlend = 0.4f;

        [Tooltip("Extra upward velocity added to every severed piece so they arc rather than slide.")]
        public float UpwardBias = 2f;

        [Header("Tumble")]
        [Tooltip("Random angular velocity magnitude. Higher = more spin.")]
        public float AngularSpinMultiplier = 2.5f;

        [Header("Upper Hull")]
        [Tooltip("Fraction of LaunchForce applied to the upper hull (prop path only). " +
                 "0 = upper hull stays still, 1 = same force as lower.")]
        [Range(0f, 1f)]
        public float UpperHullForceRatio = 0.35f;

        [Header("Mass")]
        [Tooltip("Mass assigned to each severed piece Rigidbody.")]
        public float SeveredPieceMass = 4f;
    }
}
