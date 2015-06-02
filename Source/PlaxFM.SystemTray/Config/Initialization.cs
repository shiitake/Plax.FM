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

        public Initialization()
        {
            try
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
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Setup(string userName, string password)
        {
            //setup PlexToken
            var plexToken = ConfirmPlexSetup();
            if (!plexToken)
            {
                GetPlexToken(userName, password);
            }
            Setup();
        }
        
        public async void Setup()
        {
            var token = await GetLastFmToken();
            _config.SetValue("Token", token);
            var authorized = GetAuthorization(token);
            if (authorized)
            {
                _config.SetValue("Authorized", true);
                var session = await DownloadLastFmSession(token);
                if (session != "")
                {
                    _config.SetValue("Setup", "Initialized", true);
                    _config.SetValue("Token", "");
                }
                var ready = ConfirmLastFmSetup();
                if (ready)
                {
                    PopUp msg = new PopUp();
                    msg.Message("Setup has completed successfully");
                }
            }
        }
        
        public bool ConfirmLastFmSetup()
        {
            _storage = _config.LoadConfig();
            var init = _config.GetValue("Setup", "Initialized").ToLower() == "true";
            var auth = _config.GetValue("Authorized").ToLower() == "true";
            return (init && auth);
        }

        public bool ConfirmPlexSetup()
        {
            return _config.GetValue("PlexToken") != "";
        }
        
        public bool GetAuthorization(string token)
        {
            PopUp msg = new PopUp();
            return msg.Message(LastFmApiKey, token);
        }

        public async Task<string> GetLastFmToken()
        {
            //check for saved token
            var token = _config.GetValue("Token");
            if (token != "") return token;
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

        private async void GetPlexToken(string userName, string password)
        {
            var url = "https://plex.tv/users/sign_in.xml";
            var myplexaccount = userName;
            var mypassword = password;
            var token = "";
            byte[] accountBytes = Encoding.UTF8.GetBytes(myplexaccount + ":" + mypassword);
            var encodedPassword = Convert.ToBase64String(accountBytes);
            //todo: test this
            //var encodedPassword = "c2JhcnJldHQwMDpjZ2laTkpqSkM4Vmg=";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Plex-Client-Identifier", "PlexScrobble");
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedPassword);
                HttpContent blankcontent = new StringContent("");
                using (HttpResponseMessage response = await client.PostAsync(url, blankcontent))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();
                    if (result != null)
                    {
                        using (XmlReader reader = XmlReader.Create(new StringReader(result)))
                        {
                            var data = XDocument.Load(reader);
                            token = data.ElementOrEmpty("user").ElementOrEmpty("authentication-token").Value;
                            _config.SetValue("PlexToken", token);
                        }
                    }
                }
            }
        }
    }
}
