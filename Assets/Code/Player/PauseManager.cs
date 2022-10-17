public static class PauseManager {
    public static bool Paused { get; private set; }
    public static float GlobalSpeed { get; private set; } = 1;

    private static LTDescr CurrentGlobalSpeedTween;

    public static void Pause() {
        Paused = true;
    }

    public static void Unpause() {
        Paused = false;
    }

    public static LTDescr SetGlobalSpeed(float target, float time) {
        if (CurrentGlobalSpeedTween != null)
            LeanTween.cancel(CurrentGlobalSpeedTween.id);

        CurrentGlobalSpeedTween = LeanTween.value(GlobalSpeed, target, time).setOnUpdate(speed => GlobalSpeed = speed);
        return CurrentGlobalSpeedTween;
    }
}
