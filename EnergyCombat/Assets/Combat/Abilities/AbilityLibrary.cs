using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * ScriptableObject that holds a flat catalogue of all <see cref="AbilityDefinition"/>
     * assets available to a character.
     * </summary>
     *
     * <remarks>
     * This is the master catalogue — not all abilities need to be active at once.
     * Use <see cref="AbilityLoadout"/> to define which abilities are currently equippable.
     * Abilities only reachable via combo trees do not need to be listed here.
     * </remarks>
     */
    [CreateAssetMenu(fileName = "New Ability Library", menuName = "Combat/Ability Library")]
    public class AbilityLibrary : ScriptableObject
    {
        /** <summary>All abilities in this library.</summary> */
        [Tooltip("Master list of all abilities. Use AbilityLoadout to activate a subset.")]
        public AbilityDefinition[] Abilities;
    }
}
