using System.Collections;
using UnityEngine;

public static class SITFFX
{
    public static void Impact(Vector3 position, Vector3 normal, MaterialFactory materials)
    {
        GameObject decal=GameObject.CreatePrimitive(PrimitiveType.Cube);
        decal.name="IMPACT"; decal.transform.position=position+normal*0.025f; decal.transform.localScale=new Vector3(0.09f,0.09f,0.025f);
        decal.transform.rotation=Quaternion.LookRotation(normal);
        decal.GetComponent<Renderer>().sharedMaterial=materials.Get(new Color(0.08f,0.09f,0.09f));
        Collider c=decal.GetComponent<Collider>(); if(c!=null) Object.Destroy(c);
        SITFGame.Instance.StartCoroutine(FadeAndDestroy(decal,0.65f));

        for(int i=0;i<3;i++)
        {
            GameObject spark=GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.name="SPARK"; spark.transform.position=position; spark.transform.localScale=Vector3.one*0.045f;
            spark.GetComponent<Renderer>().sharedMaterial=materials.GetEmissive(new Color(1f,0.66f,0.24f),3f);
            Collider sc=spark.GetComponent<Collider>(); if(sc!=null) Object.Destroy(sc);
            Vector3 vel=(normal+Random.insideUnitSphere*0.8f).normalized*Random.Range(0.8f,2.1f);
            SITFGame.Instance.StartCoroutine(FlyAndFade(spark,vel,0.22f));
        }
    }

    public static void Blood(Vector3 position, Vector3 direction, MaterialFactory materials)
    {
        for(int i=0;i<7;i++)
        {
            GameObject drop=GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.name="BLOOD"; drop.transform.position=position+Random.insideUnitSphere*0.02f; drop.transform.localScale=Vector3.one*Random.Range(0.035f,0.065f);
            drop.GetComponent<Renderer>().sharedMaterial=materials.Get(new Color(0.48f,0.025f,0.035f));
            Collider c=drop.GetComponent<Collider>(); if(c!=null) Object.Destroy(c);
            Vector3 velocity=(direction*Random.Range(0.6f,1.3f)+Vector3.up*Random.Range(0.4f,1.6f)+Random.insideUnitSphere*1.1f)*0.55f;
            SITFGame.Instance.StartCoroutine(FlyAndFade(drop,velocity,Random.Range(0.25f,0.50f)));
        }
    }

    private static IEnumerator FlyAndFade(GameObject o,Vector3 velocity,float duration)
    {
        float t=0f;
        while(t<duration && o!=null)
        {
            t+=Time.deltaTime;
            velocity+=Physics.gravity*0.38f*Time.deltaTime;
            o.transform.position+=velocity*Time.deltaTime;
            o.transform.localScale=Vector3.Lerp(o.transform.localScale,Vector3.zero,Time.deltaTime*4f);
            yield return null;
        }
        if(o!=null) Object.Destroy(o);
    }

    private static IEnumerator FadeAndDestroy(GameObject o,float duration)
    {
        float t=0f; Vector3 start=o.transform.localScale;
        while(t<duration && o!=null)
        {
            t+=Time.deltaTime; o.transform.localScale=Vector3.Lerp(start,start*0.35f,t/duration); yield return null;
        }
        if(o!=null) Object.Destroy(o);
    }
}
