// Place this file anywhere inside an Editor/ folder.
// Open via: Tools → Mirror Animations

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that bulk-mirrors humanoid AnimationClip assets.
/// Swaps left↔right muscle curves, negates lateral centre-body muscles,
/// and corrects root-motion translation/rotation for the flipped side.
/// Non-.anim assets (FBX-embedded clips) are skipped — use the Animations
/// import tab Mirror checkbox for those.
/// </summary>
public class MirrorAnimationTool : EditorWindow
{
    // ── UI state ──────────────────────────────────────────────────────────────

    private DefaultAsset _sourceFolder;
    private bool         _useNewFolder  = true;
    private string       _newFolderName = "Mirrored";

    // ── Cached maps (rebuilt after every domain reload on first run) ──────────

    private static Dictionary<string, string> _mirrorMap; // muscleName → its L↔R counterpart
    private static HashSet<string>            _negateSet; // centre-body lateral muscles (negate value)

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Mirror Animations")]
    public static void ShowWindow() => GetWindow<MirrorAnimationTool>("Mirror Animations");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Mirror Animations", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("Source Folder", "Folder (and all subfolders) to mirror. Only .anim files are processed."),
            _sourceFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        _useNewFolder = EditorGUILayout.Toggle(
            new GUIContent("New Folder", "Off = overwrite source clips in-place."),
            _useNewFolder);

        if (_useNewFolder)
        {
            _newFolderName = EditorGUILayout.TextField(
                new GUIContent("Folder Name", "Created as a sibling of the source folder. Subfolder structure is preserved."),
                _newFolderName);

            EditorGUILayout.HelpBox(
                "Mirrored clips are written to a new sibling folder with the same subfolder structure.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Source clips are OVERWRITTEN with their mirrored versions. Back up first!",
                MessageType.Warning);
        }

        EditorGUILayout.Space(12);

        using (new EditorGUI.DisabledScope(_sourceFolder == null))
        {
            if (GUILayout.Button("Mirror All Animations", GUILayout.Height(32)))
                RunMirror();
        }
    }

    // ── Orchestration ─────────────────────────────────────────────────────────

    private void RunMirror()
    {
        string sourcePath = AssetDatabase.GetAssetPath(_sourceFolder);

        if (!AssetDatabase.IsValidFolder(sourcePath))
        {
            EditorUtility.DisplayDialog("Error", "Selected asset is not a folder.", "OK");
            return;
        }

        if (_useNewFolder && string.IsNullOrWhiteSpace(_newFolderName))
        {
            EditorUtility.DisplayDialog("Error", "New folder name cannot be empty.", "OK");
            return;
        }

        if (!_useNewFolder)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Overwrite Originals?",
                $"All .anim clips inside\n\n  {sourcePath}\n\nwill be replaced with their mirrored versions. Continue?",
                "Yes, Overwrite", "Cancel");
            if (!confirmed) return;
        }

        EnsureMaps();

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { sourcePath });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Nothing Found",
                "No AnimationClip assets found in the selected folder.", "OK");
            return;
        }

        int succeeded = 0, skipped = 0, failed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < guids.Length; i++)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar(
                    "Mirroring Animations",
                    Path.GetFileName(clipPath),
                    (float)i / guids.Length);

                try
                {
                    Result r = ProcessClip(clipPath, sourcePath);
                    if (r == Result.Success)  succeeded++;
                    else                      skipped++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MirrorAnimationTool] Failed on {clipPath}:\n{ex}");
                    failed++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Done",
            $"Finished mirroring.\n\n" +
            $"Succeeded : {succeeded}\n" +
            $"Skipped   : {skipped}  (non-.anim or non-humanoid)\n" +
            $"Failed    : {failed}",
            "OK");
    }

    private enum Result { Success, Skipped }

    private Result ProcessClip(string clipPath, string sourceFolderPath)
    {
        // Only touch standalone .anim files — FBX-embedded clips can't be modified here
        if (!clipPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            return Result.Skipped;

        AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (source == null) return Result.Skipped;

        // Warn (but still process) non-humanoid clips — their curves will pass through unchanged
        if (!source.isHumanMotion)
            Debug.LogWarning($"[MirrorAnimationTool] {clipPath} is not a humanoid clip — curves copied without mirroring.");

        AnimationClip mirrored = MirrorClip(source);

        if (_useNewFolder)
        {
            string destPath = GetNewFolderPath(clipPath, sourceFolderPath);
            EnsureDirectory(Path.GetDirectoryName(destPath).Replace('\\', '/'));

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
            if (existing != null)
                EditorUtility.CopySerialized(mirrored, existing);
            else
                AssetDatabase.CreateAsset(mirrored, destPath);
        }
        else
        {
            // Overwrite in-place: push new data into the existing asset object
            EditorUtility.CopySerialized(mirrored, source);
            EditorUtility.SetDirty(source);
        }

        return Result.Success;
    }

    private string GetNewFolderPath(string clipPath, string sourceFolderPath)
    {
        // Strip source folder prefix to get the relative path, then reparent
        string relative     = clipPath.Substring(sourceFolderPath.Length).TrimStart('/', '\\');
        string sourceParent = Path.GetDirectoryName(sourceFolderPath).Replace('\\', '/');
        return $"{sourceParent}/{_newFolderName}/{relative}".Replace('\\', '/');
    }

    private static void EnsureDirectory(string folderPath)
    {
        string[] parts  = folderPath.Split('/');
        string   current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ── Core Mirror Logic ─────────────────────────────────────────────────────

    /// <summary>
    /// Produces a new AnimationClip with all humanoid muscle curves mirrored:
    /// <list type="bullet">
    ///   <item>Left↔Right muscle pairs are swapped (curve for curve).</item>
    ///   <item>Centre-body "Left-Right" muscles (spine tilt, head turn, etc.) are negated.</item>
    ///   <item>Root translation X is negated; root quaternion Y and Z are negated.</item>
    ///   <item>Object-reference curves and animation events are copied unchanged.</item>
    /// </list>
    /// </summary>
    private static AnimationClip MirrorClip(AnimationClip source)
    {
        var dest = new AnimationClip
        {
            frameRate = source.frameRate,
            wrapMode  = source.wrapMode,
        };

        // Preserve loop, cycle offset, bake settings, etc.
        AnimationUtility.SetAnimationClipSettings(
            dest, AnimationUtility.GetAnimationClipSettings(source));

        // ── Float (muscle + root motion) curves ───────────────────────────────

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);

        // Read everything first so swaps always see the original data
        var srcMap = new Dictionary<string, (EditorCurveBinding b, AnimationCurve curve)>(bindings.Length);
        foreach (var b in bindings)
            srcMap[b.propertyName] = (b, AnimationUtility.GetEditorCurve(source, b));

        var written = new HashSet<string>();

        foreach (var (prop, (b, curve)) in srcMap)
        {
            if (written.Contains(prop)) continue; // already handled by its pair

            if (_mirrorMap.TryGetValue(prop, out string mirrorProp))
            {
                // ── Paired muscle swap ─────────────────────────────────────
                // Write this side's curve to the opposite binding
                var bA = b;
                bA.propertyName = mirrorProp;
                AnimationUtility.SetEditorCurve(dest, bA, curve);
                written.Add(mirrorProp);

                // Write the opposite side's curve (if it exists) to this binding
                if (srcMap.TryGetValue(mirrorProp, out var mirrorEntry))
                {
                    var bB = mirrorEntry.b;
                    bB.propertyName = prop;
                    AnimationUtility.SetEditorCurve(dest, bB, mirrorEntry.curve);
                }
                // If the counterpart has no curve the dest muscle stays at its rest value — correct.

                written.Add(prop);
            }
            else if (_negateSet.Contains(prop) || IsNegatedRootCurve(prop))
            {
                // ── Negate: centre-body lateral or root motion ─────────────
                AnimationUtility.SetEditorCurve(dest, b, NegateCurve(curve));
                written.Add(prop);
            }
            else
            {
                // ── Pass through: non-lateral body muscles, generic curves ──
                AnimationUtility.SetEditorCurve(dest, b, curve);
                written.Add(prop);
            }
        }

        // ── Object-reference curves (materials, object toggles) ──────────────
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(source))
            AnimationUtility.SetObjectReferenceCurve(
                dest, b, AnimationUtility.GetObjectReferenceCurve(source, b));

        // ── Animation events ──────────────────────────────────────────────────
        AnimationUtility.SetAnimationEvents(
            dest, AnimationUtility.GetAnimationEvents(source));

        return dest;
    }

    /// <summary>
    /// Returns true for root-motion curve components that must be negated when
    /// mirroring across the character's sagittal plane (left↔right flip):
    /// <list type="bullet">
    ///   <item>RootT x — lateral position.</item>
    ///   <item>RootQ y, RootQ z — the quaternion components that encode yaw/roll.</item>
    /// </list>
    /// RootT y/z and RootQ x/w are symmetric and are left unchanged.
    /// </summary>
    private static bool IsNegatedRootCurve(string p) =>
        p is "RootT x" or "RootQ y" or "RootQ z";

    /// <summary>
    /// Returns a new AnimationCurve with every keyframe value and tangent negated.
    /// Weighted-mode flags and wrap modes are preserved.
    /// </summary>
    private static AnimationCurve NegateCurve(AnimationCurve src)
    {
        Keyframe[] srcKeys = src.keys;
        Keyframe[] dstKeys = new Keyframe[srcKeys.Length];

        for (int i = 0; i < srcKeys.Length; i++)
        {
            dstKeys[i] = new Keyframe(
                srcKeys[i].time,
                -srcKeys[i].value,
                -srcKeys[i].inTangent,
                -srcKeys[i].outTangent,
                srcKeys[i].inWeight,
                srcKeys[i].outWeight)
            {
                weightedMode = srcKeys[i].weightedMode
            };
        }

        var result = new AnimationCurve(dstKeys)
        {
            preWrapMode  = src.preWrapMode,
            postWrapMode = src.postWrapMode,
        };

        // Restore per-key tangent modes (auto, clamped, broken, etc.)
        for (int i = 0; i < srcKeys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                result, i, AnimationUtility.GetKeyLeftTangentMode(src, i));
            AnimationUtility.SetKeyRightTangentMode(
                result, i, AnimationUtility.GetKeyRightTangentMode(src, i));
        }

        return result;
    }

    // ── Muscle Map Construction ───────────────────────────────────────────────

    private static void EnsureMaps()
    {
        if (_mirrorMap != null) return;
        _mirrorMap = BuildMirrorMap();
        _negateSet = BuildNegateSet(_mirrorMap);
    }

    /// <summary>
    /// Builds a bidirectional map of muscle names to their left↔right counterparts
    /// by substituting "Left"↔"Right" in each name and verifying the result exists
    /// in the HumanTrait muscle list.
    /// </summary>
    private static Dictionary<string, string> BuildMirrorMap()
    {
        string[] names   = HumanTrait.MuscleName;
        var      nameSet = new HashSet<string>(names);
        var      map     = new Dictionary<string, string>(names.Length);

        foreach (string name in names)
        {
            if (!name.Contains("Left") && !name.Contains("Right")) continue;

            // Swap "Left" ↔ "Right" via a placeholder that can't appear in a muscle name
            const string placeholder = "\x01";
            string mirrored = name
                .Replace("Left",  placeholder)
                .Replace("Right", "Left")
                .Replace(placeholder, "Right");

            if (nameSet.Contains(mirrored))
                map[name] = mirrored;
        }

        return map;
    }

    /// <summary>
    /// Builds the set of centre-body muscle names whose values must be negated
    /// when mirroring. These are muscles with no left/right pair (spine, neck,
    /// head, jaw) that describe lateral movement ("Left-Right" in their name).
    /// </summary>
    private static HashSet<string> BuildNegateSet(Dictionary<string, string> mirrorMap)
    {
        var set = new HashSet<string>();
        foreach (string name in HumanTrait.MuscleName)
        {
            // Paired muscles are handled by the swap — skip them
            if (mirrorMap.ContainsKey(name)) continue;
            // Only lateral muscles need negating; front-back and up-down are symmetric
            if (name.Contains("Left-Right"))
                set.Add(name);
        }
        return set;
    }
}
