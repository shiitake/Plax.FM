using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Ninject.Extensions.Logging;
using PlexScrobble.Configuration;
using PlexScrobble.Models;
using Quartz;

namespace PlexScrobble.Jobs
{
    public interface IPlexJob : IJob
    {
        
    }
    
    public class PlexJob : IPlexJob
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        private readonly ILogReader _logReader;
        private readonly ILastFmScrobbler _lastFmScrobbler;
        private readonly ICustomConfiguration _customConfiguration;
        //private string _OldLog = @"C:\temp\OldLog.log";
        private readonly string LogCache = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"PlexScrobble\Logs\PlexLogCache.log"));
        //private string NewLog = @"C:\Users\E002796\AppData\Local\Plex Media Server\Logs\Plex Media Server.log";
        private readonly string PlexLog =
            Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Plex Media Server\Logs\Plex Media Server.log"));

        public PlexJob(ILogger logger, IAppSettings appSettings, ICustomConfiguration customConfiguration)
        {
            _logger = logger;
            _appSettings = appSettings;
            _customConfiguration = customConfiguration;
        }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error attempting to start the job");
                throw;
            }
            finally
            {
                var jobTime = context.NextFireTimeUtc;
                if (jobTime != null)
                {
                    _logger.Info("Job scheduled to run again at {0}", jobTime.Value.ToLocalTime().ToString("MM-dd-yyyy HH:mm"));
                }
            }
        }

        public void Start()
        {
            _logger.Debug("Job starting.");
            var reader = new LogReader(_logger, _appSettings, _customConfiguration);
            var songList = reader.ReadLog(PlexLog, LogCache);
            //var songList = new List<SongEntry>();
            //var song = new SongEntry();
            //song.Artist = "LadyHawke";
            //song.Title = "Dusk Til' Dawn";
            //song.TimePlayed = new DateTime(2015, 05, 13, 14, 56, 0);
            //songList.Add(song);
            if (songList.Count > 0)
            {
                _logger.Debug(songList.Count + "new song(s) found.");
                var scrobbler = new LastFmScrobbler(_logger, _appSettings, _customConfiguration);
                scrobbler.Scrobble(songList);
            }
            else
            {
                _logger.Debug("No new songs found.");
            }
            _logger.Debug("Job finished.");
        }
    }
}
