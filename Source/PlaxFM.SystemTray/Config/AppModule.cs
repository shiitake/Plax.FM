using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Modules;

namespace PlaxFm.SystemTray.Config
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IServiceHandler>().To<ServiceHandler>();
            Bind<IInitializer>().To<Initializer>();
        }
    }
}
