using Ninject.Modules;
using PlexScrobble.Configuration;
using PlexScrobble.Models;

namespace PlexScrobble
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IAppSettings>().To<AppSettings>();
            Bind<ILogReader>().To<LogReader>();
            Bind<ILastFmScrobbler>().To<LastFmScrobbler>();
            Bind<ICustomConfiguration>().To<CustomConfiguration>();
        }
    }
}
