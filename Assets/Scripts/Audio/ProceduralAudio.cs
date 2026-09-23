using System.Collections.Generic;
using UnityEngine;

public static class ProceduralAudio
{
    private static readonly Dictionary<string,AudioClip> cache=new Dictionary<string,AudioClip>();

    public static AudioClip Shot(string kind)
    {
        string key="shot_"+kind;
        if(cache.TryGetValue(key,out AudioClip c)) return c;
        float baseFreq=kind=="pistol"?520f:kind=="smg"?390f:kind=="rifle"?460f:kind=="shotgun"?150f:kind=="dmr"?500f:650f;
        float length=kind=="shotgun"?0.23f:kind=="sniper"?0.18f:0.12f;
        AudioClip clip=CreateNoiseTone(key,length,22050,baseFreq,0.75f,0.18f,true); cache[key]=clip; return clip;
    }

    public static AudioClip Reload()
    {
        const string key="reload";
        if(cache.TryGetValue(key,out AudioClip c)) return c;
        AudioClip clip=CreateNoiseTone(key,0.18f,16000,190f,0.12f,0.35f,false); cache[key]=clip; return clip;
    }

    public static AudioClip Hit()
    {
        const string key="hit";
        if(cache.TryGetValue(key,out AudioClip c)) return c;
        AudioClip clip=CreateNoiseTone(key,0.075f,16000,890f,0.15f,0.15f,false); cache[key]=clip; return clip;
    }

    public static AudioClip Footstep()
    {
        const string key="footstep";
        if(cache.TryGetValue(key,out AudioClip c)) return c;
        AudioClip clip=CreateNoiseTone(key,0.085f,12000,82f,0.68f,0.16f,false); cache[key]=clip; return clip;
    }

    public static AudioClip Click()
    {
        const string key="click";
        if(cache.TryGetValue(key,out AudioClip c)) return c;
        AudioClip clip=CreateNoiseTone(key,0.045f,12000,1300f,0.18f,0.06f,false); cache[key]=clip; return clip;
    }

    private static AudioClip CreateNoiseTone(string name,float seconds,int sampleRate,float frequency,float noiseAmount,float volume,bool layered)
    {
        int samples=Mathf.Max(1,Mathf.CeilToInt(seconds*sampleRate));
        AudioClip clip=AudioClip.Create(name,samples,1,sampleRate,false);
        float[] data=new float[samples];

        for(int i=0;i<samples;i++)
        {
            float t=(float)i/sampleRate;
            float env=Mathf.Exp(-16f*t/Mathf.Max(seconds,0.05f));
            float tone=Mathf.Sin(2f*Mathf.PI*frequency*t)*0.75f;
            float noise=Mathf.Sin(i*12.9898f)*0.5f+Mathf.Sin(i*78.233f)*0.25f;
            float body=layered?Mathf.Sin(2f*Mathf.PI*frequency*0.47f*t)*0.26f:0f;
            data[i]=Mathf.Clamp((tone*(1f-noiseAmount)+noise*noiseAmount+body)*env*volume,-1f,1f);
        }
        clip.SetData(data,0);
        return clip;
    }
}
