using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Ninject.Extensions.Logging;
using PlaxFm.SystemTray.Config;

namespace PlaxFm.SystemTray
{
    public class SysTrayApp : Form
    {
        private readonly NotifyIcon _trayIcon;
        private readonly Initializer _init;
        private readonly ServiceHandler _handler;
        private static string _serviceName = "Plax.FM";
        private readonly ILogger _logger;
        private MenuItem _setupMenuItem;
        private MenuItem _startServiceMenuItem;
        private MenuItem _stopServiceMenuItem;
        public static string ServiceName
        {
            get { return _serviceName; }
        }
        
        public SysTrayApp(ILogger logger, ServiceHandler handler, Initializer init)
        {
            _logger = logger;
            _handler = handler;
            _init = init;
            _logger.Info("Starting PlaxFm System Tray");

            //if service isn't installed go ahead and install it
            var installed = _handler.IsServiceInstalled;
            if (!installed)
            {
                _handler.InstallService();
            }
            
            var lastFmSetup = _init.ConfirmLastFmSetup();
            var plexSetup = _init.ConfirmPlexSetup();
            var started = _handler.IsServiceStarted();

            var trayMenu = new ContextMenu();
            _setupMenuItem = new MenuItem("Initial Setup", InitialSetup) {Enabled = (!lastFmSetup || !plexSetup)};
            trayMenu.MenuItems.Add(_setupMenuItem);

            _startServiceMenuItem = new MenuItem("Start Plax.FM", StartService) {Enabled = !started};
            trayMenu.MenuItems.Add(_startServiceMenuItem);

            _stopServiceMenuItem = new MenuItem("Stop Plax.FM", StopService) {Enabled = started};
            trayMenu.MenuItems.Add(_stopServiceMenuItem);

            trayMenu.MenuItems.Add("Exit", OnExit);

            _trayIcon = new NotifyIcon
            {
                Text = @"Plax.FM",
                Icon = new Icon("favicon_white.ico", 40, 40),
                ContextMenu = trayMenu,
                Visible = true
            };
            _logger.Info("System tray started.");
        }



        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            var installed = _handler.IsServiceInstalled;
            if (installed)
            {
                _handler.UnInstallService();
            }
            _logger.Info("Closing Plax.FM System tray.");
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
            _logger.Info("Stopping PlaxFM service through tray application");
            _handler.StopService();
            _stopServiceMenuItem.Enabled = false;
            _startServiceMenuItem.Enabled = true;
        }

        private void StartService(object sender, EventArgs e)
        {
            _logger.Info("Starting PlaxFM service through tray application.");
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
