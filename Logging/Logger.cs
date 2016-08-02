using System;
using System.IO;
using CannockAutomation.Properties;

namespace CannockAutomation.Logging
{
    public static class Logger
    {

        private static readonly Object LogLocker = new Object();
        private static readonly String LogFile = Configuration.LogFilePath;

        public static void Log(Object obj)
        {
            lock (LogLocker)
            {
                try
                {
                    using (var stream = File.AppendText(LogFile))
                    {
                        stream.Write(DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss,"));
                        stream.WriteLine(obj);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
