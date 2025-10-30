using UnityEngine;

using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;

public class ScriptableObjectManager : EditorWindow
{
    protected static string selectedFolderPath = "Assets";
    protected static List<Object> foundAssets = new List<Object>();
    protected Vector2 scrollPosition;
    protected Object selectedAssetToEdit;
    protected Editor editor;

    [MenuItem("Window/Custom Windows/MetaData Manager")]
    public static void ShowWindow()
    {
        GetWindow<ScriptableObjectManager>("Scriptable Object Manager");
    }
    protected virtual void OnGUI()
    {
        GUILayout.Label("Select Folder:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        selectedFolderPath = EditorGUILayout.TextField(selectedFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string newPath = EditorUtility.OpenFolderPanel("Select Folder", selectedFolderPath, "");
            if (!string.IsNullOrEmpty(newPath))
            {
                if (newPath.StartsWith(Application.dataPath))
                {
                    selectedFolderPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogError("Selected folder is outside the project's Assets folder.");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Find Assets", GUILayout.Height(30), GUILayout.Width(150)))
        {
            foundAssets.Clear();
            foundAssets = EditorWindowTools.FindAllAssetsInFolder(selectedFolderPath);
        }

        EditorGUILayout.BeginHorizontal();

        // Left side: List of found assets as buttons with highlighting and spacing
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
        GUILayout.Label("Found Assets:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (Object asset in foundAssets)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleLeft;

            if (asset == selectedAssetToEdit)
            {
                GUIStyle selectedButtonStyle = new GUIStyle(buttonStyle);
                selectedButtonStyle.normal.background = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IN Title").normal.background;
                selectedButtonStyle.normal.textColor = Color.white;
                selectedButtonStyle.fontStyle = FontStyle.Bold;

                if (GUILayout.Button(asset.name, selectedButtonStyle))
                {
                    selectedAssetToEdit = asset;
                    EditorWindowTools.CleanupAndCreateEditor(ref editor, ref selectedAssetToEdit);
                }
            }
            else
            {
                if (GUILayout.Button(asset.name, buttonStyle))
                {
                    selectedAssetToEdit = asset;
                    EditorWindowTools.CleanupAndCreateEditor(ref editor, ref selectedAssetToEdit);
                }
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // Vertical divider
        GUILayout.Box("", GUILayout.Height(position.height), GUILayout.Width(2));

        // Right side: Editor for the selected asset
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
        GUILayout.Label("Asset Details:", EditorStyles.boldLabel);
        if (selectedAssetToEdit != null)
        {
            if (editor != null)
            {
                EditorGUILayout.BeginHorizontal(); // Add a horizontal group
                EditorGUILayout.BeginVertical(); // Content of the inspector
                editor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
                //GUILayout.FlexibleSpace(); // Push content to the left
                GUILayout.Space(25); // Add some right-side spacing
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Select an asset from the list to view its details.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No asset selected.", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
        GUILayout.Box("", GUILayout.Height(position.height), GUILayout.Width(2));
        EditorGUILayout.EndHorizontal();
    }

    protected virtual void OnDestroy()
    {
        if (editor != null)
        {
            DestroyImmediate(editor);
            editor = null;
        }
    }
}

public static class EditorWindowTools
{
    public static void CleanupAndCreateEditor(ref Editor editor, ref Object selectedAssetToEdit)
    {
        if (editor != null)
        {
            Editor.DestroyImmediate(editor);
            editor = null;
        }
        if (selectedAssetToEdit != null)
        {
            editor = Editor.CreateEditor(selectedAssetToEdit);
        }
    }

    public static List<Object> FindAllAssetsInFolder(string selectedFolderPath)
    {
        List<Object> foundAssets = new();

        // Get all assets in the selected folder (non-recursive)
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { selectedFolderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // Check if the asset is directly under the selected folder
            if (Path.GetDirectoryName(assetPath).Replace('\\', '/') == selectedFolderPath.Replace('\\', '/'))
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                {
                    foundAssets.Add(asset);
                }
            }
        }

        if (foundAssets.Count == 0)
        {
            Debug.Log($"No ScriptableObjects found directly in {selectedFolderPath}.");
        }
        else
        {
            Debug.Log($"Found {foundAssets.Count} ScriptableObjects directly in {selectedFolderPath}.");
        }
        return foundAssets;
    }
}
#endif
