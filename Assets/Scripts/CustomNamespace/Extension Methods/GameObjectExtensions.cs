using System;
using UnityEngine;

namespace CustomNamespace.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="GameObject"/> class.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Checks if a GameObject has a specific component attached.
        /// </summary>
        /// <typeparam name="T">The type of the Component to check for.</typeparam>
        /// <param name="gameObject">The GameObject to check.</param>
        /// <returns>True if the GameObject has the specified component, otherwise false.</returns>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() is not null;
        }

        /// <summary>
        /// Recursively searches through the children and grandchildren of a Transform to find a child with the specified name.
        /// </summary>
        /// <param name="aParent">The parent Transform to start the search from.</param>
        /// <param name="aName">The name of the child Transform to find.</param>
        /// <returns>The Transform of the child with the specified name, or null if no such child is found.</returns>
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            foreach (Transform child in aParent)
            {
                if (child.name == aName)
                    return child;
                var result = child.FindDeepChild(aName);
                if (result != null)
                    return result;
            }
            return null;
        }
        /// <summary>
        /// Tries to get a <see cref="Component"/> of <see cref="T"/>. If a <see cref="Component"/> is found, this method returns it, otherwise it attempts to add the component
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to check or add a <see cref="Component"/> to</param>
        /// <typeparam name="T">The type of <see cref="Component"/> to add</typeparam>
        /// <returns>The found or new <see cref="Component"/></returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if(!component)
                component = gameObject.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// Creates a child <see cref="GameObject"/> with the same position and rotation as the parent
        /// </summary>
        /// <param name="parent">The <see cref="GameObject"/> that this new object should be a child of</param>
        /// <param name="name">The name of the child <see cref="GameObject"/></param>
        /// <param name="components">The <see cref="Component"/> that should be added to the <see cref="GameObject"/></param>
        /// <returns>The new child <see cref="GameObject"/></returns>
        public static GameObject CreateChild(this GameObject parent, string name = "GameObject", params Type[] components)
        {
            GameObject child = new GameObject(name, components);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            return child;
        }
    }
}