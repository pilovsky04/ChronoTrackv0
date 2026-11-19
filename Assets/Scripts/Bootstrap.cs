using UnityEngine;

// Creates the whole game at runtime, so any (even empty) scene can run ChronoTrack.
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (UnityEngine.Object.FindFirstObjectByType<GameManager>() != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }
}
