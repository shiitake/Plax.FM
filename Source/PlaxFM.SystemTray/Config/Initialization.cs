using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Web;
using PlaxFm.Core;
using PlaxFm.Core.Utilities;

namespace PlaxFm.SystemTray.Config
{
    class Initialization
    {
        private string LastFmApiKey = "266155149c516542879ee1ec55c93697";
        private string LastFmApiSecret = "035cf4de6dc150fff952fbf3108d10b1";
        private string ConfigFile = @"%ProgramData%\PlaxFM\Config\CustomConfiguration.xml";
        private string _configFile;
        private string SchemaFile = @"%ProgramData%\PlaxFM\Config\CustomConfiguration.xsd";
        private string _schemaFile;
        private DataSet _storage;
        private ConfigHelper _config;

        //service can check for session
        //if it doesn't exist then it can save the auth url to the config file

        //systray can check for this URL
        public Initialization()
        {
            _configFile = Environment.ExpandEnvironmentVariables(ConfigFile);
            _schemaFile = Environment.ExpandEnvironmentVariables(SchemaFile);
            var configInfo = new FileInfo(_configFile);
            if (configInfo.Exists)
            {
                _storage = new DataSet("UserConfiguration");
                _storage.ReadXmlSchema(_schemaFile);
                _storage.ReadXml(_configFile);
            }
            _config = new ConfigHelper(_storage, _configFile, _schemaFile);
        }

        public bool GetAuthorizationUrl()
        {
            var token = _config.GetValue("token");
            PopUp msg = new PopUp();
            return msg.Message(LastFmApiKey, token);
        }

        public async Task<string> GetLastFmToken()
        {
            var token = "";
            var builder = new UriBuilder("http://ws.audioscrobbler.com/2.0/");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["method"] = "auth.getToken";
            query["api_key"] = LastFmApiKey;
            builder.Query = query.ToString();
            var url = builder.ToString();

            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();
                    if (result != null)
                    {
                        using (XmlReader reader = XmlReader.Create(new StringReader(result)))
                        {
                            var data = XDocument.Load(reader);
                            token = data.ElementOrEmpty("lfm").ElementOrEmpty("token").Value;
                        }

                    }
                }
            }
            return token;
        }
        
        public async Task<string> DownloadLastFmSession(string token)
        {
            var session = "";
            var builder = new UriBuilder("http://ws.audioscrobbler.com/2.0/");
            var param = "api_key" + LastFmApiKey + "methodauth.getSessiontoken" + token +
                            LastFmApiSecret;
            var signature = GenerateLastFmSignature(param);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["method"] = "auth.getSession";
            query["token"] = token;
            query["api_key"] = LastFmApiKey;
            query["api_sig"] = signature;
            builder.Query = query.ToString();
            var url = builder.ToString();

            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();
                    if (result != null)
                    {
                        using (XmlReader reader = XmlReader.Create(new StringReader(result)))
                        {
                            var data = XDocument.Load(reader);
                            session =
                                data.ElementOrEmpty("lfm").ElementOrEmpty("session").ElementOrEmpty("key").Value;
                            var user = data.ElementOrEmpty("lfm").ElementOrEmpty("session").ElementOrEmpty("name").Value;
                            //save config
                            if (session != "" && user != "")
                            {
                                _config.SetValue("LastFmUsername", user);
                                _config.SetValue("SessionId", session);
                                _config.Save();
                            }
                        }
                    }
                }
            }
            return session;
        }

        public string GenerateLastFmSignature(string param)
        {
            return Hashing.CalculateMD5Hash(param);
        }

        //get user auth from popup
        //use token to get session Id
        //save session data to config
    }
}
