using System;
using NLog;
using PlexScrobble.Configuration;
using PlexScrobble.Jobs;
using Quartz;
using Topshelf;
using Topshelf.Ninject;
using Topshelf.Quartz;
using Topshelf.Quartz.Ninject;
using Ninject.Modules;

namespace PlexScrobble
{
    static class Program
    {
        static void Main(string[] args)
        {
            //var logger = LogManager.GetCurrentClassLogger();
            //IAppSettings appSettings = new AppSettings();

            try
            {
                HostFactory.Run(c =>
                {
                    //c.UseNLog();
                    c.UseNinject(new AppModule());
                    c.UseQuartzNinject();
                    //c.RunAsLocalSystem();

                    c.ScheduleQuartzJobAsService(q =>
                        q.WithJob(() =>
                            JobBuilder.Create<PlexJob>()
                                .WithDescription("Plex Media Server Log Reader Job")
                                .Build())
                            .AddTrigger(() =>
                                TriggerBuilder.Create()
                                    //.WithCronSchedule(appSettings.CronSchedule, cron => cron.WithMisfireHandlingInstructionDoNothing())
                                    .WithSimpleSchedule(x => x
                                        .WithIntervalInSeconds(60)
                                        .RepeatForever())
                                    .Build()));
                });
            }
            catch (Exception ex)
            {
                //logger.Fatal("My Job service failed to start. {0}", ex.Message);
                throw;
            }
        }
    }
}
