using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OeipCommon.FileTransfer
{
   public class File_creation
    {
        public static class Filepath
        {
            private static readonly string FilePath;
            private static readonly Thread LogThread;

            static Filepath()
            {
                FilePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "SteamVR_log";
                LogThread = new Thread(retur);
                LogThread.Start();
            }

            private static void retur()
            {
                if (!CreateDirectory())
                {
                }
            }
            public static bool CreateDirectory()
            {
                bool _result = true;
                try
                {
                    if (!Directory.Exists(FilePath))
                    {
                        Directory.CreateDirectory(FilePath);
                    }
                }
                catch (Exception)
                {
                    _result = false;
                }
                return _result;
            }
        }
    }
}
