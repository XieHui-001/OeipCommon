using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OeipCommon
{
    public enum OeipLogLevel
    {
        OEIP_INFO,
        OEIP_WARN,
        OEIP_ERROR,
        OEIP_ALORT,
    }
    

    public static class LogHelper
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        private static void LogMessage(string message, LogLevel level, bool bConsole = true)
        {
            lock (obj) { 
            if (bConsole)
            {
                Console.ForegroundColor = ConsoleColor.White;
                if (level > LogLevel.Info && level <= LogLevel.Warn)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if (level > LogLevel.Warn)
                    Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("{0} {1} {2}", DateTime.Now.ToLongTimeString(), level, message);
            }
            Log.Log(level, message);
            }
        }
        private static object obj = new object();
        public static void LogMessage(string message)
        {
            lock (obj) { 
            LogMessage(message, LogLevel.Info);
            }
        }

      /*  public static void Newlog(LogType logtype) {
            LogLevel level = LogLevel.Info;
            switch (logtype) {
                case LogType.Trace:
                    level = LogLevel.Trace;
                    break;
                case LogType.Error:
                    level = LogLevel.Error;
                    break;
            }
        }*/

        public static void LogMessage(string message, OeipLogLevel logLevel, bool bConsole = true)
        {
            lock (obj) { 
            LogLevel level = LogLevel.Info;
            switch (logLevel)
            {
                case OeipLogLevel.OEIP_INFO:
                    level = LogLevel.Info;
                    break;
                case OeipLogLevel.OEIP_WARN:
                    level = LogLevel.Warn;
                    break;
                case OeipLogLevel.OEIP_ERROR:
                    level = LogLevel.Error;
                    break;
                case OeipLogLevel.OEIP_ALORT:
                    level = LogLevel.Fatal;
                    break;
            }
            LogMessage(message, level, bConsole);
            }
        }



        //输出报错信息
        public static void LogMessageEx(string message, Exception exception, OeipLogLevel logLevel = OeipLogLevel.OEIP_ERROR)
        {
            lock (obj) { 
            LogMessage(message + " " + exception.Message, logLevel);
            try
            {
                LogMessage(" source:" + exception.Source, logLevel);
            }
            catch (Exception ex)
            {
                LogMessage("get source ex:" + ex.Message, logLevel);
            }
            LogMessage(" stack:" + exception.StackTrace, logLevel);
            }
        }

        public static void LogFile(string message, OeipLogLevel logLevel = OeipLogLevel.OEIP_INFO)
        {
            lock (obj) { 
            LogMessage(message, logLevel, false);
            }
        }
    }
}
