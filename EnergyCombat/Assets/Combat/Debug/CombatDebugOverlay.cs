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

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(ToggleKey))
                ShowOverlay = !ShowOverlay;
        }

        private void OnGUI()
        {
            if (!ShowOverlay || Target == null) return;

            InitStyles();

            GUILayout.BeginArea(new Rect(WindowPosition.x, WindowPosition.y, WindowWidth, Screen.height - WindowPosition.y - 10f));
            GUILayout.BeginVertical(GUI.skin.box);

            DrawHeader("COMBAT SYSTEM");

            DrawExecutionSection();
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
