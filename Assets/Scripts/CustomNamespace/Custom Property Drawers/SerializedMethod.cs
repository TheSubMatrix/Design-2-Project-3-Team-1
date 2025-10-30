using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
/// <summary>
/// Allows the user to serialize a method with the <see cref="MethodName"/>, <see cref="Target"/>, and <see cref="Parameters"/>. This method can be invoked with <see cref="Invoke()"/> and returns a value of type <see cref="TReturn"/> when invoked
/// </summary>
/// <typeparam name="TReturn">The type that this method should return</typeparam>
[Serializable]
public class SerializedMethod<TReturn> : ISerializationCallbackReceiver
{
    /// <summary>
    /// The parameters with which to invoke this serialized method
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public object[] Parameters { get; set; }
    
    Object m_target;
    /// <summary>
    /// The <see cref="Object"/> on which the serialized method should try and invoke the method with the given <see cref="MethodName"/>. Updating this will rebuild the delegate
    /// </summary>
    public Object Target
    {
        get => m_target;
        set
        {
            m_target = value;
            m_isDelegateRebuilt = false;
        }
    }
    
    string m_methodName;
    /// <summary>
    /// The name of the method we should try and invoke on out <see cref="Target"/>
    /// </summary>
    public string MethodName
    {
        get => m_methodName;
        set
        {
            m_methodName = value;
            m_isDelegateRebuilt = false;
        }
    }
    [NonSerialized] Delegate m_cachedDelegate;
    [NonSerialized] bool m_isDelegateRebuilt;

    /// <summary>
    /// Attempts to invoke the method with the name <see cref="MethodName"/> on <see cref="Target"/> with parameters <see cref="Parameters"/>
    /// </summary>
    /// <returns>The result of the found method or default</returns>
    public TReturn Invoke()
    {
        return Invoke(Parameters);
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    /// Attempts to invoke the method with the name <see cref="MethodName"/> on <see cref="Target"/>
    /// </summary>
    /// <param name="parameters">The parameters with which to invoke this method</param>
    /// <returns>The result of the found method or default</returns>
    public TReturn Invoke(object[] parameters)
    {
        if(!m_isDelegateRebuilt){ BuildDelegate();}

        if (m_cachedDelegate is not null)
        {
            object result = m_cachedDelegate.DynamicInvoke(Parameters);
            return (TReturn) Convert.ChangeType(result, typeof(TReturn));
        }
        Debug.LogError($"SerializedMethod on {m_methodName}: Delegate is unable to be invoked");
        return default;
    }
    
    void BuildDelegate()
    {
        //Erase the existing delegate
        m_cachedDelegate = null;
        if(m_target is null || m_methodName is null){Debug.LogError($"SerializedMethod on {m_methodName}: Target or Method Name is null"); return;}
        MethodInfo methodInfo = m_target.GetType().GetMethod(m_methodName, bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //Idiot Proofing :)
        if(methodInfo is null){Debug.LogError($"SerializedMethod on {m_methodName}: MethodInfo is null"); return;}
        //Get the parameter types to build our delegate
        Type[] parameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        if(Parameters.Length != parameterTypes.Length){Debug.LogError($"SerializedMethod on {m_methodName}: m_parameters length does not match MethodInfo"); return;}
        //Create the delegate type we need
        Type delegateType = Expression.GetDelegateType(parameterTypes.Append(methodInfo.ReturnType).ToArray());
        //Create delegate from determined type and cache it
        m_cachedDelegate = methodInfo.CreateDelegate(delegateType, m_target);
        m_isDelegateRebuilt = true;
    }
    /// <inheritdoc/>
    public void OnBeforeSerialize()
    {
        //No op
    }

    /// <inheritdoc/>
    public void OnAfterDeserialize()
    {
        m_isDelegateRebuilt = false;
    }
}
