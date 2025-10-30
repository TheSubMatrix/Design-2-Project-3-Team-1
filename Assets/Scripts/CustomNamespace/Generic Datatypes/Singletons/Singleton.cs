using UnityEngine;

namespace CustomNamespace.GenericDatatypes
{
    /// <summary>
    /// A single instance of <see cref="T"/> that has a constant reference to the instantiated instance via <see cref="Instance"/> and will destroy other instances if they are not the <see cref="SingletonInstance"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Singleton{T}"/> to be created</typeparam>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        /// <summary>
        /// The current active instance of the singleton
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        protected static T SingletonInstance;
        public static bool HasInstance => Instance != null;

        /// <summary>
        /// Gets the current active singleton instance, and if one does not exist, it creates it
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static T Instance
        {
            get
            {
                if (SingletonInstance is not null) return SingletonInstance;
                SingletonInstance = FindFirstObjectByType<T>();
                if (SingletonInstance is not null) return SingletonInstance;
                GameObject newSingletonHolder = new GameObject
                {
                    name = typeof(T).Name
                };
                SingletonInstance = newSingletonHolder.AddComponent<T>();
                return SingletonInstance;
            }
        }

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        protected virtual void Awake() => InitializeSingleton();
        /// <summary>
        /// Initializes the singleton as the <see cref="SingletonInstance"/> if the application is playing
        /// </summary>
        protected virtual void InitializeSingleton()
        {
            if(!Application.isPlaying) return;
            SingletonInstance = this as T;
        }
    }
}
