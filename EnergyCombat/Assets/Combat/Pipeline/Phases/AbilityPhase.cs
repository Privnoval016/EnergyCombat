using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Combat
{
    /**
     * <summary>
     * Abstract base class for all ability pipeline phases.
     * Phases are <c>[Serializable]</c> classes configured inline on <see cref="AbilityDefinition"/>
     * via <c>[SerializeReference]</c> — one asset, all phases embedded.
     * </summary>
     *
     * <remarks>
     * Phases hold per-definition configuration, not runtime state.
     * All mutable execution state must live on the <see cref="CombatContext"/> or on local
     * variables inside <see cref="ExecuteAsync"/>. Always respect the cancellation token
     * and clean up resources (hitboxes, timers) in a <c>finally</c> block.
     *
     * To add a new phase type: create a <c>[Serializable]</c> class extending
     * <c>AbilityPhase</c> and override <see cref="ExecuteAsync"/>.
     * It will automatically appear in the Unity Inspector's <c>[SerializeReference]</c>
     * type picker on <c>AbilityDefinition.Phases</c>.
     * </remarks>
     */
    [Serializable]
    public abstract class AbilityPhase : IPipelineStep
    {
        /** <summary>Human-readable label shown in the debug overlay.</summary> */
        public string PhaseName;

        /** <inheritdoc /> */
        public abstract UniTask ExecuteAsync(CombatContext context, CancellationToken token);

        /** <summary>Returns <see cref="PhaseName"/> for editor display.</summary> */
        public override string ToString() =>
            string.IsNullOrEmpty(PhaseName) ? GetType().Name : PhaseName;
    }
}
