using NinjaTrader.Cbi;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Configs;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using OrderEntry = NinjaTrader.Custom.AddOns.DiscordMessenger.Models.OrderEntry;
using Position = NinjaTrader.Custom.AddOns.DiscordMessenger.Models.Position;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Services
{
    public class TradingStatusService
    {
        private readonly TradingStatusEvents _tradingStatusEvents;
        private readonly Account _account;

        private List<Position> _positions = new();
        private List<OrderEntry> _orderEntries = new();

        public TradingStatusService(TradingStatusEvents tradingStatusEvents)
        {
            _tradingStatusEvents = tradingStatusEvents;
            _account = Config.Instance.Account;

            _tradingStatusEvents.OnManualOrderEntryUpdate += TriggerOrderUpdate;
            _tradingStatusEvents.OnOrderEntryUpdated += TriggerOrderUpdate;
            _tradingStatusEvents.OnOrderEntryUpdatedSubscribe += SubscribeToOrderUpdates;
            _tradingStatusEvents.OnOrderEntryUpdatedUnsubscribe += UnsubscribeFromOrderUpdates;
        }

        private void SubscribeToOrderUpdates()
        {
            _tradingStatusEvents.OnOrderEntryUpdated += TriggerOrderUpdate;
        }

        private void UnsubscribeFromOrderUpdates()
        {
            _tradingStatusEvents.OnOrderEntryUpdated -= TriggerOrderUpdate;
        }

        private void TriggerOrderUpdate()
        {
            UpdateOrderEntries();
            UpdatePositions();
            _tradingStatusEvents.OrderEntryProcessed(_positions, _orderEntries);
        }

        private void UpdatePositions()
        {
            _positions = _account.Positions.Select(pos => new Position
            {
                Instrument = pos.Instrument.MasterInstrument.Name,
                Quantity = pos.Quantity,
                AveragePrice = Math.Round(pos.AveragePrice, 2),
                MarketPosition = pos.MarketPosition.ToString()
            }).ToList();
        }

        private void UpdateOrderEntries()
        {
            _orderEntries.Clear();

            foreach (var order in _account.Orders)
            {
                if (order.OrderState != OrderState.Accepted && order.OrderState != OrderState.Working)
                    continue;

                double price = GetOrderPrice(order);

                var existing = _orderEntries.FirstOrDefault(e => e.Type == order.OrderType.ToString() && e.Price == price);
                if (existing != null)
                {
                    existing.Quantity += order.Quantity;
                }
                else
                {
                    _orderEntries.Add(new OrderEntry
                    {
                        Instrument = order.Instrument.MasterInstrument.Name,
                        Quantity = order.Quantity,
                        Price = Math.Round(price, 2),
                        Type = order.OrderType.ToString(),
                        Action = order.OrderAction.ToString()
                    });
                }
            }

            _orderEntries = _orderEntries.OrderByDescending(e => e.Price).ToList();
        }

        private double GetOrderPrice(Order order) =>
            order.OrderType switch
            {
                OrderType.StopLimit or OrderType.StopMarket or OrderType.MIT => order.StopPrice,
                _ => order.LimitPrice
            };
    }
}
