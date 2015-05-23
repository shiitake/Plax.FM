using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;
using System.Xml;
using System.Xml.Linq;
using System.Web;
using Ninject.Extensions.Logging;
using PlaxFm.Configuration;
using PlaxFm.Utilities;
using Quartz.Impl.Matchers;

namespace PlaxFm.Models
{
    public interface ILastFmScrobbler
    {
        void Scrobble(List<SongEntry> songList);
    }
    public class LastFmScrobbler: ILastFmScrobbler
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        private readonly ICustomConfiguration _customConfiguration;
        public static string UserAgent = "PlexScrobble";

        public LastFmScrobbler(ILogger logger, IAppSettings appSettings, ICustomConfiguration customConfiguration)
        {
            _logger = logger;
            _appSettings = appSettings;
            _customConfiguration = customConfiguration;
        }

        public async void Scrobble(List<SongEntry> songList)
        {
            _logger.Debug("Let's scrobble something");
            string session = await GetSession();

            if (session != "")
            {
                foreach (SongEntry songEntry in songList)
                {
                    ScrobbleTrack(session, songEntry);
                }
            }
            else
            {
                _logger.Debug("Unable to get LastFM session Id.");
            }
        }

        public async Task<string> GetSession()
        {
            var session = GetUserSessionFromConfig();
            var token = "";

            if (session == "")
            {
                _logger.Debug("Cannot find LastFM session in config. Attempting to download.");
                token = await GetLastFmToken();
                session = await DownloadLastFmSession(token);
            }
            if (session == "")
            {
                _logger.Debug("Unable to download LastFM session. Pending Authorization");
                var auth = GetUserAuthorization(token);
                //try again after authorizing
                if (auth)
                {
                    _logger.Debug("Authorization completed. Attempting to download LastFM session again.");
                    session = await DownloadLastFmSession(token);
                }
            }
            return session;
        }
        
        public string GetUserSessionFromConfig()
        {
            var sessionId = _customConfiguration.GetValue("SessionId");
            return sessionId;
        }

        public async Task<string> GetLastFmToken()
        {
            var token = "";
            var builder = new UriBuilder("http://ws.audioscrobbler.com/2.0/");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["method"] = "auth.getToken";
            query["api_key"] = _appSettings.LastFmApiKey;
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
            var param = "api_key" + _appSettings.LastFmApiKey + "methodauth.getSessiontoken" + token +
                            _appSettings.LastFmApiSecret;
            var signature = GenerateLastFmSignature(param);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["method"] = "auth.getSession";
            query["token"] = token;
            query["api_key"] = _appSettings.LastFmApiKey;
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
                                _customConfiguration.SetValue("LastFmUsername", user);
                                _customConfiguration.SetValue("SessionId", session);
                            }
                        }
                    }
                }
            }
            return session;
        }

        public async void ScrobbleTrack(string session, SongEntry song)
        {
            var timestamp = (Int32)(song.TimePlayed.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var request = "api_key" + _appSettings.LastFmApiKey + "artist" + song.Artist + "methodtrack.scrobble" + "sk" +
              session + "timestamp" + timestamp + "track" + song.Title + _appSettings.LastFmApiSecret;
            var sig = GenerateLastFmSignature(request);
            var builder = new UriBuilder("http://ws.audioscrobbler.com/2.0/");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["method"] = "track.scrobble";
            query["artist"] = song.Artist;
            query["track"] = song.Title;
            query["timestamp"] = timestamp.ToString();
            query["api_key"] = _appSettings.LastFmApiKey;
            query["api_sig"] = sig;
            query["sk"] = session;
            builder.Query = query.ToString();
            var url = builder.ToString();
            HttpContent blankcontent = new StringContent("");

            using (HttpClient client = new HttpClient())
            {
                //blankcontent.Headers.Add("User-Agent", UserAgent);
                using (HttpResponseMessage response = await client.PostAsync(url, blankcontent))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();
                    if (result != null)
                    {
                        using (XmlReader reader = XmlReader.Create(new StringReader(result)))
                        {
                            var data = XDocument.Load(reader);
                            var status = data.ElementOrEmpty("lfm").AttributeOrEmpty("status").Value;
                            var accepted =
                                data.ElementOrEmpty("lfm")
                                    .ElementOrEmpty("scrobbles")
                                    .AttributeOrEmpty("accepted")
                                    .Value;
                            if (status == "ok" && accepted == "1")
                            {
                                _logger.Info("Scrobbled " + song.Artist + " - " + song.Title);
                            }
                            else
                            {
                                _logger.Warn("Unable to scrobble " + song.Artist + " - " + song.Title + ". \nResponse: " + data);
                            }
                        }

                    }
                }
            }
        }
           
        public string GenerateLastFmSignature(string param)
        {
            return Hashing.CalculateMD5Hash(param);
        }

        public bool GetUserAuthorization(string token)
        {
            PopUp msg = new PopUp();
            return msg.Message(_appSettings.LastFmApiKey, token);
        }
    }
}
