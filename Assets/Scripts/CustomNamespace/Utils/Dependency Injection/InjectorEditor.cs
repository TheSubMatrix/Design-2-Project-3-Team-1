using CustomNamespace.DependencyInjection;
using UnityEditor;
using UnityEngine;

namespace CustomNamespace.DependencyInjection {
    [CustomEditor(typeof(Injector))]
    public class InjectorEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            Injector injector = (Injector) target;

            if (GUILayout.Button("Validate Dependencies")) {
                Injector.ValidateDependencies();
            }

            if (!GUILayout.Button("Clear All Injectable Fields")) return;
            Injector.ClearDependencies();
            EditorUtility.SetDirty(injector);
        }
    }
}