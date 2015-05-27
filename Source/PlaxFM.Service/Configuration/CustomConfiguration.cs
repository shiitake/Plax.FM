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
using PlaxFm.Models;

namespace PlaxFm.Configuration
{
    public interface ICustomConfiguration
    {
        void AddUser(string username, int plexId);
        string GetValue(string settingName, int plexId = 1);
        void SetValue(string settingName, string settingValue, int plexId = 1);
    }
    
    public class CustomConfiguration : ICustomConfiguration
    {
        private readonly IAppSettings _appSettings;
        private readonly ILogger _logger;
        private DataSet _storage;
        private readonly string _configFile;
        private readonly string _schemaFile;

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
            else
            {
                Create();
            }
            ConfirmSetup();
        }

        private void Create()
        {
            var configInfo = new FileInfo(_configFile);
            var directory = new DirectoryInfo(configInfo.Directory.ToString());
            if (!directory.Exists)
            {
                directory.Create();
            }
            //create config Dataset
            var config = new DataSet("UserConfiguration");
            
            //create Setup table
            var userInit = new DataTable("Setup");
            var initialized = new DataColumn("Initialized", typeof (bool), "", MappingType.Attribute);
            userInit.Columns.Add(initialized);
            var initRow = userInit.NewRow();
            initRow["Initialized"] = false;
            userInit.Rows.Add(initRow);
            
            //create User table
            var userConfig = new DataTable("User");
            var plexId = new DataColumn("PlexId", typeof(string), "", MappingType.Attribute);
            userConfig.Columns.Add(plexId);
            var plexUsername = new DataColumn("PlexUsername", typeof (string), "", MappingType.Attribute);
            userConfig.Columns.Add(plexUsername);
            userConfig.Columns.Add("LastFmUsername");
            userConfig.Columns.Add("SessionId");
            userConfig.Columns.Add("Token");
            var row = userConfig.NewRow();
            row["PlexId"] = "1";
            row["PlexUsername"] = string.Empty;
            row["LastFmUsername"] = string.Empty;
            row["SessionId"] = string.Empty;
            row["Token"] = string.Empty;
            userConfig.Rows.Add(row);
            
            //add tables to dataset
            config.Tables.Add(userInit);
            config.Tables.Add(userConfig);
            
            //write dataset to config files
            config.WriteXmlSchema(_schemaFile);
            config.WriteXml(_configFile);
            _storage = config;
        }

        private void Save()
        {
            _storage.WriteXmlSchema(_schemaFile);
            _storage.WriteXml(_configFile);
        }

        private async void ConfirmSetup()
        {
            var setup = _storage.Tables["Setup"].Rows[0]["Initialized"].Equals(true);
            if (!setup)
            {
                _logger.Info("Beginning initial setup. ");
                var lastFm = new LastFmScrobbler(_logger, _appSettings, this);
                var session = await lastFm.GetSession();
                if (session != "")
                {
                    _logger.Info("Initial setup completed successfully!");
                    _storage.Tables["Setup"].Rows[0]["Initialized"] = true;
                    Save();
                }
                else
                {
                    _logger.Error("There was a problem setting up your account. Please stop and restart the service.");
                }
            }
        }

        public void AddUser(string username, int plexId)
        {
            var row = _storage.Tables["User"].NewRow();
            row["PlexId"] = plexId.ToString();
            row["PlexUsername"] = username;
            _storage.Tables["User"].Rows.Add(row);
            Save();
        }

        public void DeleteUser(string username)
        {
            var row = GetRowByUsername(username);
            DeleteRow(row);
        }

        public void DeleteUser(int plexId)
        {
            var row = GetRowById(plexId);
            DeleteRow(row);
        }
        
        public string GetValue(string settingName, int plexId = 1)
        {
            int row = GetRowById(plexId);
            return _storage.Tables["User"].Rows[row][settingName].ToString();
        }

        public void SetValue(string settingName, string settingValue, int plexId = 1)
        {
            int row = GetRowById(plexId);

            _storage.Tables["User"].Rows[row][settingName] = settingValue;
            Save();
        }

        private int GetRowById(int plexId = 1)
        {
            int row = 0;
            for (int i = 0; i < _storage.Tables["User"].Rows.Count; i++)
            {
                if (_storage.Tables["User"].Rows[i]["PlexId"].ToString() == plexId.ToString())
                { row = i; }
            }
            return row;
        }

        private int GetRowByUsername(string username)
        {
            int row = 0;
            for (int i = 0; i < _storage.Tables["User"].Rows.Count; i++)
            {
                if (_storage.Tables["User"].Rows[i]["PlexUsername"].ToString() == username)
                { row = i; }
            }
            return row;
        }
        
        private void DeleteRow(int row)
        {
            _storage.Tables["User"].Rows[row].Delete();
            Save();
        }
    }
}
