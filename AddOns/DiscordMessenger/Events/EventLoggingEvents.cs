using NinjaTrader.Custom.AddOns.DiscordMessenger.Models;
using System;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Events
{
    public interface IEventLogger
    {
        void TriggerSendRecentEvent(EventLog eventLog);
        void TriggerRecentEventsProcessed(List<EventLog> eventLogs);
        void SubscribeToSendRecentEvent(Action<EventLog> handler);
        void SubscribeToRecentEventsProcessed(Action<List<EventLog>> handler);
    }

    public class EventLogger : IEventLogger
    {
        private readonly EventManager _eventManager;
        private event Action<EventLog> SendRecentEvent;
        private event Action<List<EventLog>> RecentEventsProcessed;

        public EventLogger(EventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        public void TriggerSendRecentEvent(EventLog eventLog)
        {
            _eventManager.InvokeEvent(SendRecentEvent, eventLog);
        }

        public void TriggerRecentEventsProcessed(List<EventLog> eventLogs)
        {
            _eventManager.InvokeEvent(RecentEventsProcessed, eventLogs);
        }

        public void SubscribeToSendRecentEvent(Action<EventLog> handler)
        {
            SendRecentEvent += handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void SubscribeToRecentEventsProcessed(Action<List<EventLog>> handler)
        {
            RecentEventsProcessed += handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }
}
