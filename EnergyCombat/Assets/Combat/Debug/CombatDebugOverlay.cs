using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Renders an on-screen IMGUI overlay showing the real-time state of a
     * <see cref="CombatController"/> — active ability, pipeline step, tags, stats,
     * modifiers, combo window, and recent input buffer.
     * </summary>
     *
     * <remarks>
     * Attach to any GameObject in the scene. Assign the controller in the Inspector.
     * Toggle with the key specified by <see cref="ToggleKey"/> (default: <c>F2</c>).
     * Uses IMGUI <c>GUILayout</c> — no additional packages required.
     * </remarks>
     */
    [AddComponentMenu("Combat/Combat Debug Overlay")]
    public class CombatDebugOverlay : MonoBehaviour
    {
        #region Inspector

        /** <summary>The controller to inspect.</summary> */
        [Header("Target")]
        public CombatController Target;

        /** <summary>Key to toggle the overlay visibility.</summary> */
        [Header("Settings")]
        public KeyCode ToggleKey = KeyCode.F2;

        /** <summary>Whether the overlay is currently visible.</summary> */
        public bool ShowOverlay = true;

        /** <summary>Screen position of the overlay window top-left corner.</summary> */
        public Vector2 WindowPosition = new Vector2(10f, 10f);

        /** <summary>Width of the overlay panel.</summary> */
        public float WindowWidth = 360f;

        #endregion

        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _activeTagStyle;
        private bool _stylesInitialized;

        private void OnGUI()
        {
            if (!ShowOverlay || Target == null) return;

            InitStyles();

            GUILayout.BeginArea(new Rect(WindowPosition.x, WindowPosition.y, WindowWidth, Screen.height - WindowPosition.y - 10f));
            GUILayout.BeginVertical(GUI.skin.box);

            DrawHeader("COMBAT SYSTEM");

            DrawExecutionSection();
            DrawComboSection();
            DrawTagSection();
            DrawStatSection();
            DrawModifierSection();
            DrawInputBufferSection();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawHeader(string title)
        {
            GUILayout.Label(title, _headerStyle);
        }

        private void DrawExecutionSection()
        {
            GUILayout.Label("── Execution ──", _headerStyle);

            bool executing = Target.IsExecuting;
            var context = Target.ActiveContext;

            GUILayout.Label($"Executing: {(executing ? "<color=lime>YES</color>" : "<color=red>NO</color>")}", _labelStyle);

            if (context != null)
            {
                GUILayout.Label($"Ability: {context.Ability?.AbilityName ?? "<none>"}", _labelStyle);
                GUILayout.Label($"Combo Window: {(context.HasTag(CombatTag.ComboWindowOpen) ? "<color=lime>OPEN</color>" : "closed")}", _labelStyle);
            }
        }

        /**
         * <summary>
         * Draws the combo chain section with three independent state lines:
         * <list type="bullet">
         *   <item><b>Chain</b> — whether <c>IsInCombo</c> is alive. Alive means the combo pointer
         *     is valid; dead means the next press will fire a starter. Distinct from the input
         *     window and from the expiry timer.</item>
         *   <item><b>Input</b> — whether the player can press right now (the
         *     <see cref="CombatTag.ComboWindowOpen"/> recovery tag is set, or the selector is in
         *     pre-open state). This controls whether <c>TryTriggerAbility</c> proceeds past its
         *     early-return guard.</item>
         *   <item><b>Expiry</b> — the auto-reset timer state. "ticking" means the countdown is
         *     running; "held (no timer)" means <c>IsInCombo</c> is true but the timer is stopped
         *     (either executing with the deferred window, or <c>ComboWindowDuration = 0</c>).</item>
         * </list>
         * </summary>
         */
        private void DrawComboSection()
        {
            GUILayout.Space(4f);
            GUILayout.Label("── Combo Chain ──", _headerStyle);

            var selector = Target.Selector;
            var context  = Target.ActiveContext;

            string abilityName  = context?.Ability?.AbilityName ?? "<none>";
            string abilityColor = Target.IsExecuting ? "lime" : "grey";
            GUILayout.Label($"<color={abilityColor}><b>ATTACK: {abilityName}</b></color>", _labelStyle);

            var comboDef = selector.CurrentComboDefinition;
            if (comboDef != null && selector.CurrentComboNodeIndex >= 0)
            {
                // ── Chain state (IsInCombo) ──────────────────────────────────────────
                // True whenever the combo pointer is valid and the expiry timer has not
                // elapsed. The overlay previously used IsComboWindowActive (the timer)
                // here, which showed CLOSED during execution even though chaining worked.
                string chainState = selector.IsInCombo
                    ? "<color=lime>ALIVE</color>"
                    : "<color=red>DEAD</color>";

                // ── Input window ─────────────────────────────────────────────────────
                // Reflects whether the player's press is accepted right now.
                // ComboWindowOpen = recovery tag (AllowComboCancel / animation event).
                // PRE-OPEN = combo set up but recovery not yet opened.
                bool inputOpen = context != null && context.HasTag(CombatTag.ComboWindowOpen);
                string inputState;
                if (inputOpen)
                    inputState = "<color=lime>OPEN</color>";
                else if (selector.IsPreOpened)
                    inputState = "<color=yellow>PRE-OPEN</color>";
                else
                    inputState = "<color=grey>waiting</color>";

                // ── Expiry timer ──────────────────────────────────────────────────────
                // "ticking"        = countdown running (started after ability completes).
                // "held (no timer)"= IsInCombo=true but timer stopped — either the ability
                //                    is executing with the deferred window, or
                //                    ComboWindowDuration=0 (infinite hold after completion).
                // "expired"        = timer elapsed, IsInCombo=false.
                string expiryState;
                if (selector.IsComboWindowActive)
                    expiryState = "<color=lime>ticking</color>";
                else if (selector.IsInCombo)
                    expiryState = "<color=cyan>held (no timer)</color>";
                else
                    expiryState = "<color=red>expired</color>";

                GUILayout.Label($"Combo: {comboDef.name}  node {selector.CurrentComboNodeIndex}  chain: {chainState}", _labelStyle);
                GUILayout.Label($"Input: {inputState}  |  Expiry: {expiryState}", _labelStyle);
            }
            else
            {
                GUILayout.Label("Combo: <color=grey>none</color>", _labelStyle);
            }
        }

        private void DrawTagSection()
        {
            GUILayout.Space(4f);
            GUILayout.Label("── Attack Tags ──", _headerStyle);

            var context = Target.ActiveContext;
            if (context == null)
            {
                GUILayout.Label("(no active context)", _labelStyle);
                return;
            }

            var tags = context.GetAllTags();
            if (tags.Count == 0)
            {
                GUILayout.Label("(none)", _labelStyle);
                return;
            }

            GUILayout.BeginHorizontal();
            int col = 0;
            foreach (var tag in tags)
            {
                GUILayout.Label($"[{tag}]", _activeTagStyle, GUILayout.Width(100f));
                if (++col % 3 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStatSection()
        {
            GUILayout.Space(4f);
            GUILayout.Label("── Stats ──", _headerStyle);

            var context = Target.ActiveContext;
            if (context?.Stats == null)
            {
                GUILayout.Label("(no active stats)", _labelStyle);
                return;
            }

            StatSheet stats = context.Stats;

            foreach (var stat in new[] { StatId.Damage, StatId.AttackSpeed, StatId.Range, StatId.Knockback })
            {
                string desc = stats.DescribeEvaluation(stat, context);
                GUILayout.Label(desc, _labelStyle);
            }
        }

        private void DrawModifierSection()
        {
            GUILayout.Space(4f);
            GUILayout.Label("── Modifiers ──", _headerStyle);

            bool any = false;
            foreach (var label in Target.Modifiers.GetDebugLabels())
            {
                GUILayout.Label($"• {label}", _labelStyle);
                any = true;
            }

            if (!any) GUILayout.Label("(none)", _labelStyle);
        }

        private void DrawInputBufferSection()
        {
            GUILayout.Space(4f);
            GUILayout.Label("── Input Buffer (last 5) ──", _headerStyle);

            var events = Target.InputBuffer.GetRecentEvents(5);
            if (events.Count == 0)
            {
                GUILayout.Label("(empty)", _labelStyle);
                return;
            }

            foreach (var evt in events)
            {
                float ago = Time.unscaledTime - evt.Timestamp;
                string holdStr = evt.HoldDuration > 0f ? $" hold={evt.HoldDuration:F2}s" : string.Empty;
                GUILayout.Label($"{evt.Button} [{evt.Phase}]{holdStr} ({ago:F2}s ago)", _labelStyle);
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.white },
                fontSize = 11
            };

            _activeTagStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold,
                fontSize = 10
            };
        }
    }
}
