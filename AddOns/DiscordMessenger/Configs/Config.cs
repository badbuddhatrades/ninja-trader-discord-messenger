using NinjaTrader.Cbi;
using System.Collections.Generic;
using System.Windows.Media;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Configs
{
    public class Config
    {
        private static readonly Config _instance = new Config();

        private Config()
        {
            WebhookUrls = new List<string>();
            ScreenshotLocation = string.Empty;
            EmbededColor = Brushes.Transparent;
        }

        public static Config Instance => _instance;

        public List<string> WebhookUrls { get; private set; }
        public Account Account { get; set; }
        public string AccountName { get; set; }
        public string ScreenshotLocation { get; set; }
        public Brush EmbededColor { get; set; }
    }
}
