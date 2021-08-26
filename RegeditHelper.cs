using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OeipCommon
{
    public static class RegeditHelper
    {
        public static void RegeditCom(string comdll)
        {
            string regasmPath = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\regasm ";
            string comRegedit = "@echo start regedit " + Environment.NewLine +
                regasmPath + comdll + "/u" + Environment.NewLine + //regasmPath + comdll + Environment.NewLine +
                regasmPath + comdll + "/verbose /tlb /codebase" + Environment.NewLine +
                "@echo end" + Environment.NewLine +
                "timeout 5" + Environment.NewLine;
            var dllRegisteredPath = Path.Combine(System.Environment.CurrentDirectory, "Resources", "regeditcom.bat");
            File.WriteAllText(dllRegisteredPath, comRegedit);
            var updateShellProcess = new ProcessStartInfo
            {
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = dllRegisteredPath,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal,
                Verb = "RunAs",
            };
            Process.Start(updateShellProcess);
        }
    }
}
