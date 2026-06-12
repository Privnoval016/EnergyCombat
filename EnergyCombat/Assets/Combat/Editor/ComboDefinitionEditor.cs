#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Combat;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
    /**
     * <summary>
     * Custom Inspector for <see cref="ComboDefinition"/> assets.
     * Replaces the default flat list with:
     * <list type="bullet">
     *   <item>A scrollable flow-graph visualising the BFS tree with colour-coded arrows.</item>
     *   <item>Collapsible node panels whose transition dropdowns resolve indices to ability names.</item>
     * </list>
     * Node 0 is treated as the root; clicking a node in the graph highlights its panel below.
     * </summary>
     */
    [CustomEditor(typeof(ComboDefinition))]
    public class ComboDefinitionEditor : Editor
    {
        // ── Layout constants ──────────────────────────────────────────────────

        private const float NodeW    = 164f;
        private const float NodeH    = 54f;
        private const float LevelGap = 84f;  // vertical space between BFS depth rows
        private const float NodeGapX = 24f;  // horizontal gap between sibling nodes
        private const float GraphPad = 22f;
        private const float GraphH   = 270f; // max visible graph height before scrolling

        // ── Colours ───────────────────────────────────────────────────────────

        private static readonly Color ColLight  = new(0.30f, 0.65f, 1.00f);
        private static readonly Color ColHeavy  = new(1.00f, 0.38f, 0.38f);
        private static readonly Color ColHold   = new(1.00f, 0.72f, 0.12f);
        private static readonly Color ColEnd    = new(0.45f, 0.45f, 0.50f);
        private static readonly Color NodeNorm  = new(0.20f, 0.20f, 0.23f, 1f);
        private static readonly Color NodeSel   = new(0.16f, 0.36f, 0.58f, 1f);
        private static readonly Color NodeWarn  = new(0.48f, 0.28f, 0.08f, 1f);
        private static readonly Color NodeBrd   = new(0.32f, 0.32f, 0.38f, 1f);
        private static readonly Color GraphBg   = new(0.12f, 0.12f, 0.14f, 1f);

        // ── State ─────────────────────────────────────────────────────────────

        private int     _selectedNode = -1;
        private Vector2 _graphScroll;
        private readonly Dictionary<int, bool> _foldouts = new();
        private Dictionary<int, Rect> _nodeRects = new();

        // ── Serialized props ──────────────────────────────────────────────────

        private SerializedProperty _maxInterruptsProp;

        private void OnEnable() =>
            _maxInterruptsProp = serializedObject.FindProperty("MaxConcurrentInterrupts");

        // ── Inspector entry ───────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var def = (ComboDefinition)target;
            def.Nodes ??= new List<ComboNode>();

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_maxInterruptsProp,
                new GUIContent("Max Concurrent Interrupts",
                    "0 = unlimited — only the combo window timer resets the combo."));

            EditorGUILayout.Space(12);

            // ── Graph ─────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Combo Flow", EditorStyles.boldLabel);
            if (def.Nodes.Count == 0)
                EditorGUILayout.HelpBox("Add a node below to start building the combo.", MessageType.Info);
            else
                DrawGraph(def);

            EditorGUILayout.Space(12);

            // ── Node panels ───────────────────────────────────────────────────
            EditorGUILayout.LabelField($"Nodes  ({def.Nodes.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            int removeAt = -1;
            for (int i = 0; i < def.Nodes.Count; i++)
            {
                if (DrawNodePanel(def, i) == PanelResult.Remove)
                    removeAt = i;
            }
            if (removeAt >= 0)
            {
                Undo.RecordObject(target, "Remove Combo Node");
                def.Nodes.RemoveAt(removeAt);
                if (_selectedNode >= def.Nodes.Count)
                    _selectedNode = def.Nodes.Count - 1;
                EditorUtility.SetDirty(target);
            }

            // ── Add Node ──────────────────────────────────────────────────────
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("  ＋  Add Node  ", GUILayout.Height(24)))
                {
                    Undo.RecordObject(target, "Add Combo Node");
                    def.Nodes.Add(new ComboNode { ComboWindowDuration = 0f });
                    _selectedNode = def.Nodes.Count - 1;
                    _foldouts[_selectedNode] = true;
                    EditorUtility.SetDirty(target);
                }
                GUILayout.FlexibleSpace();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ── Graph ─────────────────────────────────────────────────────────────

        private void DrawGraph(ComboDefinition def)
        {
            _nodeRects = ComputeLayout(def);

            float canvasW = _nodeRects.Values.Max(r => r.xMax) + GraphPad;
            float canvasH = _nodeRects.Values.Max(r => r.yMax) + GraphPad;
            float viewH   = Mathf.Min(canvasH, GraphH);

            Rect viewRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none, GUILayout.Height(viewH));
            viewRect = EditorGUI.IndentedRect(viewRect);

            // Background
            EditorGUI.DrawRect(viewRect, GraphBg);

            _graphScroll = GUI.BeginScrollView(viewRect, _graphScroll,
                new Rect(0, 0, canvasW, canvasH));

            // Arrows drawn behind nodes
            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                DrawArrows(def);
                Handles.EndGUI();
            }

            // Node boxes + click handling
            foreach (var (idx, rect) in _nodeRects)
                DrawGraphNode(def, idx, rect);

            GUI.EndScrollView();
        }

        private void DrawArrows(ComboDefinition def)
        {
            foreach (var (srcIdx, srcRect) in _nodeRects)
            {
                var node = def.GetNode(srcIdx);
                if (node?.Transitions == null) continue;

                // Offset parallel arrows that share the same target
                var groups = node.Transitions
                    .GroupBy(t => t.TargetNodeIndex)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var (tgtIdx, batch) in groups)
                {
                    for (int ti = 0; ti < batch.Count; ti++)
                    {
                        var t     = batch[ti];
                        Color col = ArrowColor(t);
                        float ox  = (ti - (batch.Count - 1) * 0.5f) * 14f;

                        if (tgtIdx < 0) // terminal — small downward stub
                        {
                            Vector2 s = new(srcRect.center.x + ox, srcRect.yMax);
                            Vector2 e = s + new Vector2(0, 28f);
                            Handles.color = ColEnd;
                            Handles.DrawLine(s, e);
                            DrawArrowHead(e, Vector2.up, ColEnd); // (0,1) is the arrow's travel direction in GUI coords
                            DrawLabel(s + new Vector2(0, 6f), ArrowLabel(t), ColEnd);
                        }
                        else if (_nodeRects.TryGetValue(tgtIdx, out Rect tgtRect))
                        {
                            Vector2 s  = new(srcRect.center.x + ox, srcRect.yMax);
                            Vector2 e  = new(tgtRect.center.x + ox, tgtRect.yMin);
                            Vector2 c1 = s + new Vector2(0, LevelGap * 0.42f);
                            Vector2 c2 = e - new Vector2(0, LevelGap * 0.42f);

                            Handles.color = col;
                            Handles.DrawBezier(s, e, c1, c2, col, null, 2.5f);
                            DrawArrowHead(e, (e - c2).normalized, col);
                            DrawLabel(Bezier(s, c1, c2, e, 0.5f), ArrowLabel(t), col);
                        }
                        else // target index out of range
                        {
                            Vector2 s = new(srcRect.center.x, srcRect.yMax);
                            Vector2 e = s + new Vector2(0, 18f);
                            Handles.color = Color.red;
                            Handles.DrawLine(s, e);
                        }
                    }
                }
            }
        }

        private static void DrawArrowHead(Vector2 tip, Vector2 dir, Color col)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            dir = dir.normalized;
            Vector2 l = tip - dir * 10f + new Vector2(-dir.y,  dir.x) * 5f;
            Vector2 r = tip - dir * 10f - new Vector2(-dir.y,  dir.x) * 5f;
            Handles.color = col;
            Handles.DrawLine(tip, l);
            Handles.DrawLine(tip, r);
        }

        private static void DrawLabel(Vector2 centre, string text, Color col)
        {
            var s = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 9,
                normal    = { textColor = col },
            };
            GUI.Label(new Rect(centre.x - 28f, centre.y - 10f, 56f, 20f), text, s);
        }

        private void DrawGraphNode(ComboDefinition def, int idx, Rect rect)
        {
            var  node  = def.GetNode(idx);
            bool sel   = idx == _selectedNode;
            bool noAb  = node?.Ability == null;

            Color bg  = noAb ? NodeWarn : (sel ? NodeSel : NodeNorm);
            EditorGUI.DrawRect(rect, bg);
            DrawBorder(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2),
                sel ? new Color(0.4f, 0.7f, 1f) : NodeBrd, 1);

            // Index badge
            Rect badge = new(rect.x + 4, rect.y + 5, 20, 15);
            EditorGUI.DrawRect(badge, new Color(0, 0, 0, 0.45f));
            GUI.Label(badge, idx.ToString(),
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 9 });

            // Ability name
            string top = node?.Ability != null ? node.Ability.name : "⚠ No Ability";
            GUI.Label(new Rect(rect.x, rect.y + 7, rect.width, 22), top,
                new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize  = 10,
                    normal    = { textColor = noAb ? new Color(1f, 0.6f, 0.2f) : Color.white },
                    wordWrap  = false,
                });

            // Sub-label: cancel / window timing + transition count
            int tCount = node?.Transitions?.Length ?? 0;
            string sub = node != null
                ? $"Expiry {(node.ComboWindowDuration > 0f ? $"{node.ComboWindowDuration:0.0}s" : "none")} · {tCount} out"
                : "";
            GUI.Label(new Rect(rect.x, rect.y + 30, rect.width, 16), sub,
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 8 });

            // Click → select + expand panel
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedNode = idx;
                _foldouts[idx] = true;
                Event.current.Use();
                Repaint();
            }
        }

        // ── BFS layout ────────────────────────────────────────────────────────

        private Dictionary<int, Rect> ComputeLayout(ComboDefinition def)
        {
            var depth   = new Dictionary<int, int>();
            var visited = new HashSet<int>();
            var queue   = new Queue<int>();

            if (def.Nodes.Count > 0) { depth[0] = 0; queue.Enqueue(0); }

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                if (visited.Contains(idx)) continue;
                visited.Add(idx);

                var node = def.GetNode(idx);
                if (node?.Transitions == null) continue;
                foreach (var t in node.Transitions)
                {
                    if (t.TargetNodeIndex >= 0 && t.TargetNodeIndex < def.Nodes.Count
                        && !depth.ContainsKey(t.TargetNodeIndex))
                    {
                        depth[t.TargetNodeIndex] = depth[idx] + 1;
                        queue.Enqueue(t.TargetNodeIndex);
                    }
                }
            }

            // Orphaned nodes get their own row below the BFS tree
            int orphanRow = depth.Count > 0 ? depth.Values.Max() + 1 : 0;
            for (int i = 0; i < def.Nodes.Count; i++)
            {
                if (!visited.Contains(i))
                    depth[i] = orphanRow;
            }

            // Group and sort each level
            var levels = new SortedDictionary<int, List<int>>();
            foreach (var (idx, d) in depth)
            {
                if (!levels.ContainsKey(d)) levels[d] = new();
                levels[d].Add(idx);
            }
            foreach (var l in levels.Values) l.Sort();

            // Centred horizontal placement
            float maxLvlW = levels.Values.Max(l => l.Count * (NodeW + NodeGapX) - NodeGapX);

            var rects = new Dictionary<int, Rect>();
            foreach (var (d, nodes) in levels)
            {
                float lvlW   = nodes.Count * (NodeW + NodeGapX) - NodeGapX;
                float startX = GraphPad + (maxLvlW - lvlW) * 0.5f;
                float y      = GraphPad + d * (NodeH + LevelGap);
                for (int i = 0; i < nodes.Count; i++)
                    rects[nodes[i]] = new Rect(startX + i * (NodeW + NodeGapX), y, NodeW, NodeH);
            }
            return rects;
        }

        // ── Node detail panels ────────────────────────────────────────────────

        private enum PanelResult { Keep, Remove }

        /** <summary>Draws one collapsible node panel. Returns <c>Remove</c> if the delete button was clicked.</summary> */
        private PanelResult DrawNodePanel(ComboDefinition def, int idx)
        {
            var node = def.GetNode(idx);
            if (node == null) return PanelResult.Keep;

            if (!_foldouts.ContainsKey(idx)) _foldouts[idx] = idx == 0;

            bool sel = idx == _selectedNode;

            // ── Header row ────────────────────────────────────────────────────
            using (new EditorGUILayout.HorizontalScope())
            {
                string abilityName = node.Ability != null ? node.Ability.name : "⚠ No Ability";
                string header      = $"Node {idx}  —  {abilityName}";

                var foldStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = sel ? new Color(0.5f, 0.85f, 1f) : Color.white },
                    onNormal  = { textColor = sel ? new Color(0.5f, 0.85f, 1f) : Color.white },
                };

                _foldouts[idx] = EditorGUILayout.Foldout(_foldouts[idx], header, true, foldStyle);
                GUILayout.FlexibleSpace();

                // Ping in graph
                if (GUILayout.Button(new GUIContent("◎", "Select in flow graph"),
                    GUILayout.Width(22), GUILayout.Height(18)))
                {
                    _selectedNode = idx;
                    Repaint();
                }

                // Delete
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.75f, 0.28f, 0.28f);
                bool wantsDelete = GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18));
                GUI.backgroundColor = prev;

                if (wantsDelete)
                {
                    if (EditorUtility.DisplayDialog("Remove Node",
                            $"Remove Node {idx} ({abilityName})?\n\nAny transitions pointing to it will reference an invalid index.",
                            "Remove", "Cancel"))
                        return PanelResult.Remove;
                }
            }

            if (!_foldouts[idx]) { EditorGUILayout.Space(2); return PanelResult.Keep; }

            EditorGUI.indentLevel++;

            // ── Ability ───────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            var newAbility = (AbilityDefinition)EditorGUILayout.ObjectField(
                new GUIContent("Ability", "The ability that executes when this node is entered."),
                node.Ability, typeof(AbilityDefinition), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Set Combo Node Ability");
                node.Ability = newAbility;
                EditorUtility.SetDirty(target);
            }

            // ── Combo expiry ──────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            float newWindow = EditorGUILayout.FloatField(
                new GUIContent("Combo Window",
                    "Seconds the player has to input the next transition before the combo resets."),
                node.ComboWindowDuration);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Set Combo Window Duration");
                node.ComboWindowDuration = Mathf.Max(0f, newWindow);
                EditorUtility.SetDirty(target);
            }

            // ── Transitions ───────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);

            if (node.Transitions == null || node.Transitions.Length == 0)
                EditorGUILayout.HelpBox("Terminal node — no follow-up transitions.", MessageType.None);

            int removeTransAt = -1;
            for (int ti = 0; ti < (node.Transitions?.Length ?? 0); ti++)
            {
                if (DrawTransition(def, node, ti))
                    removeTransAt = ti;
                EditorGUILayout.Space(3);
            }

            if (removeTransAt >= 0)
            {
                Undo.RecordObject(target, "Remove Combo Transition");
                var list = new List<ComboTransition>(node.Transitions!);
                list.RemoveAt(removeTransAt);
                node.Transitions = list.ToArray();
                EditorUtility.SetDirty(target);
            }

            // Add transition button
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                if (GUILayout.Button("＋ Add Transition"))
                {
                    Undo.RecordObject(target, "Add Combo Transition");
                    var list = new List<ComboTransition>(node.Transitions ?? Array.Empty<ComboTransition>());
                    list.Add(new ComboTransition { TargetNodeIndex = -1 });
                    node.Transitions = list.ToArray();
                    EditorUtility.SetDirty(target);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);

            return PanelResult.Keep;
        }

        /** <summary>Draws a single transition card. Returns <c>true</c> if the remove button was clicked.</summary> */
        private bool DrawTransition(ComboDefinition def, ComboNode node, int ti)
        {
            var   t      = node.Transitions![ti];
            Color stripe = ArrowColor(t);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // ── Header ────────────────────────────────────────────────────
                bool remove;
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Coloured dot indicates the input type at a glance
                    GUI.color = stripe;
                    GUILayout.Label("●", GUILayout.Width(16));
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField($"Transition {ti}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.75f, 0.28f, 0.28f);
                    remove = GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18));
                    GUI.backgroundColor = prev;
                }
                if (remove) return true;

                EditorGUI.indentLevel++;

                // ── Button ────────────────────────────────────────────────────
                EditorGUI.BeginChangeCheck();
                var newBtn = (CombatInputButton)EditorGUILayout.EnumPopup(
                    new GUIContent("Button"), t.Button);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Set Transition Button");
                    t.Button = newBtn;
                    EditorUtility.SetDirty(target);
                }

                // ── Hold ──────────────────────────────────────────────────────
                EditorGUI.BeginChangeCheck();
                bool newHold = EditorGUILayout.Toggle(
                    new GUIContent("Require Hold",
                        "Button must be held past the ability's hold threshold (not just tapped)."),
                    t.RequireHold);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Set Require Hold");
                    t.RequireHold = newHold;
                    EditorUtility.SetDirty(target);
                }

                // ── Pause range ───────────────────────────────────────────────
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(new GUIContent("Pause Range",
                        "Min / Max seconds between the previous input release and this one. 0 = no constraint."));
                    EditorGUI.BeginChangeCheck();
                    float newMin = EditorGUILayout.FloatField(t.MinPauseDuration, GUILayout.Width(46));
                    EditorGUILayout.LabelField("–", GUILayout.Width(10));
                    float newMax = EditorGUILayout.FloatField(t.MaxPauseDuration, GUILayout.Width(46));
                    EditorGUILayout.LabelField("s", GUILayout.Width(14));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Set Pause Range");
                        t.MinPauseDuration = Mathf.Max(0f, newMin);
                        t.MaxPauseDuration = Mathf.Max(0f, newMax);
                        EditorUtility.SetDirty(target);
                    }
                }

                // ── Target node (popup with ability names) ────────────────────
                int nodeCount = def.Nodes?.Count ?? 0;
                var opts      = new string[nodeCount + 1];
                opts[0] = "(end combo)";
                for (int i = 0; i < nodeCount; i++)
                {
                    var n = def.GetNode(i);
                    string aName = n?.Ability != null ? n.Ability.name : "⚠ No Ability";
                    opts[i + 1] = $"Node {i}  —  {aName}";
                }

                int curSel  = t.TargetNodeIndex < 0 ? 0 : Mathf.Clamp(t.TargetNodeIndex + 1, 0, opts.Length - 1);
                EditorGUI.BeginChangeCheck();
                int newSel  = EditorGUILayout.Popup(new GUIContent("Target Node"), curSel, opts);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Set Target Node");
                    t.TargetNodeIndex = newSel == 0 ? -1 : newSel - 1;
                    EditorUtility.SetDirty(target);
                }

                // Out-of-range warning
                if (t.TargetNodeIndex >= nodeCount)
                    EditorGUILayout.HelpBox(
                        $"Index {t.TargetNodeIndex} is out of range — only {nodeCount} node(s) exist.",
                        MessageType.Error);

                EditorGUI.indentLevel--;
            }

            return false;
        }

        // ── Utility helpers ───────────────────────────────────────────────────

        private static Color ArrowColor(ComboTransition t)
        {
            if (t.TargetNodeIndex < 0) return ColEnd;
            if (t.RequireHold) return ColHold;
            return t.Button switch
            {
                CombatInputButton.LightAttack => ColLight,
                CombatInputButton.HeavyAttack => ColHeavy,
                _                             => new Color(0.7f, 0.7f, 0.7f),
            };
        }

        private static string ArrowLabel(ComboTransition t)
        {
            string btn = t.Button switch
            {
                CombatInputButton.LightAttack => "L",
                CombatInputButton.HeavyAttack => "H",
                _                             => "?",
            };
            return t.RequireHold ? btn + " ●" : btn;
        }

        private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        private static void DrawBorder(Rect r, Color col, float w)
        {
            EditorGUI.DrawRect(new Rect(r.x,          r.y,          r.width,  w), col);
            EditorGUI.DrawRect(new Rect(r.x,          r.yMax - w,   r.width,  w), col);
            EditorGUI.DrawRect(new Rect(r.x,          r.y,          w,        r.height), col);
            EditorGUI.DrawRect(new Rect(r.xMax - w,   r.y,          w,        r.height), col);
        }
    }
}
#endif
