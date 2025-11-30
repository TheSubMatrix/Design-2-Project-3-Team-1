using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

public static class DelegateHelper
{
    public static TDelegate CreateHandler<TDelegate>(Delegate onComplete) 
        where TDelegate : Delegate
    {
        MethodInfo delegateInvoke = typeof(TDelegate).GetMethod("Invoke");
        MethodInfo callbackInvoke = onComplete.GetType().GetMethod("Invoke");

        ParameterInfo[] delegateParams = delegateInvoke?.GetParameters() ?? Array.Empty<ParameterInfo>();
        ParameterInfo[] callbackParams = callbackInvoke?.GetParameters() ?? Array.Empty<ParameterInfo>();

        if (delegateParams.Length < callbackParams.Length)
        {
            throw new ArgumentException(
                $"Delegate {typeof(TDelegate).Name} has {delegateParams.Length} parameters " +
                $"but callback requires {callbackParams.Length} parameters");
        }

        for (int i = 0; i < callbackParams.Length; i++)
        {
            if (!callbackParams[i].ParameterType.IsAssignableFrom(delegateParams[i].ParameterType))
            {
                throw new ArgumentException(
                    $"Parameter {i} type mismatch: " +
                    $"delegate has {delegateParams[i].ParameterType.Name}, " +
                    $"callback expects {callbackParams[i].ParameterType.Name}");
            }
        }

        ParameterExpression[] paramExpressions = delegateParams
            .Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();

        Expression callExpression = Expression.Call(
            Expression.Constant(onComplete.Target),
            onComplete.Method,
            paramExpressions.Take(callbackParams.Length)
        );
    
        Expression<TDelegate> lambda = Expression.Lambda<TDelegate>(callExpression, paramExpressions);
        return lambda.Compile();
    }
    public static TDelegate CreateHandlerWithCallback<TDelegate>(Delegate onComplete, Action onEventFired) 
        where TDelegate : Delegate
    {
        if (onComplete == null)
            throw new ArgumentNullException(nameof(onComplete));
        
        MethodInfo delegateInvoke = typeof(TDelegate).GetMethod("Invoke");
        MethodInfo callbackInvoke = onComplete.GetType().GetMethod("Invoke");

        ParameterInfo[] delegateParams = delegateInvoke?.GetParameters() ?? Array.Empty<ParameterInfo>();
        ParameterInfo[] callbackParams = callbackInvoke?.GetParameters() ?? Array.Empty<ParameterInfo>();

        // Validate parameter compatibility
        if (delegateParams.Length < callbackParams.Length)
        {
            throw new ArgumentException(
                $"Delegate {typeof(TDelegate).Name} has {delegateParams.Length} parameters " +
                $"but callback requires {callbackParams.Length} parameters");
        }

        for (int i = 0; i < callbackParams.Length; i++)
        {
            if (!callbackParams[i].ParameterType.IsAssignableFrom(delegateParams[i].ParameterType))
            {
                throw new ArgumentException(
                    $"Parameter {i} type mismatch: " +
                    $"delegate has {delegateParams[i].ParameterType.Name}, " +
                    $"callback expects {callbackParams[i].ParameterType.Name}");
            }
        }

        ParameterExpression[] paramExpressions = delegateParams
            .Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();
        Expression callbackExpression = Expression.Call(
            Expression.Constant(onComplete.Target),
            onComplete.Method,
            paramExpressions.Take(callbackParams.Length)
        );

        Expression eventFiredExpression = Expression.Call(
            Expression.Constant(onEventFired.Target),
            onEventFired.Method
        );
        Expression body = Expression.Block(callbackExpression, eventFiredExpression);
        
        Expression<TDelegate> lambda = Expression.Lambda<TDelegate>(body, paramExpressions);
        return lambda.Compile();
    }
}
