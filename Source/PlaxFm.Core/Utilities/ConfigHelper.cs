using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.Core.Utilities
{
    public class ConfigHelper
    {
        private DataSet _storage;
        private readonly string _configFile;
        private readonly string _schemaFile;
        
        public ConfigHelper(DataSet storage, string configFile, string schemaFile)
        {
            _storage = storage;
            _configFile = configFile;
            _schemaFile = schemaFile;
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
