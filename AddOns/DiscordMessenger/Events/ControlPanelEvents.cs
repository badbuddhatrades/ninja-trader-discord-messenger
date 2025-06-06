using NinjaTrader.Custom.AddOns.DiscordMessenger.Configs;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Models;
using System;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Events
{
    public class ControlPanelEvents
    {
        private readonly EventManager _eventManager;

        public event Action<Status> StatusUpdated;
        public event Action<EventLog> EventLogUpdated;
        public event Action<AutoMode> AutoModeToggled;
        public event Func<ProcessType, Task> ScreenshotRequested;
        public event Func<ProcessType, string, Task> ScreenshotHandled;
        public event Action AutoScreenshotAwaitingProcessing;

        public ControlPanelEvents(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        public void UpdateEventLog(EventLog eventLog) =>
            _eventManager.InvokeEvent(EventLogUpdated, eventLog);

        public void UpdateStatus(Status status) =>
            _eventManager.InvokeEvent(StatusUpdated, status);

        public void ToggleAutoMode(AutoMode mode) =>
            _eventManager.InvokeEvent(AutoModeToggled, mode);

        public async Task RequestScreenshotAsync(ProcessType processType)
        {
            if (ScreenshotRequested != null)
            {
                await ScreenshotRequested.Invoke(processType);
            }
        }

        public async Task HandleScreenshotAsync(ProcessType processType, string screenshotName)
        {
            if (ScreenshotHandled != null)
            {
                await ScreenshotHandled.Invoke(processType, screenshotName);
            }
        }

        public void NotifyAutoScreenshotPending() =>
            _eventManager.InvokeEvent(AutoScreenshotAwaitingProcessing);
    }

    public enum AutoMode
    {
        Enabled,
        Disabled
    }
}
