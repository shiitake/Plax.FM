using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Ninject.MockingKernel;
using Ninject.MockingKernel.Moq;
using PlaxFm.Configuration;
using PlaxFm.Models;

namespace PlaxFm.Tests.Unit
{
    public class FakeLastFmFactory
    {
        readonly static CustomConfiguration _customConfiguration = new CustomConfiguration(new AppSettings(), null);
        static IAppSettings _appSettings;
        private const string Session = "7756856da4ff0b999f7578a229880cb5";

        public FakeLastFmFactory(IAppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public static string CreateScrobbleRequest(SongEntry song)
        {
            var lastFmApiKey = _appSettings.LastFmApiKey;
            var lastFmApiSecret = _appSettings.LastFmApiSecret;
            
            var timestamp = (Int32)(song.TimePlayed.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var request = "api_key" + lastFmApiKey + "artist" + song.Artist + "methodtrack.scrobble" + "sk" +
              Session + "timestamp" + timestamp + "track" + song.Title + lastFmApiSecret;
            return request;
        }
    }
}
