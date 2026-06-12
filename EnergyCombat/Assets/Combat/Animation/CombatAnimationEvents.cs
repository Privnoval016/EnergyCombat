namespace Combat
{
    /**
     * <summary>
     * Compile-time constants for all animation event names used by the combat system.
     * </summary>
     *
     * <remarks>
     * In the Unity Animation window, add an animation event to your attack clip with:
     * <list type="bullet">
     *   <item><b>Function:</b> <c>OnCombatEvent</c></item>
     *   <item><b>String:</b> one of the constants below</item>
     * </list>
     *
     * All events are routed through a single <see cref="CombatAnimationEventReceiver"/>
     * component on the player's Animator GameObject, which dispatches them to
     * <see cref="CombatController.DispatchCombatEvent"/>.
     *
     * <b>Phase boundary events</b> — place these to define frame data:
     * <list type="bullet">
     *   <item><see cref="StartupEnd"/> — ends startup; hitboxes activate after this frame.</item>
     *   <item><see cref="ActiveEnd"/> — ends active frames; hitboxes deactivate, recovery begins.</item>
     *   <item><see cref="RecoveryEnd"/> — ends recovery; ability fully completes.</item>
     * </list>
     *
     * <b>Gameplay signal events</b> — place these anywhere, independent of phase boundaries:
     * <list type="bullet">
     *   <item><see cref="ComboWindowOpen"/> — player may now chain into the next combo attack.</item>
     *   <item><see cref="ComboWindowClose"/> — combo chain window closes (optional; use for tight timing).</item>
     *   <item><see cref="MovementResume"/> — player may move freely; entering movement cancels the combo.</item>
     * </list>
     *
     * These signal events can be placed before or after phase boundaries in any order.
     * A common layout for a grounded attack:
     * <code>
     ///  [0]──startup──[StartupEnd]──active──[ComboWindowOpen][ActiveEnd]──recovery──[MovementResume][RecoveryEnd]
     * </code>
     * </remarks>
     */
    public static class CombatAnimationEvents
    {
        // ── Phase boundary events ─────────────────────────────────────────────

        /**
         * <summary>
         * Ends the <see cref="StartupPhase"/>. Place at the frame where the hitbox should
         * become active. Before this frame the player is fully committed to the attack.
         * </summary>
         */
        public const string StartupEnd = "StartupEnd";

        /**
         * <summary>
         * Ends the <see cref="ActivePhase"/>. Hitboxes deactivate on this frame and the
         * <see cref="RecoveryPhase"/> begins immediately after.
         * </summary>
         */
        public const string ActiveEnd = "ActiveEnd";

        /**
         * <summary>
         * Ends the <see cref="RecoveryPhase"/>. The ability pipeline completes and the
         * character returns to neutral state.
         * </summary>
         */
        public const string RecoveryEnd = "RecoveryEnd";

        // ── Gameplay signal events ────────────────────────────────────────────

        /**
         * <summary>
         * Opens the combo chain input window. From this frame the player can press an
         * attack button to chain into the next move in the combo sequence.
         * Can be placed anywhere on the timeline — before or after <see cref="ActiveEnd"/>.
         * </summary>
         */
        public const string ComboWindowOpen = "ComboWindowOpen";

        /**
         * <summary>
         * Closes the combo chain input window early. Optional — use when you want a strict,
         * narrow timing window for chaining (e.g. pro-action-style combos).
         * If not placed, the window stays open until <see cref="RecoveryEnd"/>.
         * </summary>
         */
        public const string ComboWindowClose = "ComboWindowClose";

        /**
         * <summary>
         * Signals that the player may resume free movement. After this frame, directional
         * input is accepted; entering movement cancels the combo chain.
         * If not placed, movement is locked until the ability fully completes.
         * </summary>
         */
        public const string MovementResume = "MovementResume";
    }
}
