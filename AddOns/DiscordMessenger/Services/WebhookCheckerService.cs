using NinjaTrader.Custom.AddOns.DiscordMessenger.Configs;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Events;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Services
{
    public class WebhookCheckerService
    {
        private readonly EventManager _eventManager;
        private readonly WebhookCheckerEvents _webhookCheckerEvents;
        private readonly EventLoggingEvents _eventLoggingEvents;

        private readonly HttpClient _httpClient;
        private Timer _timer;
        private List<string> _webhookUrls;

        private const int CheckIntervalMilliseconds = 60000;

        public WebhookCheckerService(
            EventManager eventManager,
            WebhookCheckerEvents webhookCheckerEvents,
            EventLoggingEvents eventLoggingEvents)
        {
            _eventManager = eventManager;
            _webhookCheckerEvents = webhookCheckerEvents;
            _eventLoggingEvents = eventLoggingEvents;

            _httpClient = new HttpClient();
            _webhookUrls = Config.Instance.WebhookUrls;

            _webhookCheckerEvents.OnStartWebhookChecker += StartChecking;
            _webhookCheckerEvents.OnStopWebhookChecker += StopChecking;
        }

        private void StartChecking()
        {
            _timer = new Timer(async _ => await CheckWebhooksAsync(), null, 0, CheckIntervalMilliseconds);
        }

        private void StopChecking()
        {
            _timer?.Dispose();
            _httpClient?.Dispose();
        }

        private async Task CheckWebhooksAsync()
        {
            int successCount = 0;
            List<string> failedUrls = new();

            foreach (var url in _webhookUrls)
            {
                if (await IsWebhookValidAsync(url))
                {
                    successCount++;
                }
                else
                {
                    failedUrls.Add(url);
                }
            }

            ReportStatus(successCount, failedUrls);
        }

        private async Task<bool> IsWebhookValidAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void ReportStatus(int successCount, List<string> failedUrls)
        {
            int total = _webhookUrls.Count;
            Status status = successCount switch
            {
                0 => Status.Failed,
                var s when s == total => Status.Success,
                _ => Status.PartialSuccess
            };

            if (status == Status.PartialSuccess)
            {
                foreach (var url in failedUrls)
                {
                    _eventManager.PrintMessage($"Webhook Failed: {url}");
                }

                _eventLoggingEvents.SendRecentEvent(new EventLog
                {
                    Status = status,
                    Message = "Some webhooks failed."
                });
            }

            _webhookCheckerEvents.UpdateWebhookStatus(status);
        }
    }
}
