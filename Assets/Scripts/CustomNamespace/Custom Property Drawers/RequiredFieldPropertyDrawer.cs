#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomPropertyDrawer(typeof(RequiredFieldAttribute))]
public class RequiredFieldDrawer : PropertyDrawer {
    [SerializeField] Texture2D m_requiredIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scripts/CustomNamespace/Custom Property Drawers/Drawer Assets/RequiredFieldIcon.png");
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property) {
        VisualElement container = new()
        {
            style =
            {
                flexDirection = FlexDirection.Row
            }
        };

        // Create the property field
        PropertyField propertyField = new(property)
        {
            style =
            {
                flexGrow = 1
            }
        };
        container.Add(propertyField);
        
        // Create the icon
        Image icon = new()
        {
            image = m_requiredIcon,
            tooltip = "This field is required and is either missing or empty!",
            style =
            {
                width = 16,
                height = 16,
                marginLeft = 2
            }
        };
        container.Add(icon);

        // Initial check
        UpdateIconVisibility(property);
        
        // Track property changes (includes undo/redo)
        container.TrackPropertyValue(property, UpdateIconVisibility);
        
        // Also repaint hierarchy on changes
        propertyField.RegisterValueChangeCallback(_ => {
            EditorApplication.RepaintHierarchyWindow();
        });
        
        return container;

        // Update icon visibility based on field value
        void UpdateIconVisibility(SerializedProperty prop) {
            icon.style.display = IsFieldUnassigned(prop) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    static bool IsFieldUnassigned(SerializedProperty property)
    {
        return property.propertyType switch
        {
            SerializedPropertyType.ObjectReference => property.objectReferenceValue == null,
            SerializedPropertyType.ExposedReference => property.exposedReferenceValue == null,
            SerializedPropertyType.AnimationCurve => property.animationCurveValue == null || property.animationCurveValue.length == 0,
            SerializedPropertyType.String => string.IsNullOrEmpty(property.stringValue),
            SerializedPropertyType.ManagedReference => property.managedReferenceValue == null,
            _ => false
        };
    }
}
#endif