using UnityEngine;

public static class PauseManager
{
    private static int pauseRequests = 0;

    public static void RequestPause()
    {
        pauseRequests++;
        UpdatePauseState();
    }

    public static void ReleasePause()
    {
        pauseRequests = Mathf.Max(0, pauseRequests - 1);
        UpdatePauseState();
    }

    private static void UpdatePauseState()
    {
        Time.timeScale = (pauseRequests > 0) ? 0f : 1f;
    }

    public static void ForceUnpause()
    {
        pauseRequests = 0;
        Time.timeScale = 1f;
    }
}
