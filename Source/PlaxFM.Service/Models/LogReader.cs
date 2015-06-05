using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using FileHelpers;
using Ninject.Extensions.Logging;
using PlaxFm.Configuration;
using PlaxFm.Core.Utilities;

namespace PlaxFm.Models
{
    public interface ILogReader
    {
        //List<SongEntry> ReadLog(string newLog, string oldLog);
    }
    
    public class LogReader : ILogReader
    {
        private readonly ILogger _logger;
        private readonly CustomConfiguration _customConfiguration;
        private string _workingCopy;
        private string _baseUrl;

        public LogReader(ILogger logger, IAppSettings appSettings, CustomConfiguration customConfiguration)
        {
            _logger = logger;
            var settings = appSettings;
            _customConfiguration = customConfiguration;
            _workingCopy = Environment.ExpandEnvironmentVariables(settings.WorkingCopy);
            _baseUrl = settings.PlexServer;
        }

        public List<SongEntry> ReadLog(string plexLog, string logCache)
        {
            plexLog = CopyLog(plexLog);
            logCache = VerifyLogExists(logCache);
            var diff = new FileDiffEngine(typeof(PlexMediaServerLog));
            var entry = diff.OnlyNewRecords(logCache, plexLog) as PlexMediaServerLog[];
            CopyCache(logCache);
            return ParseLogsForSongEntries(entry).Result;
        }

        private string CopyLog(string log)
        {
            _logger.Info("Making working copy of Plex log.");
            _workingCopy = VerifyLogExists(_workingCopy);
            int retryCount = 1;
            while (retryCount <= 6)
            {
                try
                {
                    File.Copy(log, _workingCopy, true);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warn("There was an exception while attempting to make the copy: " + ex);
                    _logger.Info("Retry attempt " + retryCount + "of 5" );
                    retryCount++;
                    Thread.Sleep(2000);
                }    
            }
            if (retryCount == 5)
            {
                _logger.Error("Exceeded maximum number of attempts to copy the log.");
            }
            return _workingCopy;
        }

        private void CopyCache(string logCache)
        {
            _logger.Info("Caching working copy of log");
            int retryCount = 1;
            while (retryCount <= 5)
            {
                try
                {
                    File.Copy(_workingCopy, logCache, true);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warn("There was an exception while attempting to make the copy: " + ex);
                    _logger.Info("Retry attempt " + retryCount + "of 5");
                    retryCount++;
                    Thread.Sleep(2000);
                }
            }
            if (retryCount == 5)
            {
                _logger.Error("Exceeded maximum number of attempts to copy the log.");
            }
        }
        
        private string VerifyLogExists(string log)
        {
            
            var logInfo = new FileInfo(log);
            if (!logInfo.Exists)
            {
                if (logInfo.Directory != null)
                {
                    var directory = new DirectoryInfo(logInfo.Directory.ToString());
                    if (!directory.Exists)
                    {
                        directory.Create();
                    }
                }
                File.Create(log);
            }
            return log;
        }

        private async Task<List<SongEntry>> ParseLogsForSongEntries(PlexMediaServerLog[] logs)
        {
            _logger.Info("Parsing logs for song entries.");
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
                        song.MediaId = Int32.Parse(lineArray[0]);
                        song.UserId = Int32.Parse(lineArray[1]);
                        var date = log.DateAdded.Trim();
                        var format = "MMM dd, yyyy HH:mm:ss:fff";
                        song.TimePlayed = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture).ToUniversalTime();
                        songList.Add(song);
                    }    
                }
            }
            if (songList.Count > 0)
            {
                songList = RemoveDuplicates(songList);
                var songs = await GetSongData(songList);
                return songs;
            }
            return songList;
        }

        private List<SongEntry> RemoveDuplicates(List<SongEntry> songList)
        {
            return new HashSet<SongEntry>(songList).ToList();
        }
        
        private async Task<List<SongEntry>> GetSongData(List<SongEntry> songList)
        {
            _logger.Info("Retrieving song data from songlist.");
            foreach (SongEntry song in songList)
            {
                XDocument docResponse = null;
                var plexUri = string.Format("{0}/library/metadata/{1}", _baseUrl, song.MediaId);
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
        
        private async Task<string> GetPlexResponse(string uri)
        {
            string result = "";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var token = _customConfiguration.GetValue("PlexToken");
                    var request = new HttpRequestMessage();
                    request.RequestUri = new Uri(uri);
                    request.Method = HttpMethod.Get;
                    request.Headers.Add("X-Plex-Token", token);
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    using (HttpContent content = response.Content)
                    {
                        result = await content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("There was an error when getting song metadata from the Plex server. Error: " + ex);
            }
            return result;
        }
    }
}
