using System;
using NLog;
using PlaxFm.Configuration;
using PlaxFm.Jobs;
using Quartz;
using Topshelf;
using Topshelf.Ninject;
using Topshelf.Quartz;
using Topshelf.Quartz.Ninject;
using Ninject.Modules;

namespace PlaxFm
{
    static class Program
    {
        static void Main(string[] args)
        {
            var logger = LogManager.GetCurrentClassLogger();
            IAppSettings appSettings = new AppSettings();

            try
            {
                HostFactory.Run(c =>
                {
                    c.UseNLog();
                    c.UseNinject(new AppModule());
                    c.UseQuartzNinject();
                    c.RunAsLocalSystem();
                    c.SetServiceName("Plax.FM");
                    c.SetDisplayName("Plax.FM Scrobbling Service");
                    c.StartAutomatically();

                    c.ScheduleQuartzJobAsService(q =>
                        q.WithJob(() =>
                            JobBuilder.Create<PlexJob>()
                                .WithDescription("Plex Media Server Log Reader Job")
                                .Build())
                            .AddTrigger(() =>
                                TriggerBuilder.Create()
                                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(appSettings.JobInterval).RepeatForever())
                                    .Build()));
                });
            }
            catch (Exception ex)
            {
                logger.Fatal("PlexJob service failed to start. {0}", ex.Message);
                throw;
            }
        }
    }
}
