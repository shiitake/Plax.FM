using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.Core.Utilities
{
    public class StringHelper
    {
        public static string EncodeToUtf8(string myString)
        {
            if (myString == null) return String.Empty;
            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(myString);
            var newString = System.Text.Encoding.UTF8.GetString(utf8Bytes);
            return newString;
        }
    }
}
