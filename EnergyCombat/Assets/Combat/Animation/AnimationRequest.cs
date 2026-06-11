using System;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Data object describing everything the <see cref="IAnimationDriver"/> needs to
     * play an ability animation. Fully decouples gameplay logic from the animation backend.
     * </summary>
     */
    [Serializable]
    public class AnimationRequest
    {
        /** <summary>The clip to play.</summary> */
        [Header("Clip")]
        public AnimationClip Clip;

        /** <summary>Playback speed multiplier. 1.0 = normal, 2.0 = double speed.</summary> */
        [Header("Playback")]
        [Tooltip("Playback speed multiplier. 1 = normal.")]
        public float Speed = 1f;

        /** <summary>Whether this animation drives root motion on the character.</summary> */
        [Tooltip("Enable root motion for this clip.")]
        public bool UseRootMotion;

        /**
         * <summary>
         * When <c>true</c>, joystick steering is suppressed via <see cref="DynamicPhysics.MotionTag.AttackMovementLocked"/>
         * for the duration of this animation so the clip drives movement exclusively.
         * </summary>
         */
        [Tooltip("Lock joystick movement during this attack animation.")]
        public bool LockMovement;

        /** <summary>Animator layer index to play on. 0 = base.</summary> */
        [Tooltip("Animator layer. 0 = base, higher = overlay.")]
        public int Layer;

        /** <summary>Blend-in duration in seconds. 0 = instant switch.</summary> */
        [Tooltip("Cross-fade blend in duration.")]
        public float FadeInDuration = 0.1f;

        /** <summary>Whether the clip loops indefinitely.</summary> */
        [Tooltip("Loop this clip.")]
        public bool Loop;

        /**
         * <summary>
         * Named animation events the pipeline may await via
         * <see cref="AnimationHandle.WaitForEventAsync"/>.
         * Event names must match the string parameter of Unity animation events.
         * </summary>
         */
        [Tooltip("Names of animation events pipeline phases can wait for.")]
        public string[] EventNames;
    }
}
