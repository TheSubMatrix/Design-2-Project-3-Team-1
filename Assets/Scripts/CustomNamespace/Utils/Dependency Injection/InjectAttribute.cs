using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
#endif

namespace CustomNamespace.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class InjectAttribute : Attribute { }
    
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InjectAttribute))]
    public class InjectPropertyDrawer : PropertyDrawer
    {
        Texture2D m_icon;

        Texture2D LoadIcon()
        {
            if (m_icon != null) return m_icon;

            m_icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Scripts/CustomNamespace/Custom Property Drawers/Drawer Assets/Injection Icon.png");

            if (m_icon == null)
            {
                Debug.LogWarning("Failed to load injection icon. Check the path and file extension.");
            }

            return m_icon;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            Image iconElement = new()
            {
                image = LoadIcon(),
                style =
                {
                    width = 20,
                    height = 20,
                    marginRight = 4
                }
            };
            
            PropertyField propertyField = new(property, property.displayName)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            UpdateIconColor();
            propertyField.RegisterValueChangeCallback(_ => { UpdateIconColor(); });
            container.RegisterCallback<AttachToPanelEvent>(_ => { UpdateIconColor(); });
            container.Add(iconElement);
            container.Add(propertyField);
            return container;
            void UpdateIconColor()
            {
                iconElement.tintColor = property.objectReferenceValue == null ? Color.white : Color.green;
            }
        }
    }
    
    #endif

}