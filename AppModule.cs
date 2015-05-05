using Ninject.Modules;
using PlexScrobble.Configuration;

namespace PlexScrobble
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IAppSettings>().To<AppSettings>();
        }
    }
}
