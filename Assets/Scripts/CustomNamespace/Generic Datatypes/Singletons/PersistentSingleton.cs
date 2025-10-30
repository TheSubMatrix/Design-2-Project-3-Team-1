using UnityEngine;

public class PersistentSingleton<T> : MonoBehaviour where T : Component {
    public bool AutoUnparentOnAwake = true;

    protected static T CurrentInstance;

    public static bool HasInstance => CurrentInstance != null;
    public static T TryGetInstance() => HasInstance ? CurrentInstance : null;

    public static T Instance {
        get {
            if (CurrentInstance is not null) return CurrentInstance;
            CurrentInstance = FindAnyObjectByType<T>();
            if (CurrentInstance is not null) return CurrentInstance;
            GameObject go = new GameObject(typeof(T).Name + " Auto-Generated");
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

        if (AutoUnparentOnAwake) {
            transform.SetParent(null);
        }

        if (CurrentInstance == null) {
            CurrentInstance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else {
            if (CurrentInstance != this) {
                Destroy(gameObject);
            }
        }
    }
}