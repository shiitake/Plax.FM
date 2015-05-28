﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;
using Excel;
using FileHelpers;
using System.IO;
using Ninject.Extensions.Logging;
using PlaxFm.Configuration;
using PlaxFm.Core.Utilities;
using Quartz;

namespace PlaxFm.Models
{
    public interface ILogReader
    {
        //List<SongEntry> ReadLog(string newLog, string oldLog);
    }
    
    public class LogReader : ILogReader
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        private readonly ICustomConfiguration _customConfiguration;
        private string _workingCopy;
        private string _baseUrl;

        public LogReader(ILogger logger, IAppSettings appSettings, ICustomConfiguration customConfiguration)
        {
            _logger = logger;
            _appSettings = appSettings;
            _customConfiguration = customConfiguration;
            _workingCopy = Environment.ExpandEnvironmentVariables(_appSettings.WorkingCopy);
            _baseUrl = _appSettings.PlexServer;
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

        public void ReadLog(string newLog)
        {
            newLog = CopyLog(newLog);
            var engine = new FileHelperEngine(typeof (PlexMediaServerLog));
            var entry = engine.ReadFile(newLog) as PlexMediaServerLog[];
            ParseLogsForUsername(entry);
        }

        private string CopyLog(string log)
        {
            _workingCopy = VerifyLogExists(_workingCopy);
            while (true)
            {
                try
                {
                    File.Copy(log, _workingCopy, true);
                    break;
                }
                catch (IOException)
                {
                   Thread.Sleep(1000);
                }    
            }
            return _workingCopy;
        }

        private void CopyCache(string logCache)
        {
            while (true)
            {
                try
                {
                    File.Copy(_workingCopy, logCache, true);
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(1000);
                }
            }
        }
        
        private string VerifyLogExists(string log)
        {
            var logInfo = new FileInfo(log);
            if (!logInfo.Exists)
            {
                var directory = new DirectoryInfo(logInfo.Directory.ToString());
                if (!directory.Exists)
                {
                    directory.Create();
                }
                File.Create(log);
            }
            return log;
        }

        private async Task<List<SongEntry>> ParseLogsForSongEntries(PlexMediaServerLog[] logs)
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
                var songs = await GetSongData(songList);
                return songs;
            }
            return songList;
        }

        private void ParseLogsForUsername(PlexMediaServerLog[] logs)
        {
            Regex rgx = new Regex(@".*\sDEBUG\s-\s.*User\sis\s(\w+)\s\(ID:\s(\d+)\)");
            foreach (PlexMediaServerLog log in logs)
            {
                if (rgx.IsMatch(log.LogEntry))
                {
                    var line = rgx.Replace(log.LogEntry, "$1,$2");
                    if (line.Length > 0)
                    {
                        var lineArray = line.Split(',');
                        _customConfiguration.AddUser(lineArray[0],int.Parse(lineArray[1]));
                    }
                }
            }
        }

        private async Task<List<SongEntry>> GetSongData(List<SongEntry> songList)
        {
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
            using (HttpClient client = new HttpClient())
            {
                var token = _customConfiguration.GetValue("PlexToken");
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
