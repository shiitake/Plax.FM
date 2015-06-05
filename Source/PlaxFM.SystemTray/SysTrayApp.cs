using System;
using System.Drawing;
using System.Windows.Forms;
using Ninject;
using Ninject.Extensions.Logging;
using PlaxFm.SystemTray.Config;
using Ninject.Extensions.Logging.NLog2;
using NLog;

namespace PlaxFm.SystemTray
{
    public class SysTrayApp : Form
    {
        private readonly NotifyIcon _trayIcon;
        private readonly Initializer _init;
        private const string ServiceName = "Plax.FM";
        private readonly Logger _logger;
        private readonly ServiceHandler _handler;
        private MenuItem _setupMenuItem;
        private MenuItem _startServiceMenuItem;
        private MenuItem _stopServiceMenuItem;
        
        public SysTrayApp()
        {
            var kernel = new StandardKernel(new AppModule());

#if DEBUG
            _logger = LogManager.GetLogger("debug");
#else
            _logger = LogManager.GetLogger("release");
#endif
            _logger.Info("Starting PlaxFm System Tray");

            _handler = kernel.Get<ServiceHandler>();
            //if service isn't installed go ahead and install it
            var installed = _handler.DoesServiceExist(ServiceName);
            if (!installed)
            {
                _handler.InstallService();
            }

            _init = new Initializer(_logger);
            var lastFmSetup = _init.ConfirmLastFmSetup();
            var plexSetup = _init.ConfirmPlexSetup();

            var trayMenu = new ContextMenu();
            _setupMenuItem = new MenuItem("Initial Setup", InitialSetup);
            _setupMenuItem.Enabled = (!lastFmSetup || !plexSetup);
            trayMenu.MenuItems.Add(_setupMenuItem);

            _startServiceMenuItem = new MenuItem("Start Plax.FM", StartService);
            _startServiceMenuItem.Enabled = !_handler.IsServiceStarted();
            trayMenu.MenuItems.Add(_startServiceMenuItem);

            _stopServiceMenuItem = new MenuItem("Stop Plax.FM", StopService);
            _stopServiceMenuItem.Enabled = _handler.IsServiceStarted();
            trayMenu.MenuItems.Add(_stopServiceMenuItem);

            trayMenu.MenuItems.Add("Exit", OnExit);
            
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Plax.FM";
            _trayIcon.Icon = new Icon("favicon.ico", 40, 40);

            _trayIcon.ContextMenu = trayMenu;
            _trayIcon.Visible = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            var installed = _handler.DoesServiceExist(ServiceName);
            if (installed)
            {
                _handler.UnInstallService();
            }
            Application.Exit();
        }

        private void InitialSetup(object sender, EventArgs e)
        {
            var plexSetup = _init.ConfirmPlexSetup();
            try
            {
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
            catch (Exception ex)
            {
                _logger.Error("Initial configuration did not complete. " + ex);
                throw;
            }
            finally
            {
                _setupMenuItem.Enabled = false;    
            }
        }
        
        private void StopService(object sender, EventArgs e)
        {
            _handler.StopService();
            _stopServiceMenuItem.Enabled = false;
            _startServiceMenuItem.Enabled = true;
        }

        private void StartService(object sender, EventArgs e)
        {
            _handler.StartService();
            _startServiceMenuItem.Enabled = false;
            _stopServiceMenuItem.Enabled = true;
        }
        
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                _trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
