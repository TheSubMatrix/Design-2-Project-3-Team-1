using System;
using UnityEngine;
using UnityEngine.VFX;

public class VFXPropertyHelper<T>
{
    public readonly string Name;
    public T Value;
    readonly int m_nameID;
    readonly Action<VisualEffect> m_applyAction;

    public VFXPropertyHelper(string name, T value)
    {
        Name = name;
        Value = value;
        m_nameID = Shader.PropertyToID(name);
        m_applyAction = InitializeApplyAction();
    }

    Action<VisualEffect> InitializeApplyAction()
    {
        Type t = typeof(T);

        if (t == typeof(float))
            return vfx => vfx.SetFloat(m_nameID, (float)(object)Value);

        if (t == typeof(int))
            return vfx => vfx.SetInt(m_nameID, (int)(object)Value);

        if (t == typeof(uint))
            return vfx => vfx.SetUInt(m_nameID, (uint)(object)Value);

        if (t == typeof(bool))
            return vfx => vfx.SetBool(m_nameID, (bool)(object)Value);

        if (t == typeof(Vector2))
            return vfx => vfx.SetVector2(m_nameID, (Vector2)(object)Value);

        if (t == typeof(Vector3))
            return vfx => vfx.SetVector3(m_nameID, (Vector3)(object)Value);

        if (t == typeof(Vector4))
            return vfx => vfx.SetVector4(m_nameID, (Vector4)(object)Value);

        if (t == typeof(Color))
            return vfx => vfx.SetVector4(m_nameID, (Color)(object)Value);

        if (t == typeof(Matrix4x4))
            return vfx => vfx.SetMatrix4x4(m_nameID, (Matrix4x4)(object)Value);

        if (t == typeof(AnimationCurve))
            return vfx => vfx.SetAnimationCurve(m_nameID, (AnimationCurve)(object)Value);

        if (t == typeof(Gradient))
            return vfx => vfx.SetGradient(m_nameID, (Gradient)(object)Value);

        if (t == typeof(Texture))
            return vfx => vfx.SetTexture(m_nameID, (Texture)(object)Value);

        if (t == typeof(GraphicsBuffer))
            return vfx => vfx.SetGraphicsBuffer(m_nameID, (GraphicsBuffer)(object)Value);

        if (t == typeof(Mesh))
            return vfx => vfx.SetMesh(m_nameID, (Mesh)(object)Value);

        if (t == typeof(SkinnedMeshRenderer))
            return vfx => vfx.SetSkinnedMeshRenderer(m_nameID, (SkinnedMeshRenderer)(object)Value);

        Debug.LogError($"VFXPropertyHelper: Unsupported type: {typeof(T).Name} for property '{Name}'");
        return vfx => { };
    }

    public void ApplyToVFX(VisualEffect vfx)
    {
        m_applyAction.Invoke(vfx);
    }
}


[Serializable]
public abstract class VFXProperty
{
    public string Name;
    public abstract void ApplyToVFX(VisualEffect vfx);

}


[Serializable]
public class FloatVFXProperty : VFXProperty
{
    public float Value;
    [NonSerialized] VFXPropertyHelper<float> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<float>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}


[Serializable]
public class IntVFXProperty : VFXProperty
{
    public int Value;
    [NonSerialized] VFXPropertyHelper<int> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || m_helper.Value != Value)
            m_helper = new VFXPropertyHelper<int>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class UIntVFXProperty : VFXProperty
{
    public uint Value;
    [NonSerialized] VFXPropertyHelper<uint> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || m_helper.Value != Value)
            m_helper = new VFXPropertyHelper<uint>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class BoolVFXProperty : VFXProperty
{
    public bool Value;
    [NonSerialized] VFXPropertyHelper<bool> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || m_helper.Value != Value)
            m_helper = new VFXPropertyHelper<bool>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class Vector2VFXProperty : VFXProperty
{
    public Vector2 Value;
    [NonSerialized] VFXPropertyHelper<Vector2> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<Vector2>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}

[Serializable]
public class Vector3VFXProperty : VFXProperty
{
    public Vector3 Value;
    [NonSerialized] VFXPropertyHelper<Vector3> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<Vector3>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class Vector4VFXProperty : VFXProperty
{
    public Vector4 Value;
    [NonSerialized] VFXPropertyHelper<Vector4> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<Vector4>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class ColorVFXProperty : VFXProperty
{
    public Color Value;
    [NonSerialized] VFXPropertyHelper<Color> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<Color>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class Matrix4X4VFXProperty : VFXProperty
{
    public Matrix4x4 Value;
    [NonSerialized] VFXPropertyHelper<Matrix4x4> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<Matrix4x4>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class CurveVFXProperty : VFXProperty
{
    public AnimationCurve Value;
    [NonSerialized] private VFXPropertyHelper<AnimationCurve> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<AnimationCurve>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class GradientVFXProperty : VFXProperty
{
    public Gradient Value;
    [NonSerialized] private VFXPropertyHelper<Gradient> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || !m_helper.Value.Equals(Value))
            m_helper = new VFXPropertyHelper<Gradient>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class TextureVFXProperty : VFXProperty
{
    public Texture Value;
    [NonSerialized] VFXPropertyHelper<Texture> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || m_helper.Value != Value)
            m_helper = new VFXPropertyHelper<Texture>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class GraphicsBufferVFXProperty : VFXProperty
{
    public GraphicsBuffer Value;
    [NonSerialized] VFXPropertyHelper<GraphicsBuffer> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || m_helper.Value != Value)
            m_helper = new VFXPropertyHelper<GraphicsBuffer>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class MeshVFXProperty : VFXProperty
{
    public Mesh Value;
    [NonSerialized] private VFXPropertyHelper<Mesh> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || m_helper.Value != Value)
            m_helper = new VFXPropertyHelper<Mesh>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}
[Serializable]
public class SkinnedMeshRendererVFXProperty : VFXProperty
{
    public SkinnedMeshRenderer Value;
    [NonSerialized] private VFXPropertyHelper<SkinnedMeshRenderer> m_helper;

    public override void ApplyToVFX(VisualEffect vfx)
    {
        if (m_helper == null || m_helper.Name != Name || m_helper.Value != Value)
            m_helper = new VFXPropertyHelper<SkinnedMeshRenderer>(Name, Value);

        m_helper.ApplyToVFX(vfx);
    }
}

