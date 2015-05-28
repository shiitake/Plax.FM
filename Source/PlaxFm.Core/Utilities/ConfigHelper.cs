using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PlaxFm.Core.Utilities
{
    public class ConfigHelper
    {
        private DataSet _storage;
        private readonly string _configFile;
        private readonly string _schemaFile;
        
        public ConfigHelper(DataSet storage, string configFile, string schemaFile)
        {
            _configFile = configFile;
            _schemaFile = schemaFile;
            if (storage == null)
            {
                Create();
            }
            else
            {
                _storage = storage;
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
            //create config Dataset
            var config = new DataSet("UserConfiguration");

            //create Setup table
            var userInit = new DataTable("Setup");
            var initialized = new DataColumn("Initialized", typeof(bool), "", MappingType.Attribute);
            userInit.Columns.Add(initialized);
            var initRow = userInit.NewRow();
            initRow["Initialized"] = false;
            userInit.Rows.Add(initRow);

            //create User table
            var userConfig = new DataTable("User");
            var plexId = new DataColumn("PlexId", typeof(string), "", MappingType.Attribute);
            userConfig.Columns.Add(plexId);
            var plexUsername = new DataColumn("PlexUsername", typeof(string), "", MappingType.Attribute);
            userConfig.Columns.Add(plexUsername);
            userConfig.Columns.Add("LastFmUsername");
            userConfig.Columns.Add("SessionId");
            userConfig.Columns.Add("Token");
            userConfig.Columns.Add("PlexToken");
            var authorized = new DataColumn("Authorized", typeof(bool), "", MappingType.Attribute);
            userConfig.Columns.Add(authorized);
            var row = userConfig.NewRow();
            row["PlexId"] = "1";
            row["PlexUsername"] = string.Empty;
            row["LastFmUsername"] = string.Empty;
            row["SessionId"] = string.Empty;
            row["Token"] = string.Empty;
            row["Authorized"] = false;
            row["PlexToken"] = string.Empty;
            userConfig.Rows.Add(row);

            //add tables to dataset
            config.Tables.Add(userInit);
            config.Tables.Add(userConfig);

            //write dataset to config files
            config.WriteXmlSchema(_schemaFile);
            config.WriteXml(_configFile);
            _storage = config;
        }

        public DataSet LoadConfig()
        {
            var storage = new DataSet();
            storage.ReadXmlSchema(_schemaFile);
            storage.ReadXml(_configFile);
            _storage = storage;
            return _storage;
        }
        
        public void Save(DataSet storage)
        {
            _storage = storage;
            Save();
        }

        public void Save()
        {
            _storage.WriteXmlSchema(_schemaFile);
            _storage.WriteXml(_configFile);
        }

        public string GetValue(string settingName, int plexId = 1)
        {
            int row = GetRowById(plexId);
            return _storage.Tables["User"].Rows[row][settingName].ToString();
        }

        public string GetValue(string tableName, string settingName, int plexId = 1)
        {
            int row = GetRowById(plexId);
            return _storage.Tables[tableName].Rows[row][settingName].ToString();
        }

        public void SetValue(string settingName, object settingValue, int plexId = 1)
        {
            int row = GetRowById(plexId);

            _storage.Tables["User"].Rows[row][settingName] = settingValue;
            Save();
        }

        public void SetValue(string tableName, string settingName, object settingValue, int plexId = 1)
        {
            int row = GetRowById(plexId);

            _storage.Tables[tableName].Rows[row][settingName] = settingValue;
            Save();
        }

        public int GetRowById(int plexId = 1)
        {
            int row = 0;
            for (int i = 0; i < _storage.Tables["User"].Rows.Count; i++)
            {
                if (_storage.Tables["User"].Rows[i]["PlexId"].ToString() == plexId.ToString())
                { row = i; }
            }
            return row;
        }

        public int GetRowByUsername(string username)
        {
            int row = 0;
            for (int i = 0; i < _storage.Tables["User"].Rows.Count; i++)
            {
                if (_storage.Tables["User"].Rows[i]["PlexUsername"].ToString() == username)
                { row = i; }
            }
            return row;
        }

        public void DeleteRow(int row)
        {
            _storage.Tables["User"].Rows[row].Delete();
            Save();
        }
    }
}
