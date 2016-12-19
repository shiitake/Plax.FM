using System;
using System.Data;
using System.IO;
using Ninject.Extensions.Logging;
using PlaxFm.Core.Utilities;

namespace PlaxFm.Configuration
{
    public interface ICustomConfiguration
    {
        void AddUser(string username, int plexId);
        string GetValue(string settingName, int plexId = 1);
        void SetValue(string settingName, string settingValue, int plexId = 1);
        bool UserConfirmed(int plexId = 1);
    }
    
    public class CustomConfiguration : ICustomConfiguration
    {
        private readonly ILogger _logger;
        private DataSet _storage;
        private readonly ConfigHelper _config;

        public CustomConfiguration(IAppSettings appSettings, ILogger logger)
        {
            var settings = appSettings;
            _logger = logger;
            var configLocation = Environment.ExpandEnvironmentVariables(settings.ConfigLocation);
            _config = new ConfigHelper(configLocation);

            //if (settings.UseConfigFile)
            //{
            //    var configFile = Environment.ExpandEnvironmentVariables(settings.ConfigFile);
            //    var schemaFile = Environment.ExpandEnvironmentVariables(settings.SchemaFile);
            //    var configInfo = new FileInfo(configFile);
            //    if (configInfo.Exists)
            //    {
            //        _storage = new DataSet("UserConfiguration");
            //        _storage.ReadXmlSchema(schemaFile);
            //        _storage.ReadXml(configFile);
            //    }
            //    _config = new ConfigHelper(configFile, schemaFile);
            //}
            //else
            //{
            //    var configLocation = Environment.ExpandEnvironmentVariables(settings.ConfigLocation);
            //    _config = new ConfigHelper(configLocation);
            //}
            _storage = _config.GetStorage();
        }

        private void Init()
        {
            _storage = _config.LoadConfig();
        }

        public bool UserConfirmed(int plexId = 1)
        {
            Init();
            _logger.Info("Checking user configuration");
            var plex = _config.GetValue("PlexToken") != "";
            var init = _config.GetValue("Setup", "Initialized").ToLower() == "true";
            var auth = _config.GetValue("Authorized").ToLower() == "true";
            return (plex && init && auth);
        }
        
        public void AddUser(string username, int plexId)
        {
            Init();
            _logger.Info("Adding new user.");
            var row = _storage.Tables["User"].NewRow();
            row["PlexId"] = plexId.ToString();
            row["PlexUsername"] = username;
            _storage.Tables["User"].Rows.Add(row);
            _config.Save(_storage);
        }

        public void DeleteUser(string username)
        {
            _logger.Info("Deleting user " + username);
            var row = _config.GetRowByUsername(username);
            _config.DeleteRow(row);
        }

        public void DeleteUser(int plexId)
        {
            _logger.Info("Deleteing user " + plexId);
            var row = _config.GetRowById(plexId);
            _config.DeleteRow(row);
        }
        
        public string GetValue(string settingName, int plexId = 1)
        {
            return _config.GetValue(settingName, plexId);
        }

        public string GetValue(string tableName, string settingName, int plexId = 1)
        {
            return _config.GetValue(tableName, settingName, plexId);
        }

        public void SetValue(string settingName, string settingValue, int plexId = 1)
        {
            _config.SetValue(settingName, settingValue, plexId);
        }
    }
}
