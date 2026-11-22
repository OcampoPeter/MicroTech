#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Maze Tasks:
///   Create Task Layers              - adds the Bent / Fried / Missing / Part layers to the project
///   Add Controller To Selected Maze - adds MazeTaskController to the selected maze root
/// </summary>
public static class MazeTaskMenu
{
    static readonly string[] LayerNames = { "Bent", "Fried", "Missing", "Part" };

    [MenuItem("Tools/Maze Tasks/Create Task Layers")]
    static void CreateLayers()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("Couldn't open ProjectSettings/TagManager.asset");
            return;
        }

        var tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        string added = "", skipped = "";

        foreach (string layerName in LayerNames)
        {
            bool exists = false;
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) { exists = true; break; }

            if (exists) { skipped += layerName + " "; continue; }

            bool placed = false;
            for (int i = 8; i < layers.arraySize; i++) // 0-7 are built-in
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    added += layerName + " ";
                    placed = true;
                    break;
                }
            }
            if (!placed) Debug.LogError($"No free layer slot for '{layerName}'. Free one up in Project Settings > Tags and Layers.");
        }

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"Maze Tasks layers - added: [{added.Trim()}]  already existed: [{skipped.Trim()}]");
    }

    [MenuItem("Tools/Maze Tasks/Add Controller To Selected Maze")]
    static void AddController()
    {
        GameObject maze = Selection.activeGameObject;
        if (maze == null) return;

        if (maze.GetComponent<MazeTaskController>() != null)
        {
            Debug.Log(maze.name + " already has a MazeTaskController.");
            return;
        }

        Undo.AddComponent<MazeTaskController>(maze);
        Debug.Log("Added MazeTaskController to " + maze.name +
                  ". Now set platform Layers to Bent / Fried / Missing / Part.");
    }

    [MenuItem("Tools/Maze Tasks/Add Controller To Selected Maze", true)]
    static bool AddControllerValidate() => Selection.activeGameObject != null;
}
#endif