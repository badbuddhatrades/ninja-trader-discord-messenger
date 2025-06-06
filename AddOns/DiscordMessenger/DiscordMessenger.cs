#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Configs;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Events;
using NinjaTrader.Custom.AddOns.DiscordMessenger.Services;
using NinjaTrader.Gui;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Timers;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class DiscordMessenger : Indicator
    {
        public const string GROUP_NAME = "Discord Messenger";
        private static readonly string DefaultAccount = "Sim101";
        private static readonly string DefaultScreenshotPath = @"C:\screenshots";
        private static readonly Brush DefaultEmbedColor = Brushes.DodgerBlue;
        private static readonly int DebounceIntervalMs = 300;

        private Brush _embededColor;
        private bool _autoSend;
        private bool _orderUpdateTriggered;
        private Timer _debounceTimer;

        private EventManager _eventManager;
        private ControlPanelEvents _controlPanelEvents;
        private WebhookCheckerEvents _webhookCheckerEvents;
        private TradingStatusEvents _tradingStatusEvents;
        private EventLoggingEvents _eventLoggingEvents;

        #region Properties

        [NinjaScriptProperty]
        [Display(Name = "Version", Order = 0, GroupName = GROUP_NAME)]
        [ReadOnly(true)]
        public string Version => "2.0.0";

        [NinjaScriptProperty]
        [Display(Name = "Webhook URLs", Description = "Comma-separated Discord webhook URLs.", Order = 1, GroupName = GROUP_NAME)]
        public string WebhookUrls { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Account Name", Description = "Account name for posting messages.", Order = 2, GroupName = GROUP_NAME)]
        public string AccountName { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Screenshot Location", Description = "Directory for saving screenshots.", Order = 3, GroupName = GROUP_NAME)]
        public string ScreenshotLocation { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Embed Color", Description = "Color for the Discord embed.", Order = 4, GroupName = GROUP_NAME)]
        public Brush EmbededColor
        {
            get => _embededColor;
            set => _embededColor = value;
        }

        [Browsable(false)]
        public string EmbededColorSerialize
        {
            get => Serialize.BrushToString(_embededColor);
            set => _embededColor = Serialize.StringToBrush(value);
        }

        #endregion

        protected override void OnStateChange()
        {
            switch (State)
            {
                case State.SetDefaults:
                    InitializeDefaults();
                    break;
                case State.Configure:
                    Configure();
                    break;
                case State.DataLoaded:
                    LoadControlPanel();
                    break;
                case State.Realtime:
                    _webhookCheckerEvents?.StartWebhookChecker();
                    break;
                case State.Terminated:
                    Cleanup();
                    break;
            }
        }

        private void InitializeDefaults()
        {
            Description = @"Send account positions and active orders to Discord channels.";
            Name = "Discord Messenger";
            Calculate = Calculate.OnEachTick;
            IsOverlay = true;
            DisplayInDataBox = false;
            DrawOnPricePanel = false;
            DrawHorizontalGridLines = false;
            DrawVerticalGridLines = false;
            PaintPriceMarkers = false;
            ScaleJustification = ScaleJustification.Right;
            IsSuspendedWhileInactive = true;

            WebhookUrls = "";
            AccountName = DefaultAccount;
            ScreenshotLocation = DefaultScreenshotPath;
            EmbededColor = DefaultEmbedColor;
        }

        private void Configure()
        {
            _autoSend = true;
            _orderUpdateTriggered = false;

            SetupDebounceTimer();

            var account = Account.All.FirstOrDefault(a => a.Name == AccountName);
            if (account == null)
            {
                Print("Account not found.");
                return;
            }

            account.OrderUpdate += OnOrderUpdate;
            ApplyConfiguration(account);
            InitializeEventSystem();
        }

        private void Cleanup()
        {
            UnloadControlPanel();
            _webhookCheckerEvents?.StopWebhookChecker();
        }

        private void SetupDebounceTimer()
        {
            _debounceTimer = new Timer(DebounceIntervalMs)
            {
                AutoReset = false
            };
            _debounceTimer.Elapsed += (s, e) => _orderUpdateTriggered = true;
        }

        private void ApplyConfiguration(Account account)
        {
            Config.Instance.WebhookUrls = ParseWebhookUrls(WebhookUrls);
            Config.Instance.Account = account;
            Config.Instance.AccountName = AccountName;
            Config.Instance.ScreenshotLocation = ScreenshotLocation.TrimEnd('\\');
            Config.Instance.EmbededColor = EmbededColor;
        }

        private void InitializeEventSystem()
        {
            _eventManager = new EventManager();
            _eventManager.OnPrintMessage += Print;

            _controlPanelEvents = new ControlPanelEvents(_eventManager);
            _controlPanelEvents.OnAutoButtonClicked += ToggleAutoSend;
            _controlPanelEvents.OnTakeScreenshot += HandleScreenshot;

            _webhookCheckerEvents = new WebhookCheckerEvents(_eventManager);
            _webhookCheckerEvents.OnWebhookStatusUpdated += HandleOnWebhookStatusUpdated;

            _tradingStatusEvents = new TradingStatusEvents(_eventManager);
            _eventLoggingEvents = new EventLoggingEvents(_eventManager);

            new WebhookCheckerService(_eventManager, _webhookCheckerEvents, _eventLoggingEvents);
            new TradingStatusService(_tradingStatusEvents);
            new EventLoggingService(_eventLoggingEvents);
            new DiscordMessengerService(_eventManager, _eventLoggingEvents, _tradingStatusEvents, _controlPanelEvents);
        }

        private void ToggleAutoSend(bool enabled)
        {
            _autoSend = enabled;

            if (enabled)
                _tradingStatusEvents.OrderEntryUpdatedSubscribe();
            else
                _tradingStatusEvents.OrderEntryUpdatedUnsubscribe();
        }

        private List<string> ParseWebhookUrls(string input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? new List<string>()
                : input.Split(',').Select(url => url.Trim()).ToList();
        }

        private void OnOrderUpdate(object sender, OrderEventArgs e)
        {
            if (_autoSend)
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        protected override void OnBarUpdate()
        {
            if (State != State.Realtime || !_autoSend || !_orderUpdateTriggered)
                return;

            _orderUpdateTriggered = false;
            _tradingStatusEvents.UpdateOrderEntry();
        }

        public override string DisplayName => "Discord Messenger";
    }
}
