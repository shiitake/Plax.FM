using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Ninject.Extensions.Logging;
using PlaxFm.Configuration;
using PlaxFm.Core.Utilities;

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
        private readonly CustomConfiguration _customConfiguration;
        public static string UserAgent = "PlexScrobble";

        public LastFmScrobbler(ILogger logger, IAppSettings appSettings, CustomConfiguration customConfiguration)
        {
            _logger = logger;
            _appSettings = appSettings;
            _customConfiguration = customConfiguration;
        }

        public void Scrobble(List<SongEntry> songList)
        {
            _logger.Info("Scrobbling song entries.");
            string session = _customConfiguration.GetValue("SessionId");
            //var session = "aa17677e445e60e864f1eb100259987c";

            if (session != "")
            {
                foreach (SongEntry songEntry in songList)
                {
                    ScrobbleTrack(session, songEntry);
                }
            }
            else
            {
                _logger.Warn("Unable to get LastFM session Id.");
            }
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
            try
            {
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
            catch (Exception ex)
            {
                _logger.Error("There was an error when connecting to the Last.FM server. " + ex);
            }
        }
           
        public string GenerateLastFmSignature(string param)
        {
            return Hashing.CalculateMD5Hash(param);
        }

    }
}
