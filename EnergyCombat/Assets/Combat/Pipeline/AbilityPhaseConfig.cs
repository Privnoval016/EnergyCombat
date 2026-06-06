using System;

namespace Combat
{
    /**
     * <summary>
     * Obsolete wrapper — no longer used. Phases are now embedded directly on
     * <see cref="AbilityDefinition.Phases"/> via <c>[SerializeReference] AbilityPhase[]</c>.
     * This file is kept only to prevent compilation errors from old asset references.
     * </summary>
     */
    [Obsolete("Use AbilityDefinition.Phases ([SerializeReference] AbilityPhase[]) instead.")]
    [Serializable]
    public class AbilityPhaseConfig
    {
        public AbilityPhase Phase;
        public float FallbackDuration;
    }
}
