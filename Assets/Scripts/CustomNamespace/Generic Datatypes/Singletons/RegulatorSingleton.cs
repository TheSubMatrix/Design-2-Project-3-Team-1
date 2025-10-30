using UnityEngine;

/// <summary>
/// Persistent Regulator singleton, will destroy any other older components of the same type it finds on awake
/// </summary>
public class RegulatorSingleton<T> : MonoBehaviour where T : Component {
    protected static T CurrentInstance;

    public static bool HasInstance => CurrentInstance != null;

    public float InitializationTime { get; private set; }

    public static T Instance {
        get {
            if (CurrentInstance != null) return CurrentInstance;
            CurrentInstance = FindAnyObjectByType<T>();
            if (CurrentInstance != null) return CurrentInstance;
            GameObject go = new GameObject(typeof(T).Name + " Auto-Generated")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            CurrentInstance = go.AddComponent<T>();

            return CurrentInstance;
        }
    }

    /// <summary>
    /// Make sure to call base.Awake() in override if you need awake.
    /// </summary>
    protected virtual void Awake() {
        InitializeSingleton();
    }

    protected virtual void InitializeSingleton() {
        if (!Application.isPlaying) return;
        InitializationTime = Time.time;
        DontDestroyOnLoad(gameObject);

        T[] oldInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (T old in oldInstances) {
            if (old.GetComponent<RegulatorSingleton<T>>().InitializationTime < InitializationTime) {
                Destroy(old.gameObject);
            }
        }

        if (CurrentInstance == null) {
            CurrentInstance = this as T;
        }
    }
}
