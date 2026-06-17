using BladeMode.Debug;
using UnityEditor;
using UnityEngine;

namespace BladeMode.Editor
{
    [CustomEditor(typeof(SliceDebugger))]
    public sealed class SliceDebuggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6);

            GUI.backgroundColor = new Color(0.4f, 0.9f, 1f);
            if (GUILayout.Button("▶  Execute Cut", GUILayout.Height(30)))
                ((SliceDebugger)target).ExecuteCut();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.HelpBox(
                "PlaneTransform: drag any GameObject (e.g. a Unity Plane). " +
                "Its position = cut point; its Up axis = plane normal (which side stays alive).\n" +
                "Physics and shader creep only run in Play Mode.",
                MessageType.Info);
        }
    }
}
