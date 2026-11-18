using UnityEngine;

/// <summary>Runs before the first scene: the screen never dims or locks while the game is open (a sleeping phone pauses
/// the app and Photon drops the connection), and the game keeps running when the window loses focus (editor / desktop).</summary>
public static class AppDisplaySettings
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.runInBackground = true;
    }
}
