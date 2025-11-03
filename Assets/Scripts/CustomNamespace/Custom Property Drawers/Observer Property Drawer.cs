#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using CustomNamespace.GenericDatatypes;
using CustomNamespace.Editor;
using CustomNamespace.Extensions;

namespace CustomNamespace.Editor
{
    [CustomPropertyDrawer(typeof(Observer<>), true)]
    public class ObserverPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement();
            
            // Create foldout
            Foldout foldout = new Foldout
            {
                text = property.displayName,
                value = property.isExpanded
            };
            
            foldout.RegisterValueChangedCallback(evt =>
            {
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });
            
            // Get the actual runtime type of the field
            System.Reflection.FieldInfo fieldInfo = property.GetFieldInfoAndStaticType(out System.Type fieldType);
            
            // Try to create hybrid UI using the cache system
            bool usedCustomDrawer = PropertyDrawerCache.CreateHybridPropertyUI(
                property, 
                foldout, 
                fieldType,
                excludeDrawerType: typeof(ObserverPropertyDrawer)
            );
            
            if (!usedCustomDrawer)
            {
                // Fallback to default field drawing
                SerializedProperty valueProperty = property.FindPropertyRelative("m_value");
                SerializedProperty eventProperty = property.FindPropertyRelative("m_onValueChanged");
                
                if (valueProperty != null)
                {
                    PropertyField valueField = new PropertyField(valueProperty);
                    valueField.BindProperty(valueProperty);
                    foldout.Add(valueField);
                }
                
                if (eventProperty != null)
                {
                    PropertyField eventField = new PropertyField(eventProperty);
                    eventField.BindProperty(eventProperty);
                    foldout.Add(eventField);
                }
            }
            
            root.Add(foldout);
            return root;
        }
    }
}
#endif