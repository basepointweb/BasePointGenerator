using BasePointGenerator.AppSettings;

namespace BasePointGenerator.ConfigurationSettings
{
    public class AppConfigurationSettings
    {
        public LoggingSettingsDto Logging { get; set; }
        public string AllowedHosts { get; set; }
        public AppSettingsDto AppSettings { get; set; }
    }
}
