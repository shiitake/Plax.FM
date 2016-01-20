using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.Core.Utilities
{
    public class Hashing
    {
        public static string CalculateMD5Hash(string input)
        {
            //step 0 - make sure string is UTF-8 encoded
            input = ConvertToUtf8(input);
            
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static string ConvertToUtf8(string utf16String)
        {
            string utf8String = String.Empty;

            // Get the UTF16 bytes and convert them to UTF8 bytes
            byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf16String);
            byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);

            // Fill UTF8 bytes inside UTF8 string
            for (int i = 0; i < utf8Bytes.Length; i++)
            {
                // Because char always saves 2 bytes, fill char with 0
                byte[] utf8Container = new byte[2] { utf8Bytes[i], 0};
                utf8String += BitConverter.ToChar(utf8Container, 0);
            }

            return utf8String;
        }
    }
}
