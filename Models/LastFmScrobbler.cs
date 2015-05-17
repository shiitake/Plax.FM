using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Web;
using Ninject.Extensions.Logging;
using PlexScrobble.Configuration;
using PlexScrobble.Utilities;
using Quartz.Impl.Matchers;

namespace PlexScrobble.Models
{
    public interface ILastFmScrobbler
    {
        void Scrobble(List<SongEntry> songList);
    }
    public class LastFmScrobbler: ILastFmScrobbler
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        private CustomConfiguration _customConfiguration;
        public static string UserAgent = "PlexScrobble";

        public LastFmScrobbler(ILogger logger, IAppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
            _customConfiguration = new CustomConfiguration();
        }

        public void Scrobble(List<SongEntry> songList)
        {
            //Todo: scrobble!
            _logger.Debug("Let's scrobble something");

            //authentication steps
            //1 - get token (with api-key + sig)
            //2 - get user authentication (with api-key and token)
            //3 - create session (api_key + token + api_sig) - save this for later
            //4 - scrobble track with authenticated call (sk + api_key + api_sig)

            //token will make sure that user is authorized and will help you get session
            //authorization - http://www.last.fm/api/auth/?api_key=266155149c516542879ee1ec55c93697&token=0619b02fd401c6129fa855cf2a2a2c55

            var session = GetUserSessionFromConfig();

            if (session == "" || session == "n/a")
            {
                var token = GetLastFmToken().Result;
                session = GetLastFmSession(token).Result;
            }

            //scrobbletrack
            foreach (SongEntry songEntry in songList)
            {
                ScrobbleTrack(session,songEntry);
            }
            
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

        public async Task<string> GetLastFmSession(string token)
        {
            var session = "db360eb32df7b43455a6ccefa0d262e2";
            var user = "shiitake_dev";
            //var builder = new UriBuilder("http://ws.audioscrobbler.com/2.0/");
            //var param = "api_key" + _appSettings.LastFmApiKey + "methodauth.getSessiontoken" + token +
            //                _appSettings.LastFmApiSecret;
            //var signature = GenerateLastFmSignature(param);
            //var query = HttpUtility.ParseQueryString(builder.Query);
            //query["method"] = "auth.getSession";
            //query["token"] = token;
            //query["api_key"] = _appSettings.LastFmApiKey;
            //query["api_sig"] = signature;
            //builder.Query = query.ToString();
            //var url = builder.ToString();

            //using (HttpClient client = new HttpClient())
            //{
            //    using (HttpResponseMessage response = await client.GetAsync(url))
            //    using (HttpContent content = response.Content)
            //    {
            //        string result = await content.ReadAsStringAsync();
            //        if (result != null)
            //        {
            //            using (XmlReader reader = XmlReader.Create(new StringReader(result)))
            //            {
            //                var data = XDocument.Load(reader);
            //                session =
            //                    data.ElementOrEmpty("lfm").ElementOrEmpty("session").ElementOrEmpty("key").Value;
            //                user = data.ElementOrEmpty("lfm").ElementOrEmpty("session").ElementOrEmpty("name").Value;
                            //save config
                            _customConfiguration.SetValue("LastFmUsername", user, "1");
                            _customConfiguration.SetValue("SessionId", session, "1");
            //            }

            //        }
            //    }
            //}
            return session;
        }

        public async void ScrobbleTrack(string session, SongEntry song)
        {
            var timestamp = (Int32)(song.TimePlayed.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var request = "api_key" + _appSettings.LastFmApiKey + "artist" + song.Artist + "methodtrack.scrobble" + "sk" +
              session + "timestamp" + timestamp + "track" + song.Title + _appSettings.LastFmApiSecret;
            //var sig = GenerateLastFmSignature(HttpUtility.UrlEncode(request, Encoding.UTF8));
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

        public string GetUserSessionFromConfig()
        {
            var sessionId = _customConfiguration.GetValue("SessionId");
            //sessionId = "db360eb32df7b43455a6ccefa0d262e2";
            return sessionId;
        }
        
        public void GetUserAuthorization(string token)
        {
            PopUp msg = new PopUp();
            msg.Message(_appSettings.LastFmApiKey, token);
        }
    }
}
