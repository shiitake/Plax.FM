using System.IO;
using System.Runtime.InteropServices;

namespace PlaxFm.Core.Utilities
{
    public class FileExtensions
    {
        public static bool IsFileLocked(string fileName)
        {
            try
            {
                using (File.Open(fileName, FileMode.Open)){}
            }
            catch (IOException e)
            {
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                return errorCode == 32 || errorCode == 33;
            }
            return false;
        }
    }
}
