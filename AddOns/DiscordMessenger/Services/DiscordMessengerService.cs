public class DiscordMessengerService
{
    private readonly EventManager _eventManager;
    private readonly EventLoggingEvents _eventLoggingEvents;
    private readonly TradingStatusEvents _tradingStatusEvents;
    private readonly ControlPanelEvents _controlPanelEvents;
    private readonly HttpClient _httpClient;
    private readonly List<string> _webhookUrls;
    private readonly string _screenshotLocation;
    private readonly int _finalEmbedColor;

    private string _screenshotPath;
    private object _embedContent;

    private readonly MethodInfo _serializeMethod;

    public DiscordMessengerService(
        EventManager eventManager,
        EventLoggingEvents eventLoggingEvents,
        TradingStatusEvents tradingStatusEvents,
        ControlPanelEvents controlPanelEvents)
    {
        _eventManager = eventManager;
        _eventLoggingEvents = eventLoggingEvents;
        _tradingStatusEvents = tradingStatusEvents;
        _controlPanelEvents = controlPanelEvents;

        _httpClient = new HttpClient();
        _webhookUrls = Config.Instance.WebhookUrls;
        _screenshotLocation = Config.Instance.ScreenshotLocation;
        _screenshotPath = "";
        _embedContent = null;

        _finalEmbedColor = GetEmbedColor(Config.Instance.EmbededColor);
        _serializeMethod = LoadSerializeMethod();

        _tradingStatusEvents.OnOrderEntryProcessed += HandleOnOrderEntryProcessed;
        _controlPanelEvents.OnScreenshotProcessed += HandleOnScreenshotProcessed;
        _controlPanelEvents.OnAutoScreenshotProcessedWaiting += HandleOnAutoScreenshotProcessedWaiting;
    }

    private int GetEmbedColor(object colorObject)
    {
        if (colorObject is SolidColorBrush brush)
        {
            var color = brush.Color;
            return (color.R << 16) | (color.G << 8) | color.B;
        }
        return 0xFFFFFF;
    }

    private MethodInfo LoadSerializeMethod()
    {
        var assembly = Assembly.LoadFrom(@"C:\Program Files\NinjaTrader 8\bin\Newtonsoft.Json.dll");
        var jsonConvert = assembly.GetType("Newtonsoft.Json.JsonConvert");
        return jsonConvert.GetMethod("SerializeObject", new[] { typeof(object) });
    }

    private void HandleOnOrderEntryProcessed(List<Position> positions, List<OrderEntry> orderEntries)
    {
        _embedContent = BuildEmbedContent(positions, orderEntries);

        Task.Run(async () =>
        {
            await Task.Delay(500);
            _ = _controlPanelEvents.TakeScreenshot(ProcessType.Auto);
        });
    }

    private async Task HandleOnScreenshotProcessed(ProcessType processType, string screenshotName)
    {
        _screenshotPath = Path.Combine(_screenshotLocation, screenshotName);

        if (processType == ProcessType.Auto)
        {
            _controlPanelEvents.AutoScreenshotProcessedWaiting();
        }
        else
        {
            await SendScreenshotAsync(ReportScreenshotResult);
        }
    }

    private void HandleOnAutoScreenshotProcessedWaiting()
    {
        _ = SendMessageAsync(success =>
        {
            ReportEvent(success, "Trading Status Sent");
            _screenshotPath = "";
            _embedContent = null;
        });
    }

    private async Task SendMessageAsync(Action<bool> callback)
    {
        var payload = new { embeds = new[] { _embedContent } };
        var json = (string)_serializeMethod.Invoke(null, new[] { payload });
        await SendHttpRequestAsync(json, callback);
    }

    private async Task SendScreenshotAsync(Action<bool> callback)
    {
        if (!await EnsureFileExists())
        {
            callback(false);
            return;
        }

        await SendHttpRequestAsync(null, callback);
    }

    private async Task<bool> EnsureFileExists(int retries = 5, int delay = 500)
    {
        for (int i = 0; i < retries; i++)
        {
            if (File.Exists(_screenshotPath)) return true;
            await Task.Delay(delay);
        }
        return false;
    }

    private async Task SendHttpRequestAsync(string jsonPayload, Action<bool> callback)
    {
        try
        {
            foreach (var url in _webhookUrls)
            {
                using var form = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(jsonPayload))
                    form.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "payload_json");

                using var stream = new FileStream(_screenshotPath, FileMode.Open, FileAccess.Read);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                form.Add(fileContent, "file", Path.GetFileName(_screenshotPath));

                var response = await _httpClient.PostAsync(url, form);
                if (!response.IsSuccessStatusCode)
                {
                    callback(false);
                    _eventManager.PrintMessage($"HTTP failed: {response.StatusCode}");
                    return;
                }
            }

            File.Delete(_screenshotPath);
            callback(true);
        }
        catch (Exception ex)
        {
            _eventManager.PrintMessage($"Exception: {ex.Message}");
            callback(false);
        }
    }

    private void ReportEvent(bool success, string action)
    {
        _eventLoggingEvents.SendRecentEvent(new EventLog
        {
            Status = success ? Status.Success : Status.Failed,
            Message = action
        });
    }

    private void ReportScreenshotResult(bool success)
    {
        ReportEvent(success, "Screenshot Sent");
    }

    private object BuildEmbedContent(List<Position> positions, List<OrderEntry> orderEntries)
    {
        var embed = new
        {
            title = "Trading Status",
            color = _finalEmbedColor,
            fields = new List<object>()
        };

        void AddField(string name, string value) =>
            embed.fields.Add(new { name, value = $"```{value}```", inline = false });

        if (!positions.Any())
            AddField("**Positions**", "No Positions");
        else
            foreach (var group in positions.GroupBy(p => p.Instrument))
                AddField($"**{group.Key} Positions**", string.Join("\n", group.Select(p =>
                    $"Quantity: {p.Quantity}\nAvg Price: {p.AveragePrice}\nPosition: {p.MarketPosition}")));

        if (!orderEntries.Any())
            AddField("**Active Orders**", "No Active Orders");
        else
            foreach (var group in orderEntries.GroupBy(o => o.Instrument))
                AddField($"**{group.Key} Active Orders**", string.Join("\n", group.Select(o =>
                    $"Quantity: {o.Quantity}\nPrice: {o.Price}\nAction: {o.Action}\nType: {o.Type}")));

        return embed;
    }
}
