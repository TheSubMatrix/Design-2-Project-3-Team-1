using System;
using UnityEngine;

public class SerializedMethodTest : MonoBehaviour
{
    [SerializeField] SerializedMethod<object> m_testMethod;

    void PrintTestMethod()
    {
        Debug.Log("PrintTestMethod");
    }    
}
