using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Excel;
using FileHelpers;
using System.IO;
using Ninject.Extensions.Logging;
using PlexScrobble.Configuration;
using PlexScrobble.Utilities;
using Quartz;

namespace PlexScrobble.Models
{
    public interface ILogReader
    {
        //List<SongEntry> ReadLog(string newLog, string oldLog);
    }
    
    public class LogReader : ILogReader
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        //public static string _newLog;
        //public static string _oldLog;
        public static string LogCopy = "LogCopy.txt";
        public string BaseUrl = @"http://mediapc:32400";

        public LogReader(ILogger logger, IAppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        public List<SongEntry> ReadLog(string newLog, string oldLog)
        {
            newLog = CopyLog(newLog);
            FileDiffEngine diff = new FileDiffEngine(typeof(PlexMediaServerLog));
            PlexMediaServerLog[] entry = diff.OnlyNewRecords(oldLog, newLog) as PlexMediaServerLog[];
            //System.IO.File.Copy(newLog, oldLog, true);
            return ParseLogs(entry);
        }

        private string CopyLog(string log)
        {
            try
            {
                File.Copy(log, LogCopy, true);
                return LogCopy;
            }
            catch (Exception ex)
            {
                //_logger.Error("There was an error attempting to access the Plex Media Server Log. Error: " + ex.Message);
                throw;
            }
        }
        
        private List<SongEntry> ParseLogs(PlexMediaServerLog[] logs)
        {
            var songList = new List<SongEntry>();
            Regex rgx = new Regex(@".*\sDEBUG\s-\sLibrary\sitem\s(\d+)\s'.*'\sgot\splayed\sby\saccount\s(\d+).*");
            foreach (PlexMediaServerLog log in logs)
            {
                if (rgx.IsMatch(log.LogEntry))
                {
                    var line = rgx.Replace(log.LogEntry, "$1,$2");
                    if (line.Length > 0)
                    {
                        var lineArray = line.Split(',');
                        var song = new SongEntry();
                        song.MediaId = Int16.Parse(lineArray[0]);
                        song.UserId = Int16.Parse(lineArray[1]);
                        var date = log.DateAdded.Trim();
                        var format = "MMM dd, yyyy HH:mm:ss:fff";
                        song.TimePlayed = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
                        songList.Add(song);
                    }    
                }
                
            }
            return songList.Count > 0 ? GetSongData(songList).Result : songList;
        }

        public async Task<List<SongEntry>> GetSongData(List<SongEntry> songList)
        {
            foreach (SongEntry song in songList)
            {
                XDocument docResponse = null;
                var plexUri = BaseUrl + "/library/metadata/" + song.MediaId;
                Task<string> plexCall = GetPlexResponse(plexUri);
                string responseRaw = await plexCall;

                if (responseRaw != null)
                {
                    using (XmlReader reader = XmlReader.Create(new StringReader(responseRaw)))
                    {
                        docResponse = XDocument.Load(reader);
                    }
                }

                if (docResponse != null)
                {
                    song.Artist =
                        docResponse.ElementOrEmpty("MediaContainer")
                            .ElementOrEmpty("Track")
                            .AttributeOrEmpty("originalTitle")
                            .Value;
                    if (song.Artist == "")
                    {
                        song.Artist =
                            docResponse.ElementOrEmpty("MediaContainer")
                                .ElementOrEmpty("Track")
                                .AttributeOrEmpty("grandparentTitle")
                                .Value;
                    }
                    song.Title =
                        docResponse.ElementOrEmpty("MediaContainer")
                            .ElementOrEmpty("Track")
                            .AttributeOrEmpty("title")
                            .Value;
                    song.Album =
                        docResponse.ElementOrEmpty("MediaContainer")
                            .ElementOrEmpty("Track")
                            .AttributeOrEmpty("parentTitle")
                            .Value;
                }
                if (song.Artist == "" || song.Title == "")
                {
                    songList.Remove(song);
                }
            }
            return songList;
        }

        public async Task<string> GetPlexToken()
        {
            var url = "https://plex.tv/users/sign_in.xml";
            var myplexaccount = "sbarrett00";
            var mypassword = "123456";
            var token = "";
            byte[] accountBytes = Encoding.UTF8.GetBytes(myplexaccount +":"+ mypassword);
            //var encodedPassword = Convert.ToBase64String(accountBytes);
            var encodedPassword = "c2JhcnJldHQwMDpjZ2laTkpqSkM4Vmg=";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Plex-Client-Identifier","PlexScrobble");
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
                        }
                        
                    }
                }
            }
            return token;
        }

        public async Task<string> GetPlexResponse(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                var token = await GetPlexToken();
                var request = new HttpRequestMessage();
                request.RequestUri = new Uri(uri);
                request.Method = HttpMethod.Get;
                request.Headers.Add("X-Plex-Token", token.ToString());
                using (HttpResponseMessage response = await client.GetAsync(uri))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();
                    return result;
                }
            }
        }
    }
}
