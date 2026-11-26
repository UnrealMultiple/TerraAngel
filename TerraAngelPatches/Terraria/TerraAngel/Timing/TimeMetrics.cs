using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TerraAngel.Timing;

public class TimeMetrics
{
    public readonly static TimeLog FramerateDeltaTimeSlices = new TimeLog(25);

    public readonly static TimeLog UpdateDeltaTimeSlices = new TimeLog(25);

    public readonly static Dictionary<string, TimeLog> TimeLogs = new Dictionary<string, TimeLog>();

    private readonly static Dictionary<string, MetricsTimer> MetricTimers = new Dictionary<string, MetricsTimer>();

    public static MetricsTimer GetMetricsTimer(string name)
    {
        if (!MetricTimers.TryGetValue(name, out MetricsTimer? timer))
        {
            timer = new MetricsTimer(name);
            MetricTimers.Add(name, timer);
        }

        return timer;
    }

    public static void AddTime(string name, TimeSpan time)
    {
        if (!TimeLogs.TryGetValue(name, out TimeLog? log))
        {
            log = new TimeLog();
            TimeLogs.Add(name, log);
        }

        log.Add(time);
    }

    public class TimeLog
    {
        public Queue<TimeSpan> Times = new();

        private int CustomMaxTimeCount = -1;

        public int MaxTimeCount => CustomMaxTimeCount >= 0
            ? CustomMaxTimeCount
            : ClientLoader.WindowManager!.FPSCap * 2; // set that to twice of FPSCap

        public bool Paused = false;

        public TimeSpan Sum = TimeSpan.Zero;

        public TimeSpan Average
        {
            get
            {
                if (Times.Count == 0 || Sum == TimeSpan.Zero)
                {
                    return TimeSpan.Zero;
                }

                return Sum / Times.Count;
            }
        }

        public TimeSpan Max
        {
            get
            {
                if (Times.Count == 0 || Sum == TimeSpan.Zero)
                {
                    return TimeSpan.Zero;
                }

                return Times.Max();
            }
        }

        public TimeSpan Min
        {
            get
            {
                if (Times.Count == 0 || Sum == TimeSpan.Zero)
                {
                    return TimeSpan.Zero;
                }

                return Times.Min();
            }
        }

        public void Add(TimeSpan time)
        {
            if (Paused) return;
            Times.Enqueue(time);

            Sum += time;

            if (Times.Count > MaxTimeCount) Sum -= Times.Dequeue();
        }

        public TimeLog()
        {
        }

        public TimeLog(int maxTimeCount)
        {
            CustomMaxTimeCount = maxTimeCount;
        }
    }
}

public abstract class Timer
{
    public readonly string Name;
    public readonly Stopwatch Watch;

    public Timer(string name)
    {
        Name = name;
        Watch = new Stopwatch();
    }
}

public class MetricsTimer : Timer
{
    public MetricsTimer(string name)
        : base(name)
    {

    }

    public MetricsTimer Start()
    {
        Watch.Restart();
        return this;
    }

    public void Stop()
    {
        Watch.Stop();
        TimeMetrics.AddTime(Name, Watch.Elapsed);
    }
}