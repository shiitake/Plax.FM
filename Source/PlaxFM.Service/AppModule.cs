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
            Bind<LogReader>().ToSelf();
            Bind<LastFmScrobbler>().ToSelf();
            Bind<CustomConfiguration>().ToSelf();
        }
    }
}
