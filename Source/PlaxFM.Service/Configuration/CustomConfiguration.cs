using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Xml.Schema;
using Ninject.Extensions.Logging;
using PlaxFm.Core.Utilities;
using PlaxFm.Models;

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
        private readonly IAppSettings _appSettings;
        private readonly ILogger _logger;
        private DataSet _storage;
        private readonly string _configFile;
        private readonly string _schemaFile;
        private ConfigHelper _config;

        public CustomConfiguration(IAppSettings appSettings, ILogger logger)
        {
            _appSettings = appSettings;
            _logger = logger;
            _configFile = Environment.ExpandEnvironmentVariables(_appSettings.ConfigFile);
            _schemaFile = Environment.ExpandEnvironmentVariables(_appSettings.SchemaFile);
            var configInfo = new FileInfo(_configFile);
            if (configInfo.Exists)
            {
                _storage = new DataSet("UserConfiguration");
                _storage.ReadXmlSchema(_schemaFile);
                _storage.ReadXml(_configFile);
            }
            _config = new ConfigHelper(_storage, _configFile, _schemaFile);
        }

        private void Init()
        {
            _storage = _config.LoadConfig();
        }

        public bool UserConfirmed(int plexId = 1)
        {
            Init();
            var plex = _config.GetValue("PlexToken") != "";
            var init = _config.GetValue("Setup", "Initialized") == "true";
            var auth = _config.GetValue("Authorized") == "true";
            return (plex && init && auth);
        }
        
        public void AddUser(string username, int plexId)
        {
            Init();
            var row = _storage.Tables["User"].NewRow();
            row["PlexId"] = plexId.ToString();
            row["PlexUsername"] = username;
            _storage.Tables["User"].Rows.Add(row);
            _config.Save(_storage);
        }

        public void DeleteUser(string username)
        {
            var row = _config.GetRowByUsername(username);
            _config.DeleteRow(row);
        }

        public void DeleteUser(int plexId)
        {
            var row = _config.GetRowById(plexId);
            _config.DeleteRow(row);
        }
        
        public string GetValue(string settingName, int plexId = 1)
        {
            return _config.GetValue(settingName, plexId);
        }

        public void SetValue(string settingName, string settingValue, int plexId = 1)
        {
            _config.SetValue(settingName, settingValue, plexId);
        }
    }
}
