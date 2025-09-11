using System;

namespace TerraAngel.Utility;

public struct TimerSlim
{
    public float RemainingTime = 0.0f;
    public float Interval;
    public TimeBy Strategy;
    
    public TimerSlim(float interval, TimeBy strategy, bool willTriggerOnFirstTick = false)
    {
        Interval = interval;
        Strategy = strategy;
        if (willTriggerOnFirstTick)
            RemainingTime = interval;
    }

    public bool TickTock()
    {
        RemainingTime -= Strategy switch
        {
            TimeBy.DrawDeltaTime => Time.DrawDeltaTime,
            TimeBy.UpdateDeltaTime => Time.UpdateDeltaTime,
            TimeBy.Frame => 1.0f,
            _ => throw new ArgumentOutOfRangeException(nameof(Strategy), Strategy, null)
        };

        if (RemainingTime > 0.0f)
            return false;
        
        RemainingTime += Interval; // Times up, reset timer
        RemainingTime = Math.Max(RemainingTime, 0.0f); // if still negative after resetting, force it to be 0.0f
        return true;
    }

    public void Reset(bool willTriggerOnNextTick = false)
    {
        RemainingTime = willTriggerOnNextTick
            ? 0.0f
            : Interval;
    }
    
    public enum TimeBy
    {
        DrawDeltaTime,
        UpdateDeltaTime,
        Frame
    }
}