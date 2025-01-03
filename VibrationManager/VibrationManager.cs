﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using static Viberaria.ViberariaConfig;
using static Viberaria.bClient;

namespace Viberaria.VibrationManager;

public static class VibrationManager
{
    private static VibrationEvent _currentEvent = null;
    private static readonly Dictionary<VibrationPriority, LinkedList<VibrationEvent>> EventLists = new();
    private static bool _processingBusy = false;
    private static float _currentStrength = 0f;
    private static object _currentStrengthLock = new();

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
        // Todo: There is likely a crash when locking the list if the intiface server stops while connected
        //  "NullReferenceException" at Viberaria.VibrationManager.VibrationManager.GetNextEvent(LinkedList`1 eventList)
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
        if (_processingBusy) return;
        _processingBusy = true;
        string logMsg = "Iterating events.";

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
            if (_currentEvent == currentEvent)
            {
                _processingBusy = false;
                if (Instance.DebugChatMessages) tChat.LogToPlayer(logMsg + " Event ongoing.", Color.GreenYellow);
                return;
            }
            _currentEvent = currentEvent;

            int callbackTime = (int)(currentEvent.Timestamp - DateTime.Now).TotalMilliseconds + currentEvent.Duration;
            if (callbackTime <= 0) continue;

            // first unset busy, then vibrate. In case async VibrateAllDevices somehow finished before _busy is set
            // to false. Then it would not re-run ProcessEvents() and never call StopVibratingAllDevices.
            _processingBusy = false;
            if (Instance.DebugChatMessages) tChat.LogToPlayer(logMsg + " Event found.", Color.GreenYellow);
            VibrateAllDevices(currentEvent.Strength, callbackTime);
            return;
        }

        if (Instance.DebugChatMessages) tChat.LogToPlayer(logMsg + " Events passed! :D", Color.GreenYellow);
        if (_currentEvent.HasPassed())
        {
            StopVibratingAllDevices();
        }
        _processingBusy = false;
    }

    /// <summary>
    /// Vibrate all connected toys at a given strength for a given time, after which it calls ProcessEvents to get
    /// the next up vibration (eg. a lower priority event).
    /// </summary>
    /// <param name="strength">How strong the toys should vibrate.</param>
    /// <param name="callBackTime">How long to vibrate the toy.</param>
    private static async void VibrateAllDevices(float strength, int callBackTime)
    {
        lock (_currentStrengthLock)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_currentStrength != strength)
            {
                // lower the amount of chat spam
                _currentStrength = strength;
                if (Instance.DebugChatMessages)
                {
                    tChat.LogToPlayer($"Vibrating at `{strength}` for `{callBackTime}` msec", Color.Lime);
                    // safeguard to prevent crash from out of bounds strength.
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

                TryVibrateAllDevices(strength);
            }
        }

        await Task.Delay(callBackTime);
        if (Instance.DebugChatMessages) tChat.LogToPlayer($"  Event `{strength},{callBackTime}` finished.", Color.GreenYellow);
        ProcessEvents();
    }

    /// <summary>
    /// A helper function to handle Intiface errors when vibrating toys.
    /// </summary>
    /// <param name="strength">The strength to vibrate toys at.</param>
    private static async void TryVibrateAllDevices(float strength)
    {
        try
        {
            foreach (var device in _client.Devices)
            {
                await device.VibrateAsync(strength * Instance.VibratorMaxIntensity);
            }
        }
        catch (Buttplug.Core.ButtplugException ex)
        {
            tChat.LogToPlayer($"Error trying to vibrate plug(s) with strength `{strength}`! \"{ex.Message}\"",
                Color.Red);
            ModContent.GetInstance<Viberaria>().Logger.ErrorFormat("Couldn't vibrate plug(s) on strength `{0}`:\n{1}",
                strength, ex.StackTrace);
        }
        catch (Exception ex)
        {
            // todo
            ModContent.GetInstance<Viberaria>().Logger.FatalFormat("UNHANDLED EXCEPTION while trying to vibrate plug(s) on strength `{0}`:\n{1}", strength, ex.StackTrace);
        }
    }

    /// <summary>
    /// A helper function to handle resetting the vibration manager and setting all toys to 0 strength
    /// </summary>
    private static void StopVibratingAllDevices()
    {
        lock (_currentStrengthLock)
        {
            // lower the amount of chat spam
            if (_currentStrength != 0)
            {
                _currentStrength = 0;

                // Similar to VibrateAllDevices but without calling ProcessEvents afterward
                if (Instance.DebugChatMessages)
                    tChat.LogToPlayer("Vibrating at `0`", Color.Lime);
            }

            TryVibrateAllDevices(0);
        }
    }
}