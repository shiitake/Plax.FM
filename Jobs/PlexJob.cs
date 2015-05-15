using System;
using System.Collections.Generic;
using System.Configuration;
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
        private string OldLog = @"C:\temp\OldLog.log";
        private string NewLog = @"C:\Users\E002796\AppData\Local\Plex Media Server\Logs\Plex Media Server.log";

        public PlexJob(ILogger logger, IAppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
            //_logReader = logReader;
            //_lastFmScrobbler = lastFmScrobbler;
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
            var reader = new LogReader(_logger, _appSettings);
            //var songList = reader.ReadLog(NewLog, OldLog);
            var songList = new List<SongEntry>();
            var song = new SongEntry();
            song.Artist = "LadyHawke";
            song.Title = "Dusk Til' Dawn";
            song.TimePlayed = new DateTime(2015, 05, 13, 14, 56, 0);
            songList.Add(song);
            if (songList.Count > 0)
            {
                _logger.Debug(songList.Count + "new song(s) found.");
                var scrobbler = new LastFmScrobbler(_logger, _appSettings);
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
