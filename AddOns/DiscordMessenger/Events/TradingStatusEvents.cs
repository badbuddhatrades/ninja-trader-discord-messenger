using NinjaTrader.Custom.AddOns.DiscordMessenger.Models;
using System;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Events
{
    public class TradingStatusEvents
    {
        private readonly EventManager _eventManager;

        public event Action OrderEntryUpdated;
        public event Action ManualOrderEntryUpdated;
        public event Action<List<Position>, List<OrderEntry>> OrderEntryProcessed;
        public event Action OrderEntryUpdateSubscribed;
        public event Action OrderEntryUpdateUnsubscribed;

        public TradingStatusEvents(EventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        public void NotifyOrderEntryUpdated() =>
            _eventManager.InvokeEvent(OrderEntryUpdated);

        public void NotifyManualOrderEntryUpdated() =>
            _eventManager.InvokeEvent(ManualOrderEntryUpdated);

        public void NotifyOrderEntryProcessed(List<Position> positions, List<OrderEntry> orderEntries) =>
            _eventManager.InvokeEvent(OrderEntryProcessed, positions, orderEntries);

        public void NotifyOrderEntryUpdateSubscribed() =>
            _eventManager.InvokeEvent(OrderEntryUpdateSubscribed);

        public void NotifyOrderEntryUpdateUnsubscribed() =>
            _eventManager.InvokeEvent(OrderEntryUpdateUnsubscribed);
    }
}
