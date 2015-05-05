using System;
using System.Configuration;
using NLog;

namespace PlexScrobble.Configuration
{
    public class AppSettings : IAppSettings
    {
        private readonly Logger _logger;
        public string CronSchedule { get; set; }
        public string LastFmApiKey { get; set; }
        public string LastFmApiSecret { get; set; }

        public AppSettings()
        {
            _logger = LogManager.GetCurrentClassLogger();
            CronSchedule = GetAppSetting<string>("CronSchedule");
            LastFmApiKey = GetAppSetting<string>("LastFmApiKey");
            LastFmApiSecret = GetAppSetting<string>("LastFmApiSecret");
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
