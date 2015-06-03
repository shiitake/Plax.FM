using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;
using PlaxFm.SystemTray.Config;
using NLog;

namespace PlaxFm.SystemTray
{
    public class SysTrayApp : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private readonly ServiceController _service;
        private Initialization _init;
        private readonly string ServiceName = "Plax.FM";
        private static Logger logger;
        private ServiceHandler _handler;
        
        public SysTrayApp()
        {
#if DEBUG
            logger = LogManager.GetLogger("debug");
#else
            logger = LogManager.GetLogger("release");
#endif
            logger.Info("Starting PlaxFm System Tray");
            _handler = new ServiceHandler(logger);
            //if service isn't installed go ahead and install it
            var installed = ServiceHandler.DoesServiceExist(ServiceName);
            if (!installed)
            {
                ServiceHandler.InstallService();
            }

            _service = new ServiceController("Plax.FM");
            _init = new Initialization();
            var lastFmSetup = _init.ConfirmLastFmSetup();
            var plexSetup = _init.ConfirmPlexSetup();

            trayMenu = new ContextMenu();
            var setupMenu = new MenuItem("Initial Setup", InitialSetup);
            setupMenu.Enabled = (!lastFmSetup || !plexSetup);
            trayMenu.MenuItems.Add(setupMenu);
            trayMenu.MenuItems.Add("Start Plax.FM", StartService);
            trayMenu.MenuItems.Add("Stop Plax.FM", StopService);
            trayMenu.MenuItems.Add("Exit", OnExit);
            
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Plax.FM";
            trayIcon.Icon = new Icon("favicon.ico", 40, 40);

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            var installed = ServiceHandler.DoesServiceExist(ServiceName);
            if (installed)
            {
                ServiceHandler.UnInstallService();
            }
            Application.Exit();
        }

        private void InitialSetup(object sender, EventArgs e)
        {
            var plexSetup = _init.ConfirmPlexSetup();
            if (!plexSetup)
            {
                using (var popup = new PlaxConfig())
                {
                    if (popup.ShowDialog(this) == DialogResult.OK)
                    {
                        var userInfo = popup.UserInfo;
                        _init.Setup(userInfo[0], userInfo[1]);
                    }
                }
            }
            else
            {
                _init.Setup();
            }
        }
        
        private void StopService(object sender, EventArgs e)
        {
            try
            {
                var timeout = TimeSpan.FromMilliseconds(60000);

                _service.Stop();
                _service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch
            {
                // ...
            }
        }

        private void StartService(object sender, EventArgs e)
        {
            try
            {
                var timeout = TimeSpan.FromMilliseconds(60000);

                _service.Start();
                _service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch
            {
                // ...
            } 
        }
        
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
