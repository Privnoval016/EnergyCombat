using BladeMode.Slicing;
using UnityEngine;

namespace BladeMode.Core
{
    [CreateAssetMenu(menuName = "BladeMode/Settings", fileName = "BladeModeSettings")]
    public sealed class BladeModeSettings : ScriptableObject
    {
        [Header("Time Dilation")]
        [Tooltip("Target Time.timeScale while blade mode is active. 0.15 = world runs at 15% speed; the player moves at full real-world speed via unscaled delta time.")]
        public float TimeScale = 0.15f;

        [Tooltip("Real-time seconds to ramp time scale down and fade the arm IK in when entering blade mode.")]
        public float EnterDuration = 0.25f;

        [Tooltip("Real-time seconds to restore time scale and fade the arm IK out when exiting blade mode.")]
        public float ExitDuration = 0.35f;

        [Header("Right Stick")]
        [Tooltip("Stick magnitude (0–1) below which the cut plane stops rotating and a button cut defaults to cardinal (L/H) rather than stick direction.")]
        [Range(0f, 0.5f)] public float JoystickDeadzone = 0.2f;

        [Tooltip("Flip the direction the cut plane spins when the stick rotates. Disable if clockwise stick makes the plane go counterclockwise.")]
        public bool InvertStickRotation = false;

        [Header("Cardinal Cuts (L / H)")]
        [Tooltip("Base tilt in degrees applied to every L (horizontal) or H (vertical) cut. " +
                 "Horizontal cuts tilt forward/backward around the player's right axis; " +
                 "vertical cuts tilt left/right around the player's forward axis. " +
                 "0 = perfectly axis-aligned. Combine with CutAngleVariance for organic feel.")]
        [Range(0f, 30f)] public float CutAngleOffset = 8f;

        [Tooltip("Maximum random variance in degrees added on top of CutAngleOffset each time a cardinal cut fires. " +
                 "A random value in [-CutAngleVariance, +CutAngleVariance] is chosen per cut, so no two cardinal cuts land at exactly the same angle. " +
                 "0 = every cut is identical (useful for precise testing). 5–10 is a good organic range.")]
        [Range(0f, 30f)] public float CutAngleVariance = 5f;

        [Tooltip("Vertical offset from the player root at which the cut plane is anchored. " +
                 "Set to roughly half your character's standing height so horizontal cuts land at the waist rather than the feet.")]
        [Range(0f, 2.5f)] public float CutPlaneHeight = 1.0f;

        [Header("Detection")]
        [Tooltip("Physics layers that can be sliced. Must match the layers your SliceableSurface/SliceableBody objects are on.")]
        public LayerMask SliceableLayers;

        [Tooltip("Radius of the OverlapSphere around the player used to find sliceable candidates. " +
                 "Larger values let you slice distant or wide objects but also increase the chance of hitting unintended targets.")]
        public float DetectionRadius = 4f;

        [Tooltip("Half-angle cone (degrees) around the camera's forward direction within which targets can be sliced. " +
                 "Only the horizontal component is tested — height is ignored. " +
                 "90 = full hemisphere in front of the camera. 180 = all-around (no filtering).")]
        [Range(5f, 180f)] public float DetectionAngle = 90f;

        [Header("Slicing")]
        [Tooltip("Real-time duration of the arm-sweep animation when a cut executes. " +
                 "Also controls how long the Executing state blocks before the player can chain another cut.")]
        [Range(0.05f, 0.5f)] public float SliceDuration = 0.15f;

        [Tooltip("Cross-section material used when a sliceable object has no CrossSectionMaterial of its own. " +
                 "Assign a BurnEdge or FrozenEdge material here.")]
        public Material DefaultCrossSectionMaterial;

        [Tooltip("Sub-asset that controls how severed pieces fly: launch force, upward bias, spin, and mass. " +
                 "Create via Assets → Create → BladeMode → Slice Physics Settings.")]
        public SlicePhysicsSettings PhysicsSettings;

        [Header("Camera")]
        [Tooltip("Multiplier applied to look input while blade mode is active. " +
                 "Lower values make aiming the cut plane deliberate rather than twitchy. " +
                 "1 = unchanged sensitivity, 0 = camera locked.")]
        [Range(0f, 1f)] public float BladeCameraLookMultiplier = 0.35f;

        [Header("Visualization")]
        [Tooltip("Prefab for the cut-plane quad. Must use a mesh whose surface normal points along local Y " +
                 "and a material built on the CutPlane shader.")]
        public GameObject CutPlanePrefab;

        [Tooltip("Per-axis local scale applied to the cut-plane prefab. X/Z control the visible width/height of the quad; " +
                 "Y is the thickness (irrelevant for a flat quad but available for custom prefabs).")]
        public Vector3 CutPlaneScale = new Vector3(3.5f, 3.5f, 3.5f);
    }
}
