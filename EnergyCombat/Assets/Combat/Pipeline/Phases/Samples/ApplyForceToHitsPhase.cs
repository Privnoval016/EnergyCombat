using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Phase that applies a physics force to the <c>Rigidbody</c> of every target recorded
     * in <see cref="CombatContext.RegisteredHits"/>. Run this after an <see cref="ActivePhase"/>
     * so hits have already been recorded.
     * </summary>
     *
     * <remarks>
     * If <see cref="UseAttackerOrientation"/> is true the force vector is rotated to align
     * with the attacker's forward direction at execution time, so knockback always pushes
     * targets away from the attacker regardless of world orientation.
     * </remarks>
     *
     * <example>
     * Pipeline: StartupPhase → ActivePhase → ApplyForceToHitsPhase (ForceVector = (0,2,4), Mode = Impulse)
     * Result: every hit enemy is knocked back and slightly upward.
     * </example>
     */
    [Serializable]
    public class ApplyForceToHitsPhase : AbilityPhase
    {
        [Tooltip("Force to apply in the attacker's local space (or world space if UseAttackerOrientation is false).")]
        public Vector3 ForceVector = new(0f, 0f, 5f);

        [Tooltip("Unity ForceMode for Rigidbody.AddForce.")]
        public ForceMode Mode = ForceMode.Impulse;

        [Tooltip("When true, ForceVector is rotated to match the attacker's facing direction.")]
        public bool UseAttackerOrientation = true;

        public override UniTask ExecuteAsync(CombatContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (context.RegisteredHits.Count == 0) return UniTask.CompletedTask;

            Vector3 force = ForceVector;
            if (UseAttackerOrientation && context.Controller != null)
                force = context.Controller.transform.rotation * ForceVector;

            foreach (var hit in context.RegisteredHits)
            {
                if (hit?.Target == null) continue;
                var rb = hit.Target.GetComponent<Rigidbody>();
                rb?.AddForce(force, Mode);
            }

            return UniTask.CompletedTask;
        }
    }
}
