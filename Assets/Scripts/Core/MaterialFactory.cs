using System.Collections.Generic;
using UnityEngine;

public sealed class MaterialFactory
{
    private readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    public Material Get(Color color, float metallic = 0.0f, float smoothness = 0.15f, bool emission = false)
    {
        Color32 c = color;
        string key = $"{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}:{metallic:F2}:{smoothness:F2}:{emission}";
        if (cache.TryGetValue(key, out Material existing)) return existing;

        Shader shader = Shader.Find("Standard");
        Material mat = new Material(shader)
        {
            name = "SITF_MAT_" + key,
            enableInstancing = true
        };
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        if (emission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.2f);
        }
        cache[key] = mat;
        return mat;
    }

    public Material GetEmissive(Color color, float intensity = 2.0f)
    {
        Color c = color * intensity;
        Color32 cc = color;
        string key = $"EM:{cc.r:X2}{cc.g:X2}{cc.b:X2}:{intensity:F2}";
        if (cache.TryGetValue(key, out Material existing)) return existing;

        Shader shader = Shader.Find("Standard");
        Material mat = new Material(shader)
        {
            name = "SITF_EMISSIVE_" + key,
            enableInstancing = true
        };
        mat.SetColor("_Color", color);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", c);
        mat.SetFloat("_Glossiness", 0.05f);
        cache[key] = mat;
        return mat;
    }
}
