#if UNITY_EDITOR
using System;
using CustomNamespace.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        Type[] genericArgs = fieldInfo.FieldType.GetGenericArguments();
        if (genericArgs.Length != 2)
            return new Label("Error: Invalid SerializableDictionary type");
        
        Type keyType = genericArgs[0];
        Type valueType = genericArgs[1];

        VisualElement container = new();
        SerializedProperty listProperty = property.FindPropertyRelative("m_list");
        SerializedProperty stagingProperty = property.FindPropertyRelative("m_stagingEntry");
        SerializedProperty stagingKey = stagingProperty.FindPropertyRelative("Key");

        Foldout foldout = new()
        {
            text = $"{property.displayName} ({listProperty.arraySize} entries)",
            value = property.isExpanded
        };
        foldout.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);

        // --- Staging area ---
        VisualElement newEntrySection = new();

        DrawUIWithLabel(stagingKey, newEntrySection, "Key", true, keyType);

        Button addButton = new(() =>
        {
            int existingIndex = -1;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty existingKey = element.FindPropertyRelative("Key");
                
                // Use the extension method for proper comparison
                if (!existingKey.CompareToProperty(stagingKey)) continue;
                existingIndex = i;
                break;
            }

            if (existingIndex >= 0)
            {
                property.serializedObject.ApplyModifiedProperties();
                return;
            }

            int index = listProperty.arraySize;
            listProperty.arraySize++;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            SerializedProperty elementNew = listProperty.GetArrayElementAtIndex(index);
            SerializedProperty keyProp = elementNew.FindPropertyRelative("Key");
            SerializedProperty valueProp = elementNew.FindPropertyRelative("Value");

            // Copy key value properly based on type
            if (typeof(UnityEngine.Object).IsAssignableFrom(keyType))
            {
                keyProp.objectReferenceValue = stagingKey.objectReferenceValue;
            }
            else
            {
                keyProp.boxedValue = stagingKey.boxedValue;
            }
            
            // Initialize value - check actual reflected type, not just SerializedProperty type
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
            {
                valueProp.objectReferenceValue = null;
                // Set the object reference instance ID to allow proper type filtering in the object picker
                valueProp.objectReferenceInstanceIDValue = 0;
            }
            else
            {
                valueProp.boxedValue = Activator.CreateInstance(valueType);
            }

            property.serializedObject.ApplyModifiedProperties();
        })
        {
            text = "Add / Select",
            style = { alignSelf = Align.FlexEnd, width = 100 }
        };
        newEntrySection.Add(addButton);

        // Divider
        newEntrySection.Add(new VisualElement
        {
            style = {
                height = 1,
                marginTop = 6,
                marginBottom = 6,
                backgroundColor = new StyleColor(EditorGUIUtility.isProSkin 
                    ? new Color(0.4f,0.4f,0.4f,0.35f) 
                    : new Color(0.25f,0.25f,0.25f,0.35f))
            }
        });

        foldout.Add(newEntrySection);

        // --- Existing entries list ---
        VisualElement listContainer = new();
        foldout.Add(listContainer);
        container.Add(foldout);

        RebuildList();
        
        int lastArraySize = listProperty.arraySize;
        container.TrackPropertyValue(listProperty, p =>
        {
            if (p.arraySize == lastArraySize) return;
            lastArraySize = p.arraySize;
            foldout.text = $"{property.displayName} ({p.arraySize} entries)";
            RebuildList();
        });

        return container;

        void RebuildList()
        {
            listContainer.Clear();

            if (listProperty.arraySize == 0)
            {
                listContainer.Add(new Label("No entries") { style = { unityFontStyleAndWeight = FontStyle.Italic } });
                return;
            }

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                int idx = i;
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("Key");
                SerializedProperty valProp = element.FindPropertyRelative("Value");

                VisualElement row = new()
                {
                    style = {
                        flexDirection = FlexDirection.Column,
                        paddingTop = 2,
                        paddingBottom = 2,
                        paddingLeft = 2,
                        paddingRight = 2,
                        marginBottom = 3,
                        borderBottomWidth = 1,
                        borderBottomColor = new StyleColor(new Color(0.1f,0.1f,0.1f,0.3f))
                    }
                };

                DrawUIWithLabel(keyProp, row, "Key", false, keyType);
                DrawUIWithLabel(valProp, row, "Value", true, valueType);

                Button remove = new(() =>
                    {
                        listProperty.DeleteArrayElementAtIndex(idx);
                        property.serializedObject.ApplyModifiedProperties();
                    })
                    { text = "Remove", style = { alignSelf = Align.FlexEnd } };
                row.Add(remove);

                listContainer.Add(row);
            }
        }
    }

    static void DrawUIWithLabel(SerializedProperty prop, VisualElement container, string label, bool enabled, Type expectedType = null)
    {
        VisualElement fieldContainer = new VisualElement();
        
        // If it's an object reference, and we know the expected type, use ObjectField directly
        if (expectedType != null && 
            typeof(UnityEngine.Object).IsAssignableFrom(expectedType) && 
            prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            ObjectField objectField = new ObjectField(label)
            {
                objectType = expectedType,
                allowSceneObjects = true,
                value = prop.objectReferenceValue
            };
            
            // Manually handle value changes instead of using BindProperty
            objectField.RegisterValueChangedCallback(evt =>
            {
                prop.objectReferenceValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            
            // Track property changes to update the field
            fieldContainer.TrackPropertyValue(prop, p =>
            {
                if (objectField.value != p.objectReferenceValue)
                    objectField.value = p.objectReferenceValue;
            });
            
            objectField.SetEnabled(enabled);
            fieldContainer.Add(objectField);
        }
        else
        {
            // Use standard PropertyField for everything else
            PropertyField field = new PropertyField(prop, label);
            field.BindProperty(prop);
            field.SetEnabled(enabled);
            fieldContainer.Add(field);
        }
        
        container.Add(fieldContainer);
    }

}
#endif