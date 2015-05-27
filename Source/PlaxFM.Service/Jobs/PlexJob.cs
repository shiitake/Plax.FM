﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Ninject.Extensions.Logging;
using PlaxFm.Configuration;
using PlaxFm.Models;
using Quartz;

namespace PlaxFm.Jobs
{
    public interface IPlexJob : IJob { }
    
    public class PlexJob : IPlexJob
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
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
            //check for initialization
            var initialized = _customConfiguration.GetValue()
            if (!initialized)
            {
                //initialy starting the service will create the config files so that system tray app can generate the session key
                _logger.Info("Account needs to be initialized before songs can be scrobbled.");
            }
            else
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
}
