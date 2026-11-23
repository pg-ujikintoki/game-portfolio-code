using System;

public static class PauseState
{
    public static bool IsPaused { get; private set; }
    public static event Action<bool> OnChanged;  // 監視したい側が購読

    public static void Set(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        OnChanged?.Invoke(IsPaused);
    }
}
