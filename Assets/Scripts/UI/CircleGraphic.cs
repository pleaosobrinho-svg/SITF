using UnityEngine;
using UnityEngine.UI;

public sealed class CircleGraphic : Graphic
{
    [SerializeField] private float fill=0.16f;
    [SerializeField] private float ring=0.72f;
    [SerializeField] private float thickness=5f;
    [SerializeField] private int segments=64;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*0.5f;
        Vector2 center=rectTransform.rect.center;
        float inner=Mathf.Max(0f,radius-thickness);
        int count=Mathf.Clamp(segments,16,96);

        for(int i=0;i<count;i++)
        {
            float a0=(float)i/count*Mathf.PI*2f, a1=(float)(i+1)/count*Mathf.PI*2f;
            Vector2 o0=center+new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*radius;
            Vector2 o1=center+new Vector2(Mathf.Cos(a1),Mathf.Sin(a1))*radius;
            Vector2 i0=center+new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*inner;
            Vector2 i1=center+new Vector2(Mathf.Cos(a1),Mathf.Sin(a1))*inner;
            int baseIndex=vh.currentVertCount;
            vh.AddVert(o0,color); vh.AddVert(o1,color);
            vh.AddVert(i1,new Color(color.r,color.g,color.b,ring*color.a));
            vh.AddVert(i0,new Color(color.r,color.g,color.b,ring*color.a));
            vh.AddTriangle(baseIndex,baseIndex+1,baseIndex+2);
            vh.AddTriangle(baseIndex,baseIndex+2,baseIndex+3);
        }

        if(fill>0f)
        {
            int baseIndex=vh.currentVertCount;
            vh.AddVert(center,new Color(color.r,color.g,color.b,fill*color.a));
            for(int i=0;i<count;i++)
            {
                float a0=(float)i/count*Mathf.PI*2f;
                Vector2 p0=center+new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*(radius-thickness*0.5f);
                vh.AddVert(p0,new Color(color.r,color.g,color.b,fill*color.a));
            }
            for(int i=0;i<count;i++)
            {
                int b=baseIndex+1+i, n=baseIndex+1+((i+1)%count);
                vh.AddTriangle(baseIndex,b,n);
            }
        }
    }
}
