using System;
using System.Windows.Forms;
using Ninject;
using PlaxFm.SystemTray.Config;

namespace PlaxFm.SystemTray
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var kernel = new StandardKernel(new AppModule());
            var tray = kernel.Get<SysTrayApp>();
            Application.EnableVisualStyles();
            Application.Run(tray);
        }
    }
}
