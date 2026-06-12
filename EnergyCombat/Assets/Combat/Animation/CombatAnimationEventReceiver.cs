using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Receives all combat animation events fired from attack clips and forwards them
     * to <see cref="CombatController.DispatchCombatEvent"/>. Attach to the same
     * GameObject as the <c>Animator</c> and assign the controller in the Inspector.
     * </summary>
     *
     * <remarks>
     * In the Unity Animation window, add events to your attack clips using:
     * <list type="bullet">
     *   <item><b>Function:</b> <c>OnCombatEvent</c></item>
     *   <item><b>String:</b> the event name — see <see cref="CombatAnimationEvents"/> for all constants</item>
     * </list>
     *
     * A single method handles everything: phase boundaries (<c>StartupEnd</c>, <c>ActiveEnd</c>,
     * <c>RecoveryEnd</c>) and gameplay signals (<c>ComboWindowOpen</c>, <c>MovementResume</c>, etc.).
     * <see cref="CombatController.DispatchCombatEvent"/> routes each name to the correct subsystem.
     * </remarks>
     */
    [AddComponentMenu("Combat/Animation/Combat Animation Event Receiver")]
    public class CombatAnimationEventReceiver : MonoBehaviour
    {
        /**
         * <summary>
         * The <see cref="CombatController"/> that owns the currently executing ability.
         * Must be assigned in the Inspector.
         * </summary>
         */
        [Tooltip("CombatController on the player. Receives all forwarded animation events.")]
        [SerializeField] private CombatController _controller;

        /**
         * <summary>
         * Universal entry point for all combat animation events.
         * In the Animation window set <b>Function</b> to <c>OnCombatEvent</c> and
         * <b>String</b> to the desired event name (see <see cref="CombatAnimationEvents"/>).
         * </summary>
         *
         * <param name="eventName">
         * Name of the event. Use the constants in <see cref="CombatAnimationEvents"/>
         * to avoid typos.
         * </param>
         */
        public void OnCombatEvent(string eventName)
        {
            _controller?.DispatchCombatEvent(eventName);
        }
    }
}
