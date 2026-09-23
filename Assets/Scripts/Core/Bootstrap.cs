using UnityEngine;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateGame()
    {
        if (Object.FindFirstObjectByType<SITFGame>() != null) return;
        GameObject root = new GameObject("SITF_GAME");
        root.AddComponent<SITFGame>();
    }
}
