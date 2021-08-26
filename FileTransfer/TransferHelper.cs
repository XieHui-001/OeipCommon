using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static WinLiveManage.NewLoginfo;

namespace OeipCommon.FileTransfer
{
    public static class TransferHelper
    {
        public static string RemoveLineSeparatorsAndNulls(this string input)
        {
            return input?.Replace("\n", string.Empty).Replace("\0", string.Empty).Replace("\r", string.Empty);
        }
       
        public static string GetStreamHash(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;
            //获取哈希值
            using (Stream file = File.OpenRead(filePath))
            {
                return GetStreamHash(file);
            }
        }
        public static string GetStreamHash(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var result = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                return result;
            }
        }

        /// <summary>
        /// 检查文件是否完整下载
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool IsFileIntegrityIntact(this TransferItem item, string parent)
        {
            //检查路径
            var fullPath = Path.Combine(parent, item.RelativePath);
           
            if (!File.Exists(fullPath))
                LogUtility.Log($"检查路径错误: {fullPath} error:", LogType.Warning);
            return false;
            var fileInfo = new FileInfo(fullPath);
            //检查大小
            if (fileInfo.Length != item.Size)
                LogUtility.Log($"检查文件大小与服务器不一致--错误: {fullPath.Length}----{item.Size} error:", LogType.Warning);
            return false;
            using (Stream file = File.OpenRead(fullPath))
            {
                string localHash = GetStreamHash(file);
                //检查哈希值
                if (localHash != item.Hash)
                    LogUtility.Log($"检查本地与服务器哈希值不正确: {localHash}----{item.Hash} error:", LogType.Warning);
                return false;
            }
            return true;
        }

        public static TransferItem CreateEntryForFile(string parentDirectory, string filePath)
        {
            string hash;
            long fileSize;
            using (var fileStream = File.OpenRead(filePath))
            {
                hash = GetStreamHash(fileStream);
                fileSize = fileStream.Length;
            }

            var relativeFilePath = filePath.Substring(parentDirectory.Length);
            var newEntry = new TransferItem
            {
                RelativePath = relativeFilePath,
                Hash = hash,
                Size = fileSize
            };

            return newEntry;
        }
    }
}
