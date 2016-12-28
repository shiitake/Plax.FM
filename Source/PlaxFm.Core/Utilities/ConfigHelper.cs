﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Principal;
using PlaxFm.Core;
using PlaxFm.Core.Models;
using PlaxFm.Core.Store;

namespace PlaxFm.Core.Utilities
{
    public class ConfigHelper
    {
        private DataSet _storage;
        private readonly string _configFile = "CustomConfiguration.xml";
        private readonly string _schemaFile = "CustomConfiguration.xsd";
        private string _configLocation;
        private bool _loadConfigFromFile;
        private PlaxFmData _plaxDb { get; set; }
        public bool IsInitialized { get; set; }
        
        public ConfigHelper(string configFile, string schemaFile)
        {
            _configFile = configFile;
            _schemaFile = schemaFile;
            _loadConfigFromFile = true;
            var configInfo = new FileInfo(configFile);
            if (configInfo.Exists)
            {
                _storage = new DataSet("UserConfiguration");
                _storage.ReadXmlSchema(schemaFile);
                _storage.ReadXml(configFile);
            }
            else
            {
                Create();
            }
            
            //if (storage == null)
            //{
            //    Create();
            //}
            //else
            //{
            //    _storage = storage;
            //}
        }

        public ConfigHelper(string configLocation)
        {
            //check for database
            _plaxDb = new PlaxFmData(configLocation);
            var userCount = GetUserCount();
            if (userCount < 1)
            {
                _configLocation = configLocation;
                //check for config file
                var configFile = _configLocation + @"\" + _configFile;
                var schemaFile = _configLocation + @"\" + _schemaFile;
                var configInfo = new FileInfo(configFile);
                if (configInfo.Exists)
                {
                    var migrateStorage = new DataSet("UserConfiguration");
                    migrateStorage.ReadXmlSchema(schemaFile);
                    migrateStorage.ReadXml(configFile);

                    //todo: migrate old storage to new storage
                }
                else
                {
                    //create new users in db
                    //create new dataset
                }
            }
        }

        public int GetUserCount()
        {
                using (var context = new PlaxContext(_plaxDb.DbConnection))
                {
                    return context.Users.Count();
                }
        }

        public bool IsUserConfirmed(int plexId)
        {
            using (var context = new PlaxContext(_plaxDb.DbConnection))
            {
                return context.Users.Any(u => u.PlexId == plexId && u.IsAuthorized && u.PlexToken != "");
            }
        }

        public void AddUser(User user)
        {
            using (var context = new PlaxContext(_plaxDb.DbConnection))
            {
                context.Users.Add(user);
            }
        }

        public User GetUserByUserName(string userName)
        {
            using (var context = new PlaxContext(_plaxDb.DbConnection))
            {
                return context.Users.First(u => u.PlexUsername == userName);
            }
        }
        public User GetUserByPlexId(int plexId)
        {
            using (var context = new PlaxContext(_plaxDb.DbConnection))
            {
                return context.Users.First(u => u.PlexId  == plexId);
            }
        }

        public DataSet GetStorage()
        {
            return _storage;
        }

        private void CreateConfig()
        {
            if (!string.IsNullOrWhiteSpace(_configFile))
            {
                var configInfo = new FileInfo(_configFile);
                _configLocation = configInfo.Directory.ToString();
            }
            var directory = new DirectoryInfo(_configLocation);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }

        private void Create()
        {
            CreateConfig();
            CreateDataSet();
            Save();
        }

        private void CreateDataSet() { 
            
            //create config Dataset
            var config = new DataSet("UserConfiguration");

            //create Setup table
            var userInit = new DataTable("Setup");
            var initialized = new DataColumn("Initialized", typeof(bool), "", MappingType.Attribute);
            userInit.Columns.Add(initialized);
            userInit.Columns.Add("Profile");
            var initRow = userInit.NewRow();
            initRow["Initialized"] = false;
            initRow["Profile"] = String.Empty;
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null)
            {
                var name = windowsIdentity.Name;
                var index = name.LastIndexOf(@"\",StringComparison.CurrentCulture);
                name = name.Substring(index + 1);
                initRow["Profile"] = name;
            }
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
            var row = CreateNewUser(userConfig);
            userConfig.Rows.Add(row);

            //add tables to dataset
            config.Tables.Add(userInit);
            config.Tables.Add(userConfig);
            
            _storage = config;
        }

        private DataRow CreateNewUser(DataTable userConfig)
        {
            var row = userConfig.NewRow();
            row["PlexId"] = "1";
            row["PlexUsername"] = string.Empty;
            row["LastFmUsername"] = string.Empty;
            row["SessionId"] = string.Empty;
            row["Token"] = string.Empty;
            row["Authorized"] = false;
            row["PlexToken"] = string.Empty;
            return row;
        }

        public DataSet LoadConfig()
        {
            if (_loadConfigFromFile)
            {
                return LoadConfigFromFile();
            }
            var storage = new DataSet();
            storage.ReadXmlSchema(_schemaFile);
            storage.ReadXml(_configFile);
            _storage = storage;
            return _storage;
        }

        public DataSet LoadConfigFromFile()
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
            if (_loadConfigFromFile)
            {
                _storage.WriteXmlSchema(_schemaFile);
                _storage.WriteXml(_configFile);
            }
            else
            {
                
            }
            
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

        public void SetValue(string tableName, string settingName, string settingValue, int plexId = 1)
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
