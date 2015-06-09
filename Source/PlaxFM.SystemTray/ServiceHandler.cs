using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Ninject.Extensions.Logging;

namespace PlaxFm.SystemTray
{
    public interface IServiceHandler
    { }
    
    public class ServiceHandler : IServiceHandler
    {
        private readonly ILogger _logger;
        private ServiceController _service;
        private const string BasePath64 = @"C:\Program Files (x86)";
        private const string BasePath32 = @"C:\Program Files";
        private const string FileLocation = @"\Shiitake Studios\Plax.Fm\Plax.FM.exe";
        private static string _command;
        private static bool _isServiceInstalled;
        public bool IsServiceInstalled
        {
            get { return _isServiceInstalled; } 
        }

        public ServiceHandler(ILogger logger)
        {
            _logger = logger;
            _service = new ServiceController("Plax.FM");
            _isServiceInstalled = DoesServiceExist();
            
#if DEBUG
            _command = @"..\..\..\PlaxFM.Service\bin\Debug\plax.fm.exe";
#else
            _command = GetServiceFilePath();
#endif
        }
        
        public bool DoesServiceExist(string serviceName = "Plax.FM")
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
                return service != null;
            }
            catch (Exception ex)
            {
                _logger.Error("There was an error looking for the service. " + ex);
                throw;
            }
        }

        public bool IsServiceStarted()
        {
            if (!IsServiceInstalled) return false;
            _service.Refresh();
            return _service.Status.Equals(ServiceControllerStatus.Running);
        }

        public bool IsServiceStopped()
        {
            if (!IsServiceInstalled) return false;
            _service.Refresh();
            return _service.Status.Equals(ServiceControllerStatus.Stopped);
        }
        
        public void InstallService()
        {
            try
            {
                _logger.Info("Installing PlaxFM service");
                var procStartInfo = new ProcessStartInfo(_command, "install");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                var proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                var result = proc.StandardOutput.ReadToEnd();
                _logger.Info(result);
                _isServiceInstalled = DoesServiceExist();
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.Error("Error when installing service. Error: " + ex);
            }
        }

        public void UnInstallService()
        {
            try
            {
                var isStarted = IsServiceStarted();
                if (isStarted)
                {
                    StopService();
                }
                _logger.Info("Uninstalling PlaxFM service");
                var procStartInfo = new ProcessStartInfo(_command, "uninstall");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                _logger.Info(result);
                _isServiceInstalled = DoesServiceExist();
            }
            catch (Exception ex)
            {
                _logger.Error("Error when uninstalling service. Error: " + ex);
            }
        }

        public void StopService()
        {
            try
            {
                var procStartInfo = new ProcessStartInfo(_command, "stop");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                _logger.Info(result);
                var timeout = TimeSpan.FromMilliseconds(60000);
                _service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch (Exception ex)
            {
                _logger.Error("Error when stopping service. Error: " + ex);
            }
        }

        public void StartService()
        {
            try
            {
                var procStartInfo = new ProcessStartInfo(_command, "start");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                _logger.Info(result);
                var timeout = TimeSpan.FromMilliseconds(60000);
                _service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch (Exception ex)
            {
                _logger.Error("Error when starting service. Error: " + ex);
            }
        }

        private string GetServiceFilePath()
        {
            _logger.Info("Getting service path");
            try
            {
                var fileInfo = new FileInfo(BasePath64 + FileLocation);
                if (!fileInfo.Exists)
                {
                    return BasePath32 + FileLocation;
                }
                return BasePath64 + FileLocation;
            }
            catch (Exception ex)
            {
                _logger.Error("There was a problem getting the service path location. " + ex);
                throw;
            }
        }
    }
}
