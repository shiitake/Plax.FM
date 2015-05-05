using System;
using Ninject.Extensions.Logging;
using PlexScrobble.Configuration;
using Quartz;

namespace PlexScrobble.Jobs
{
    public class MyJob : IJob
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;

        public MyJob(ILogger logger, IAppSettings appSettings)
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
            //job stuff
            _logger.Debug("Job finished.");
        }
    }
}
