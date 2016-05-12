using System;
using System.Net;
using System.IO.IsolatedStorage;
using System.Text;
using System.IO;

namespace S3ServerChecker
{
    class EventLog
    {
        static Object thisLock = new Object();

        public static string FilePath { get; set; }

        public static void Write(string format, params object[] arg)
        {
            Write(string.Format(format, arg));
        }

        public static void Write(string message)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                FilePath = Directory.GetCurrentDirectory();
            }
            string filename = FilePath + string.Format("\\{0:yyyy}\\{0:MM}\\{0:yyyy-MM-dd}.txt", DateTime.Now);
            FileInfo finfo = new FileInfo(filename);
            if (finfo.Directory.Exists == false)
            {
                finfo.Directory.Create();
            }

            lock (thisLock)
            {
                string writeString = string.Format("{0:yyyy/MM/dd HH:mm:ss.fff} {1}", DateTime.Now, "\t" + System.Threading.Thread.CurrentThread.ManagedThreadId + "\t" + message) + Environment.NewLine;
                Console.WriteLine(writeString);
                File.AppendAllText(filename, writeString, Encoding.Unicode);
            }
        }
    }
}
