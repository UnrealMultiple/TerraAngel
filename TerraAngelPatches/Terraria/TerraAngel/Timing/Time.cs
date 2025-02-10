using System.Diagnostics;

namespace TerraAngel.Timing;

public class Time
{
    public static float DrawDeltaTime { get; private set; } = 1f / 60f;
    public static float UpdateDeltaTime { get; private set; } = 1f / 60f;

    public static double PreciseDrawDeltaTime { get; private set; } = 1.0 / 60.0;
    public static double PreciseUpdateDeltaTime { get; private set; } = 1.0 / 60.0;

    private static Stopwatch DrawStopwatch = new();
    private static Stopwatch UpdateStopwatch = new();

    public static void UpdateDraw()
    {
        DrawStopwatch.Stop();

        TimeMetrics.FramerateDeltaTimeSlices.Add(DrawStopwatch.Elapsed);
        PreciseDrawDeltaTime = DrawStopwatch.Elapsed.TotalSeconds;
        DrawDeltaTime = (float)PreciseDrawDeltaTime;

        DrawStopwatch.Restart();
    }

    public static void UpdateUpdate()
    {
        UpdateStopwatch.Stop();

        TimeMetrics.UpdateDeltaTimeSlices.Add(UpdateStopwatch.Elapsed);
        PreciseUpdateDeltaTime = UpdateStopwatch.Elapsed.TotalSeconds;
        UpdateDeltaTime = (float)PreciseUpdateDeltaTime;

        UpdateStopwatch.Restart();
    }
}
