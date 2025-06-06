using NinjaTrader.Custom.AddOns.DiscordMessenger.Configs;
using System;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Events
{
    /// <summary>
    /// Manages events related to the lifecycle and status of a webhook checker.
    /// </summary>
    public class WebhookCheckerEvents
    {
        private readonly EventManager _eventManager;

        /// <summary>
        /// Triggered when the webhook checker starts.
        /// </summary>
        public event Action OnWebhookCheckerStarted;

        /// <summary>
        /// Triggered when the webhook checker stops.
        /// </summary>
        public event Action OnWebhookCheckerStopped;

        /// <summary>
        /// Triggered when the webhook status is updated.
        /// </summary>
        public event Action<Status> WebhookStatusUpdated;

        /// <summary>
        /// Initializes a new instance with the given event manager.
        /// </summary>
        /// <param name="eventManager">The event manager to use for invoking events.</param>
        public WebhookCheckerEvents(EventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        /// <summary>
        /// Invokes the start webhook checker event.
        /// </summary>
        public void TriggerStart()
        {
            _eventManager.InvokeEvent(OnWebhookCheckerStarted);
        }

        /// <summary>
        /// Invokes the stop webhook checker event.
        /// </summary>
        public void TriggerStop()
        {
            _eventManager.InvokeEvent(OnWebhookCheckerStopped);
        }

        /// <summary>
        /// Invokes the webhook status update event.
        /// </summary>
        /// <param name="status">The new webhook status.</param>
        public void TriggerStatusUpdate(Status status)
        {
            _eventManager.InvokeEvent(WebhookStatusUpdated, status);
        }
    }
}
