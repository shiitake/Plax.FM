using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;

namespace PlaxFm.SystemTray
{
    public class SysTrayApp : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private readonly ServiceController _service;
        
        public SysTrayApp()
        {
            _service = new ServiceController("Plax.FM");
            
            trayMenu = new ContextMenu();
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
            Application.Exit();
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
