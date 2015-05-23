using Ninject.Extensions.Logging;
using Ninject.Modules;
using PlaxFm.Configuration;
using PlaxFm.Models;

namespace PlaxFm
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
