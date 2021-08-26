using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OeipCommon.FileTransfer
{
    public class RequestItem
    {
        protected const int bufferSize = 8192;
        public string RemoteAddress { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string UserName { get; set; } = "anonymous";
        public string Password { get; set; } = "anonymous";

        public event Action<ProgressArgs> OnDownloadProgressEvent;

        public void SetPath(string remoteAddress, string relativePath)
        {
            RemoteAddress = remoteAddress.TrimEnd(' ', '\t', '\n');
            RelativePath = relativePath;
        }

        protected void OnDownloadProgress(ProgressArgs progress)
        {
            OnDownloadProgressEvent?.Invoke(progress);
        }
        public string RemoteUri
        {
            get
            {
                string remotePath = Path.Combine(RemoteAddress, RelativePath).Replace(Path.DirectorySeparatorChar, '/');
                return remotePath;
            }
        }

        public virtual bool IsHaveRemote(int timeout = -1)
        {
            return false;
        }

        public virtual string ReadRemote()
        {
            return string.Empty;
        }

        public virtual void DownloadRemote(string localPath, long totalSize = 0, long contentOffset = 0)
        {
        }

        public virtual bool CreateDirectory()
        {
            return false;
        }

        public virtual bool Delete()
        {
            return false;
        }

        public virtual bool Upload(string localPath)
        {
            return false;
        }

        public static RequestItem GetRequestItem(string remoteAddress)
        {
            Uri uri = new Uri(remoteAddress);
            switch (uri.Scheme.ToLowerInvariant())
            {
                case "ftp":
                    return new FtpRequestItem();
                case "http":
                case "https":
                    return new HttpRequestItem();
                default:
                    throw new ArgumentException($"{remoteAddress} not support.");
            }
        }
    }
}
