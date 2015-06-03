using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Modules;
using Ninject.Extensions.Logging;

namespace PlaxFm.SystemTray
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            Bind<Ilo>()
        }
    }
}
