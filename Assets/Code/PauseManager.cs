public static class PauseManager {
    public static bool Paused { get; private set; }

    public static void Pause() {
        Paused = true;
    }

    public static void Unpause() {
        Paused = false;
    }
}
