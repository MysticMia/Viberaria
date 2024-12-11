using System;

namespace Viberaria.VibrationManager;

public class VibrationEvent
{
    /// <summary>
    /// The start time of the event
    /// </summary>
    public DateTime Timestamp { get; }
    /// <summary>
    /// The length of an event in milliseconds
    /// </summary>
    public int Duration { get; }
    /// <summary>
    /// The strength of vibrations during the event
    /// </summary>
    public float Strength { get; }


    public VibrationEvent(DateTime timestamp, int duration, float strength)
    {
        Timestamp = timestamp;
        Duration = duration;
        Strength = strength;
    }

    public VibrationEvent(int duration, float strength) : this(DateTime.Now, duration, strength) { }

    public bool HasPassed()
    {
        DateTime eventEndTime = Timestamp + TimeSpan.FromMilliseconds(Duration);
        return DateTime.Now >= eventEndTime;
    }
}