using System;
using System.Collections.Generic;
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
        private readonly CustomConfiguration _customConfiguration;
        private readonly LogReader _reader;
        private readonly LastFmScrobbler _scrobbler;
        private readonly string _logCache;
        private readonly string _plexLog;
        
        public PlexJob(ILogger logger, IAppSettings appSettings, CustomConfiguration customConfiguration, LogReader reader, LastFmScrobbler scrobbler)
        {
            _logger = logger;
            var settings = appSettings;
            _customConfiguration = customConfiguration;
            _reader = reader;
            _scrobbler = scrobbler;
            _logCache = Environment.ExpandEnvironmentVariables(settings.LogCache);
            var userFolder = @"C:\Users\" + _customConfiguration.GetValue("Setup", "Profile");
            _plexLog = userFolder + settings.PlexLog;
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
            var initialized = _customConfiguration.UserConfirmed();
            if (!initialized)
            {
                _logger.Info("Account needs to be initialized before songs can be scrobbled.");
            }
            else
            {
                _logger.Info("Job starting.");
                //var songList = _reader.ReadLog(_plexLog, _logCache);
                var songList = new List<SongEntry>();
                var song1 = new SongEntry
                {
                    Artist = "Jack Pe&#195;&#177;ate",
                    Title = "When We Die",
                    TimePlayed = DateTime.Now
                };
                songList.Add(song1);

                var song2 = new SongEntry
                {
                    Artist = "Tiësto",
                    Title = "Just Be",
                    TimePlayed = DateTime.UtcNow
                };
                songList.Add(song2);

                
                var song3 = new SongEntry
                {
                    Artist = "The Mountain Goats",
                    Title = "No Children",
                    TimePlayed = DateTime.UtcNow
                };
                songList.Add(song3);

                if (songList.Count > 0)
                {
                    _logger.Info(songList.Count + " new song(s) found.");
                    _scrobbler.Scrobble(songList);
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
