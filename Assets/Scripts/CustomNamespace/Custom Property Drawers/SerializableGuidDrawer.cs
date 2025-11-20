#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
[CustomPropertyDrawer(typeof(SerializableGuid))]
public class SerializableGuidDrawer : PropertyDrawer {
    static readonly string[] s_GuidParts = { "Part1", "Part2", "Part3", "Part4" };
    
    static SerializedProperty[] GetGuidParts(SerializedProperty property) {
        SerializedProperty[] values = new SerializedProperty[s_GuidParts.Length];
        for (int i = 0; i < s_GuidParts.Length; i++) {
            values[i] = property.FindPropertyRelative(s_GuidParts[i]);
        }
        return values;
    }
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property) {
        VisualElement container = new()
        {
            style =
            {
                flexDirection = FlexDirection.Row
            }
        };

        Label label = new(property.displayName)
        {
            style =
            {
                minWidth = 120,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };
        container.Add(label);
        
        Label guidLabel = new()
        {
            style =
            {
                flexGrow = 1,
                unityTextAlign = TextAnchor.MiddleLeft,
                paddingLeft = 2,
                paddingRight = 2
            }
        };

        UpdateGuidDisplay();
        
        // Update display when the property changes (e.g., undo/redo)
        container.TrackPropertyValue(property, _ => UpdateGuidDisplay());
        
        // Context menu
        guidLabel.AddManipulator(new ContextualMenuManipulator(evt => {
            evt.menu.AppendAction("Copy GUID", _ => CopyGuid(property));
            evt.menu.AppendAction("Reset GUID", _ => ResetGuid(property, UpdateGuidDisplay));
            evt.menu.AppendAction("Regenerate GUID", _ => RegenerateGuid(property, UpdateGuidDisplay));
        }));
        
        container.Add(guidLabel);
        
        return container;

        // Update GUID display
        void UpdateGuidDisplay()
        {
            SerializedProperty[] guidParts = GetGuidParts(property);
            guidLabel.text = guidParts.All(x => x != null) ? BuildGuidString(guidParts) : "GUID Not Initialized";
        }
    }

    static void CopyGuid(SerializedProperty property) {
        SerializedProperty[] guidParts = GetGuidParts(property);
        if (guidParts.Any(x => x == null)) return;
        
        string guid = BuildGuidString(guidParts);
        EditorGUIUtility.systemCopyBuffer = guid;
        Debug.Log($"GUID copied to clipboard: {guid}");
    }

    static void ResetGuid(SerializedProperty property, Action updateCallback) {
        const string warning = "Are you sure you want to reset the GUID?";
        if (!EditorUtility.DisplayDialog("Reset GUID", warning, "Yes", "No")) return;
        
        SerializedProperty[] guidParts = GetGuidParts(property);
        foreach (SerializedProperty part in guidParts) {
            part.uintValue = 0;
        }
        property.serializedObject.ApplyModifiedProperties();
        updateCallback?.Invoke();
        Debug.Log("GUID has been reset.");
    }

    static void RegenerateGuid(SerializedProperty property, Action updateCallback) {
        const string warning = "Are you sure you want to regenerate the GUID?";
        if (!EditorUtility.DisplayDialog("Regenerate GUID", warning, "Yes", "No")) return;
        
        byte[] bytes = Guid.NewGuid().ToByteArray();
        SerializedProperty[] guidParts = GetGuidParts(property);
        
        for (int i = 0; i < s_GuidParts.Length; i++) {
            guidParts[i].uintValue = BitConverter.ToUInt32(bytes, i * 4);
        }
        property.serializedObject.ApplyModifiedProperties();
        updateCallback?.Invoke();
        Debug.Log("GUID has been regenerated.");
    }
    
    static string BuildGuidString(SerializedProperty[] guidParts) {
        return new StringBuilder()
            .AppendFormat("{0:X8}", guidParts[0].uintValue)
            .AppendFormat("{0:X8}", guidParts[1].uintValue)
            .AppendFormat("{0:X8}", guidParts[2].uintValue)
            .AppendFormat("{0:X8}", guidParts[3].uintValue)
            .ToString();
    }
}
#endif