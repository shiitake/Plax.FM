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
    public interface IPlexJob : IJob { }
    
    public class PlexJob : IPlexJob
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        private readonly ILogReader _logReader;
        private readonly ILastFmScrobbler _lastFmScrobbler;
        private readonly ICustomConfiguration _customConfiguration;
        private readonly string LogCache;
        private readonly string PlexLog;
        
        public PlexJob(ILogger logger, IAppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
            _customConfiguration = new CustomConfiguration(_appSettings, logger);
            LogCache = Environment.ExpandEnvironmentVariables(_appSettings.LogCache);
            PlexLog = Environment.ExpandEnvironmentVariables(_appSettings.PlexLog);
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
            _logger.Info("Job starting.");
            var reader = new LogReader(_logger, _appSettings, _customConfiguration);
            var songList = reader.ReadLog(PlexLog, LogCache);
            if (songList.Count > 0)
            {
                _logger.Info(songList.Count + " new song(s) found.");
                var scrobbler = new LastFmScrobbler(_logger, _appSettings, _customConfiguration);
                scrobbler.Scrobble(songList);
            }
            else
            {
                _logger.Info("No new songs found.");
            }
            _logger.Info("Job finished.");
        }
    }
}
