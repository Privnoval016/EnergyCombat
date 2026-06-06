#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Combat;
using DynamicPhysics;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
    /**
     * <summary>
     * Odin Inspector drawer for the <see cref="Tag"/> struct.
     * Renders a text field with a dropdown button listing all predefined constants
     * from <see cref="CombatTag"/> and <see cref="MotionTag"/>.
     *
     * Works automatically for every <c>Tag</c> field in the Inspector — including
     * arrays, <c>HasTagCondition.RequiredTag</c>, <c>ApplyTagPhase.TagsToApply[]</c>, etc.
     * </summary>
     */
    public sealed class TagDrawer : OdinValueDrawer<Tag>
    {
        private static readonly List<(string Label, Tag Value)> _choices = BuildChoices();

        private static List<(string Label, Tag Value)> BuildChoices()
        {
            var list = new List<(string, Tag)>();
            CollectFrom(typeof(CombatTag), "Combat", list);
            CollectFrom(typeof(MotionTag), "Motion", list);
            return list;
        }

        private static void CollectFrom(System.Type type, string prefix, List<(string, Tag)> list)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(Tag)) continue;
                var tag = (Tag)field.GetValue(null);
                list.Add(($"{prefix}/{field.Name}", tag));
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var entry = ValueEntry;
            Tag current = entry.SmartValue;

            EditorGUILayout.BeginHorizontal();

            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            string text = SirenixEditorFields.TextField(current.ToString());
            Tag typed = text;

            float btnWidth = 22f;
            Rect btnRect = GUILayoutUtility.GetRect(btnWidth, EditorGUIUtility.singleLineHeight,
                GUILayout.Width(btnWidth));

            if (GUI.Button(btnRect, EditorGUIUtility.IconContent("d_icon dropdown"), EditorStyles.iconButton))
            {
                var menu = new GenericMenu();
                foreach (var (lbl, val) in _choices)
                {
                    var captured = val;
                    menu.AddItem(new GUIContent(lbl), current == val,
                        () =>
                        {
                            entry.SmartValue = captured;
                            entry.ApplyChanges();
                        });
                }
                menu.ShowAsContext();
            }

            if (!typed.Equals(current))
                entry.SmartValue = typed;

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
