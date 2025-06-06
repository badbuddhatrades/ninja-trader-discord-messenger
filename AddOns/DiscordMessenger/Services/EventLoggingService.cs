using NinjaTrader.Custom.AddOns.DiscordMessenger.Events;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Models;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Services
{
    public class EventLoggingService
    {
        private readonly EventLoggingEvents _eventLoggingEvents;
        private readonly Queue<EventLog> _eventLogs;

        private const int MaxLogCount = 5;

        public EventLoggingService(EventLoggingEvents eventLoggingEvents)
        {
            _eventLoggingEvents = eventLoggingEvents;
            _eventLogs = new Queue<EventLog>(MaxLogCount);

            _eventLoggingEvents.OnSendRecentEvent += OnSendRecentEvent;
        }

        private void OnSendRecentEvent(EventLog eventLog)
        {
            AddEventLog(eventLog);
            _eventLoggingEvents.RecentEventProcessed(new List<EventLog>(_eventLogs));
        }

        private void AddEventLog(EventLog log)
        {
            if (_eventLogs.Count >= MaxLogCount)
            {
                _eventLogs.Dequeue();
            }
            _eventLogs.Enqueue(log);
        }
    }
}
