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
        readonly CustomConfiguration _customConfiguration = new CustomConfiguration(new AppSettings(), null);

        public static string CreateScrobbleRequest(SongEntry song)
        {
            var LastFmApiKey = "266155149c516542879ee1ec55c93697";
            var LastFmApiSecret = "035cf4de6dc150fff952fbf3108d10b1";
            
            var session = "7756856da4ff0b999f7578a229880cb5";
            var timestamp = (Int32)(song.TimePlayed.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var request = "api_key" + LastFmApiKey + "artist" + song.Artist + "methodtrack.scrobble" + "sk" +
              session + "timestamp" + timestamp + "track" + song.Title + LastFmApiSecret;
            return request;
        }
    }
}
