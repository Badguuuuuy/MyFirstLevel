using UnityEngine;

public static class UnscaledShaderTimeBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (GameObject.Find("[Global] ShaderTimeUpdater") == null)
        {
            GameObject g = new GameObject("[Global] ShaderTimeUpdater");
            g.AddComponent<UnscaledTimeUpdater>();
            Object.DontDestroyOnLoad(g);
        }
    }
}