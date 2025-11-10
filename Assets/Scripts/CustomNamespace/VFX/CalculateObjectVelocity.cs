using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

public class CalculateObjectVelocity : VFXSpawnerCallbacks
{
    static readonly int s_PositionPropertyId = Shader.PropertyToID("ObjectPositionWS");

    static readonly int s_PositionAttributeId = Shader.PropertyToID("position");
    static readonly int s_OldPositionAttributeId = Shader.PropertyToID("oldPosition");
    static readonly int s_VelocityAttributeId = Shader.PropertyToID("velocity");

    public class InputProperties
    {
        public Vector3 ObjectPositionWS = Vector3.zero;
    }
    float3 m_lastPosition;

    public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        m_lastPosition = vfxValues.GetVector3(s_PositionPropertyId);
    }

    public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        if (!state.playing || state.deltaTime == 0) return;

        float3 position = vfxValues.GetVector3(s_PositionPropertyId);

        state.vfxEventAttribute.SetVector3(s_OldPositionAttributeId, m_lastPosition);
        state.vfxEventAttribute.SetVector3(s_PositionAttributeId, position);

        state.vfxEventAttribute.SetVector3(s_VelocityAttributeId, (position - m_lastPosition) / state.deltaTime);

        m_lastPosition = position;
    }

    public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {

    }
}