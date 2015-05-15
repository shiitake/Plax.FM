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

namespace PlexScrobble.Configuration
{
    public class CustomConfiguration
    {
        //https://msdn.microsoft.com/en-gb/library/2tw134k3(v=vs.100).aspx
        //https://msdn.microsoft.com/en-us/library/aa730869(VS.80).aspx
        //http://stackoverflow.com/questions/12149041/appsettings-usersettings-and-registry-keys
        //managing app settings - https://msdn.microsoft.com/en-us/library/a65txexh(v=vs.120).aspx
        //store user and session data

        private DataTable _storage = null;

        public CustomConfiguration()
        {
            var settingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PlexScrobble", "CustomConfiguration.xml");
            var configInfo = new FileInfo(settingFile);
            if (configInfo.Exists)
            {
                _storage = new DataTable();
                _storage.ReadXml(settingFile);
            }
            else
            {
                Create(settingFile);
            }
        }

        public void Create(string settingFile)
        {
            var configInfo = new FileInfo(settingFile);
            var directory = new DirectoryInfo(configInfo.Directory.ToString());
            if (!directory.Exists)
            {
                directory.Create();
            }
            //create config file
            var userConfig = new DataTable("CustomConfiguration");
            userConfig.Columns.Add("UserName");
            userConfig.Columns.Add("SessionId");
            var row = userConfig.NewRow();
            row["UserName"] = "default";
            row["SessionId"] = "n/a";
            userConfig.Rows.Add(row);
            userConfig.WriteXml(settingFile, XmlWriteMode.WriteSchema);
        }

        public void Save()
        {
            var settingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PlexScrobble", "CustomConfiguration.xml");
            _storage.WriteXml(settingFile);
        }

        public string GetValue(string settingName)
        {
            return _storage.Rows[0]["SessionId"].ToString();
        }

        public void DeleteRow(string settingName)
        {
            int row = 0;
            for (int i = 0; i < _storage.Rows.Count; i++)
            {
                if (_storage.Rows[i]["User"].ToString() == settingName)
                {row = i;}
            }
            _storage.Rows[row].Delete();
        }

        public void SetValue(string settingName, string settingValue)
        {
            var currentValue = GetValue(settingName);
            if (currentValue == "" && currentValue != settingName)
            {
                //delete row 0 and replace
                DeleteRow(settingName);
                var row = _storage.NewRow();
                row[settingName] = settingValue;
                _storage.Rows.Add(row);
                Save();
            }
        }
    }
}
