using System;
using System.Data;
using System.IO;
using System.Net.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using PlaxFm.Core.Utilities;
using NLog;
using Ninject.Extensions.Logging;
using Ninject.Extensions.Logging.NLog2;
using Ninject;
using PlaxFm.Core.CustomExceptions;

namespace PlaxFm.SystemTray.Config
{
    public interface IInitializer
    { }
      
    class Initializer : IInitializer
    {
        private readonly Logger _logger;
        private readonly string LastFmApiKey = "266155149c516542879ee1ec55c93697";
        private readonly string LastFmApiSecret = "035cf4de6dc150fff952fbf3108d10b1";
        private readonly string ConfigFile = @"%ProgramData%\PlaxFM\Config\CustomConfiguration.xml";
        private readonly string SchemaFile = @"%ProgramData%\PlaxFM\Config\CustomConfiguration.xsd";
        private DataSet _storage;
        private readonly ConfigHelper _config;

        public Initializer(Logger logger)
        {
            _logger = logger;
            try
            {
                _logger.Debug("Looking for configuration information.");
                var configFile = Environment.ExpandEnvironmentVariables(ConfigFile);
                var schemaFile = Environment.ExpandEnvironmentVariables(SchemaFile);
                var configInfo = new FileInfo(configFile);
                if (configInfo.Exists)
                {
                    _storage = new DataSet("UserConfiguration");
                    _storage.ReadXmlSchema(schemaFile);
                    _storage.ReadXml(configFile);
                }
                _config = new ConfigHelper(_storage, configFile, schemaFile);
            }
            catch (FileNotFoundException ex)
            {
                _logger.Error("Error during initialization. "+ ex);
                
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
                else
                {
                    _logger.Warn("No Session Id found.");
                }
            }
            else
            {
                _logger.Warn("Account has not been authorized.");
            }

            var ready = ConfirmLastFmSetup();
            if (ready)
            {
                PopUp msg = new PopUp();
                msg.Message("Setup has completed successfully");
                _logger.Info("Setup has completed successfully.");
            }
            else
            {
                PopUp msg = new PopUp();
                msg.Message("There was a problem with your setup. Please try again.");
                _logger.Warn("Setup did not complete successfully. See logs for details.");
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
            try
            {
                _logger.Debug("Getting LastFM Authorization");
                PopUp msg = new PopUp();
                return msg.Message(LastFmApiKey, token);
            }
            catch (IncompleteAuthorization ex)
            {
                _logger.Warn(ex);
            }
            return false;
        }

        public async Task<string> GetLastFmToken()
        {
            //check for saved token
            _logger.Info("Getting Last.Fm Token");
            var token = _config.GetValue("Token");
            if (token != "") return token;
            _logger.Info("No local token found. Attempting to download from Last.Fm.");
            var builder = new UriBuilder("http://ws.audioscrobbler.com/2.0/");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["method"] = "auth.getToken";
            query["api_key"] = LastFmApiKey;
            builder.Query = query.ToString();
            var url = builder.ToString();
            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.Error("Error downloading Last.Fm Token. " + ex);
            }
            return token;
        }
        
        public async Task<string> DownloadLastFmSession(string token)
        {
            _logger.Info("Downloading Session Id from Last.FM");
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
            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.Error("Error downloading Last.Fm Session Id. " + ex);
            }
            return session;
        }

        public string GenerateLastFmSignature(string param)
        {
            return Hashing.CalculateMD5Hash(param);
        }

        private async void GetPlexToken(string userName, string password)
        {
            _logger.Info("Downloading Plex user token.");
            var url = "https://plex.tv/users/sign_in.xml";
            var myplexaccount = userName;
            var mypassword = password;
            byte[] accountBytes = Encoding.UTF8.GetBytes(myplexaccount + ":" + mypassword);
            var encodedPassword = Convert.ToBase64String(accountBytes);
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
                            var token = data.ElementOrEmpty("user").ElementOrEmpty("authentication-token").Value;
                            _config.SetValue("PlexToken", token);
                        }
                    }
                }
            }
        }
    }
}
