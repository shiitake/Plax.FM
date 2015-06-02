using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.SystemTray
{
    public class ServiceCheck
    {
        
#if DEBUG
        private readonly static string _command = @"..\..\..\PlaxFM.Service\bin\Debug\plaxfm.exe";
#else
        private readonly static string _command = @"..\plaxfm.exe";
#endif
        public static bool DoesServiceExist(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
            return service != null;
        }

        public static void InstallService()
        {
            try
            {
                var procStartInfo = new System.Diagnostics.ProcessStartInfo(_command, "install");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                var proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                
                var result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex);
            }
        }

        public static void UnInstallService()
        {
            try
            {
                var procStartInfo = new System.Diagnostics.ProcessStartInfo(_command, "uninstall");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
