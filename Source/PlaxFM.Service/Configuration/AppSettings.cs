using System;
using System.Configuration;
using NLog;

namespace PlaxFm.Configuration
{
    public class AppSettings : IAppSettings
    {
        private readonly Logger _logger;
        public int JobInterval { get; set; }
        public string LastFmApiKey { get; set; }
        public string LastFmApiSecret { get; set; }
        public string LogCache { get; set; }
        public string PlexLog { get; set; }
        public string WorkingCopy { get; set; }
        public string ConfigFile { get; set; }
        public string SchemaFile { get; set; }
        public string PlexServer { get; set; }
        public int MaxTimeout { get; set; }
        public bool UseConfigFile { get; set; }
        public string ConfigLocation { get; set; }

        public AppSettings()
        {
            _logger = LogManager.GetCurrentClassLogger();
            JobInterval = GetAppSetting<int>("JobInterval", 600);
            LastFmApiKey = GetAppSetting<string>("LastFmApiKey");
            LastFmApiSecret = GetAppSetting<string>("LastFmApiSecret");
            LogCache = GetAppSetting<string>("LogCache");
            PlexLog = GetAppSetting<string>("PlexLog");
            WorkingCopy = GetAppSetting<string>("WorkingCopy");
            ConfigFile = GetAppSetting<string>("ConfigFile");
            SchemaFile = GetAppSetting<string>("SchemaFile");
            PlexServer = GetAppSetting<string>("PlexServer");
            MaxTimeout = GetAppSetting<int>("MaxTimeout", 10000);
            UseConfigFile = GetAppSetting<bool>("UseConfigFile", true);
            ConfigLocation = GetAppSetting<string>("ConfigLocation");
        }

        private T GetAppSetting<T>(string key)
        {
            var appSetting = ConfigurationManager.AppSettings[key];

            if (String.IsNullOrEmpty(appSetting))
            {
                _logger.Fatal("AppSetting '{0}' does not exist or is empty.", key);
                throw new ConfigurationErrorsException(string.Format("AppSetting '{0}' does not exist or is empty.", key));
            }

            try
            {
                return (T)Convert.ChangeType(appSetting, typeof(T));
            }
            catch
            {
                _logger.Fatal("AppSetting '{0}' must be of type {1}.", key, typeof(T).Name);
                throw new ConfigurationErrorsException(string.Format("AppSetting '{0}' must be of type {1}.", key, typeof(T).Name));
            }
        }

        private T GetAppSetting<T>(string key, T defaultValue)
        {
            var appSetting = ConfigurationManager.AppSettings[key];

            if (String.IsNullOrEmpty(appSetting))
            {
                _logger.Warn("AppSetting '{0}' does not exist or is empty.  Defaulting To: {1}", key, defaultValue);
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(appSetting, typeof(T));
            }
            catch
            {
                _logger.Warn("AppSetting '{0}' must be of type {1}.  Defaulting To: {2}", key, typeof(T).Name, defaultValue);
            }

            return defaultValue;
        }
    }
}
