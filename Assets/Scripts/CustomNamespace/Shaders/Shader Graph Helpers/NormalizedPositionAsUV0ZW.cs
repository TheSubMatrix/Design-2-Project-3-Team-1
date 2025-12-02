using UnityEngine;
using UnityEngine.UI;

namespace ShaderGraph.Helpers
{
    [AddComponentMenu("UI/Effects/Normalized Position As UV0.ZW", 82)]
    public class NormalizedPositionAsUV0ZW : BaseMeshEffect
    {
        protected NormalizedPositionAsUV0ZW()
        { }
        public override void ModifyMesh(VertexHelper vh)
        {
            Bounds bounds = new Bounds();

            UIVertex vert = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                bounds.Encapsulate(vert.position);
            }
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);

                Vector2 normalizedPosition = new Vector2(
                    Mathf.InverseLerp(bounds.min.x, bounds.max.x, vert.position.x),
                    Mathf.InverseLerp(bounds.min.y, bounds.max.y, vert.position.y));

                vert.uv0 = new Vector4(vert.uv0.x, vert.uv0.y, normalizedPosition.x, normalizedPosition.y);

                vh.SetUIVertex(vert, i);
            }
        }
    }
}