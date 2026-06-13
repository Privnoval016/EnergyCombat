using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DynamicPhysics;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Synchronous pipeline phase that applies a one-shot physics impulse to the player
     * via <c>MotionOrchestrator.AddImpulse</c> and returns immediately.
     * </summary>
     *
     * <remarks>
     * Useful for the sharp initial "launch" sensation at the start of a plunge attack
     * (downward impulse) or any move that needs a self-directed velocity kick.
     * For sustained velocity maintenance during a fall use
     * <see cref="LoopUntilGroundedPhase.MinPlungeSpeed"/> instead, which enforces a
     * floor every physics tick rather than applying a single impulse.
     * </remarks>
     */
    [Serializable]
    public class SelfImpulsePhase : AbilityPhase
    {
        /**
         * <summary>
         * World-space impulse vector applied to the player.
         * <c>(0, -15, 0)</c> gives a sharp downward burst for a plunge entry.
         * When <see cref="UseCharacterOrientation"/> is <c>true</c>, this vector is treated
         * as local-space and rotated to match the character's current facing.
         * </summary>
         */
        [Tooltip("World-space impulse to apply. (0,-15,0) gives a sharp downward plunge burst. Enable UseCharacterOrientation to rotate it with the character's facing.")]
        public Vector3 Impulse = new Vector3(0f, -10f, 0f);

        /**
         * <summary>
         * When <c>true</c>, <see cref="Impulse"/> is rotated from local-space to world-space
         * using the character's current facing direction, so a forward impulse always pushes
         * the player in the direction they are facing regardless of camera angle.
         * </summary>
         */
        [Tooltip("Treat Impulse as local-space and rotate it to match the character's current facing direction.")]
        public bool UseCharacterOrientation = false;

        /** <inheritdoc /> */
        public override UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var orchestrator = context.Controller?.GetComponent<MotionOrchestrator>();
            if (orchestrator != null)
            {
                Vector3 impulse = UseCharacterOrientation && context.Controller != null
                    ? context.Controller.transform.TransformDirection(Impulse)
                    : Impulse;
                orchestrator.AddImpulse(impulse);
            }

            return UniTask.CompletedTask;
        }
    }
}
