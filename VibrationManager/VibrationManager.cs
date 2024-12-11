using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static Viberaria.ViberariaConfig;
using static Viberaria.bClient;

namespace Viberaria.VibrationManager;

public static class VibrationManager
{
    private static VibrationEvent _currentEvent = null;
    private static readonly Dictionary<VibrationPriority, LinkedList<VibrationEvent>> EventLists = new();
    private static bool _processingBusy = false;

    static VibrationManager()
    {
        foreach (VibrationPriority priority in Enum.GetValues(typeof(VibrationPriority)))
        {
            EventLists[priority] = [];
        }
    }

    /// <summary>
    /// Create a new event to vibrate plugs for a certain duration.
    /// </summary>
    /// <param name="priority">The priority of the event.</param>
    /// <param name="duration">The length of the vibration, in milliseconds.</param>
    /// <param name="strength">The strength of the vibration, from 0f to 1f.</param>
    /// <param name="addToFront">Whether the event should prioritize over other events of the same priority.</param>
    /// <param name="clearOthers">Whether the event should remove all other registered events of its priority.</param>
    public static void AddEvent(VibrationPriority priority, int duration, float strength, bool addToFront, bool clearOthers = false)
    {
        VibrationEvent vibrationEvent = new(duration, strength);
        if (clearOthers)
            EventLists[priority].Clear();
        if (addToFront)
            EventLists[priority].AddFirst(vibrationEvent);
        else
            EventLists[priority].AddLast(vibrationEvent);
        ProcessEvents();
    }

    public static void Halt()
    {
        foreach (var priority in Enum.GetValues(typeof(VibrationPriority))
                                     .Cast<VibrationPriority>()
                                     .OrderByDescending(priority => (int)priority))
        {
            EventLists[priority].Clear();
        }
        StopVibratingAllDevices();
    }

    private static VibrationEvent GetNextEvent(LinkedList<VibrationEvent> eventList)
    {
        // prevent async threads from modifying eventList while potentially removing elements.
        lock (eventList)
        {
            while (eventList.First != null)
            {
                VibrationEvent currentEvent = eventList.First.Value;
                if (!currentEvent.HasPassed())
                    return currentEvent;
                eventList.RemoveFirst();
            }

            return null;
        }
    }

    /// <summary>
    /// Loop through all priorities and pick the first event of highest priority. Then vibrate toys with this event's strength.
    /// </summary>
    private static void ProcessEvents()
    {
        foreach (var priority in Enum.GetValues(typeof(VibrationPriority))
                                     .Cast<VibrationPriority>()
                                     .OrderByDescending(priority => (int)priority))
        {
            VibrationEvent currentEvent = GetNextEvent(EventLists[priority]);
            if (currentEvent == null)
            {
                continue;
            }

            // only vibrate if vibration strength/event changed
            if (_currentEvent == currentEvent) continue;
            _currentEvent = currentEvent;

            int callbackTime = (int)(currentEvent.Timestamp - DateTime.Now).TotalMilliseconds + currentEvent.Duration;
            callbackTime = callbackTime < 0 ? 0 : callbackTime;
            VibrateAllDevices(currentEvent.Strength, callbackTime);

            _processingBusy = false;
            return;
        }

        if (_currentEvent.HasPassed())
        {
            StopVibratingAllDevices();
        }
        _processingBusy = false;
    }

    private static async void VibrateAllDevices(float strength, int callBackTime)
    {
        if (Instance.DebugChatMessages)
        {
            tChat.LogToPlayer($"Vibrating at `{strength}` for `{callBackTime}` msec", Color.Lime);
            if (strength < 0)
            {
                tChat.LogToPlayer("Tried to vibrate at a strength below 0! Clamping.", Color.Red);
                strength = 0;
            }
            if (strength > 1)
            {
                tChat.LogToPlayer("Tried to vibrate at a strength above 1! Clamping.", Color.Red);
                strength = 1;
            }
        }

        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(strength * Instance.VibratorMaxIntensity).ConfigureAwait(false);
        }

        await Task.Delay(callBackTime).ConfigureAwait(false);
        ProcessEvents();
    }

    private static async void StopVibratingAllDevices()
    {
        // Similar to VibrateAllDevices but without calling ProcessEvents afterward
        if (Instance.DebugChatMessages)
            tChat.LogToPlayer("Vibrating at `0`", Color.Lime);

        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(0).ConfigureAwait(false);
        }
    }
}