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
        private int _maxTimeout;

        public LogReader(ILogger logger, IAppSettings appSettings, CustomConfiguration customConfiguration)
        {
            _logger = logger;
            var settings = appSettings;
            _customConfiguration = customConfiguration;
            _workingCopy = Environment.ExpandEnvironmentVariables(settings.WorkingCopy);
            _baseUrl = settings.PlexServer;
            _maxTimeout = settings.MaxTimeout;
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
            
            var locked = FileExtensions.IsFileLocked(log);
            var timeout = 0;
            //wait for unlock
            while (locked)
            {
                locked = FileExtensions.IsFileLocked(log);
                Thread.Sleep(1000);
                timeout += 1000;
                if (timeout >= _maxTimeout)
                {
                    _logger.Warn("File lock operation has timed out.");
                    break;
                }
            }
            try
            {
                File.Copy(log, _workingCopy, true);
            }
            catch (Exception ex)
            {
                _logger.Error("There was an exception while attempting to make the copy of the Plex Media server log: " + ex);
            }
            return _workingCopy;
        }

        private void CopyCache(string logCache)
        {
            _logger.Info("Caching working copy of log");
            var locked = FileExtensions.IsFileLocked(logCache);
            var timeout = 0;
            while (locked)
            {
                locked = FileExtensions.IsFileLocked(logCache);
                Thread.Sleep(1000);
                timeout += 1000;
                if (timeout > _maxTimeout)
                {
                    _logger.Warn("File lock operation has timed out.");
                    break;
                }
            }
            try
            {
                File.Copy(_workingCopy, logCache, true);
            }
            catch (Exception ex)
            {
                _logger.Error("There was an exception while attempting to make the copy of the log cache: " + ex);
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
