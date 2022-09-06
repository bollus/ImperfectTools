using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperfectTools.common
{
    public class FileTools
    {
        public static void WriteAllText(string path, string content, Encoding encoding) 
        {
            byte[] myByte = encoding.GetBytes(content);
            using (FileStream fsWrite = new(@path, FileMode.Create))
            {
                fsWrite.Write(myByte, 0, myByte.Length);
            };
        }

        public static string ReadAllText(string path, Encoding encoding)
        {
            using FileStream fsRead = new(@path, FileMode.Open);
            int fsLen = (int)fsRead.Length;
            byte[] heByte = new byte[fsLen];
            int r = fsRead.Read(heByte, 0, heByte.Length);
            return encoding.GetString(heByte);
        }
    }
}
