using Ninject.Modules;

namespace PlaxFm.SystemTray.Config
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            Bind<SysTrayApp>().ToSelf();
            Bind<IServiceHandler>().To<ServiceHandler>().InSingletonScope();
            Bind<IInitializer>().To<Initializer>().InSingletonScope();
        }
    }
}
