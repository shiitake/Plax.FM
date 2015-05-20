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

namespace PlexScrobble.Configuration
{
    public interface ICustomConfiguration
    {
        void AddUser(string username, int plexId);
        string GetValue(string settingName, int plexId = 1);
        void SetValue(string settingName, string settingValue, int plexId = 1);
    }
    
    public class CustomConfiguration : ICustomConfiguration
    {
        //https://msdn.microsoft.com/en-gb/library/2tw134k3(v=vs.100).aspx
        //https://msdn.microsoft.com/en-us/library/aa730869(VS.80).aspx
        //http://stackoverflow.com/questions/12149041/appsettings-usersettings-and-registry-keys
        //managing app settings - https://msdn.microsoft.com/en-us/library/a65txexh(v=vs.120).aspx
        //store user and session data

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
        }

        private void Create()
        {
            var configInfo = new FileInfo(_configFile);
            var directory = new DirectoryInfo(configInfo.Directory.ToString());
            if (!directory.Exists)
            {
                directory.Create();
            }
            //create config file
            var config = new DataSet("UserConfiguration");
            var userConfig = new DataTable("User");
            var plexId = new DataColumn("PlexId", typeof(string), "", MappingType.Attribute);
            userConfig.Columns.Add(plexId);
            var plexUsername = new DataColumn("PlexUsername", typeof (string), "", MappingType.Attribute);
            userConfig.Columns.Add(plexUsername);
            userConfig.Columns.Add("LastFmUsername");
            userConfig.Columns.Add("SessionId");
            var row = userConfig.NewRow();
            row["PlexId"] = "1";
            row["PlexUsername"] = string.Empty;
            row["LastFmUsername"] = string.Empty;
            row["SessionId"] = string.Empty;         
            userConfig.Rows.Add(row);
            config.Tables.Add(userConfig);
            config.WriteXmlSchema(_schemaFile);
            config.WriteXml(_configFile);
            _storage = config;
        }

        private void Save()
        {
            _storage.WriteXmlSchema(_schemaFile);
            _storage.WriteXml(_configFile);
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
