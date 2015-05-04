using System;
using Ninject.Extensions.Logging;
using PlexScrobble.Configuration;
using PlexScrobble.Models;
using Quartz;

namespace PlexScrobble.Jobs
{
    public class PlexJob : IJob
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        private string OldLog = @"C:\temp\OldLog.log";
        private string NewLog = @"C:\Users\E002796\AppData\Local\Plex Media Server\Logs\Plex Media Server.log";

        public PlexJob(ILogger logger, IAppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error attempting ot start the job");
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
            var reader = new LogReader(NewLog, OldLog);
            reader.ReadLog();
            _logger.Debug("Job finished.");
        }
    }
}
