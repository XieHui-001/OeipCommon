using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static WinLiveManage.NewLoginfo;

namespace OeipCommon.FileTransfer
{
    /// <summary>
    /// 下载单个文件
    /// </summary>
    public class HttpRequestItem : RequestItem
    {

        private HttpWebRequest CreateWebRequest()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(new Uri(RemoteUri));
                request.Credentials = new NetworkCredential(UserName, Password);
                request.Timeout = 4000;
                return request;
            }
            catch (ArgumentException aex)
            {
                LogHelper.LogMessageEx("Unable to create a WebRequest for the specified file (ArgumentException): ", aex);
                LogUtility.Log("CreateWebRequest  Http无法为指定的文件创建WebRequest (ArgumentException)ERROR: Unable to create a WebRequest for the specified file (ArgumentException): " + aex, LogType.Error);
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("Unable to create a WebRequest for Error: ", ex);
                LogUtility.Log("CreateWebRequest  CreateWebRequest Http无法创建WebRequest Error" + ex, LogType.Error);
                return null;
            }
        }

        public override bool IsHaveRemote(int timeout = -1)
        {
            try
            {
                var request = CreateWebRequest();
                request.Method = WebRequestMethods.Http.Head;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("check uri error:", ex, OeipLogLevel.OEIP_WARN);
                LogUtility.Log("IsHaveRemote  Http获取Uri Error" + ex, LogType.Error);
            }
            return false;
        }

        public override string ReadRemote()//Encoder encoder = Encoding.UTF8
        {
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                var request = CreateWebRequest();
                request.Method = WebRequestMethods.Http.Get;
                using (var stream = request.GetResponse().GetResponseStream())
                {
                    if (stream == null)
                    {
                        LogHelper.LogMessage(RemoteUri + " no data.");
                        LogUtility.Log("ReadRemote   Http:" + RemoteUri+"No Date",LogType.Error);
                        return string.Empty;
                    }
                    var buffer = new byte[bufferSize];
                    while (true)
                    {
                        var bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx($"read {RemoteUri} error:", ex, OeipLogLevel.OEIP_WARN);
                LogUtility.Log($"Http: Read {RemoteUri} error:"+ex,LogType.Error);
            }
            return stringBuilder.ToString();
        }

        public override void DownloadRemote(string localPath, long totalSize = 0, long contentOffset = 0)
        {
            try
            {
                var request = CreateWebRequest();
                request.Method = WebRequestMethods.Http.Get;
                request.AddRange(contentOffset);
                using (var stream = request.GetResponse().GetResponseStream())
                {
                    if (stream == null)
                    {
                        LogHelper.LogMessage(RemoteUri + " no data.", OeipLogLevel.OEIP_WARN);
                        LogUtility.Log("DownloadRemote"+ RemoteUri + " no data.", LogType.Error);
                        return;
                    }
                    using (var fileStream = contentOffset > 0 ? new FileStream(localPath, FileMode.Append) :
                                                                       new FileStream(localPath, FileMode.Create))
                    {
                        fileStream.Position = contentOffset;
                        var totalBytesDownloaded = contentOffset;

                        long totalFileSize = totalSize; ;
                        if (stream.CanSeek)
                        {
                            totalFileSize = contentOffset + stream.Length;
                        }
                        var buffer = new byte[bufferSize];
                        while (true)
                        {
                            var bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                break;
                            }
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesDownloaded += bytesRead;
                            // Report download progress
                            ProgressArgs itemArgs = new ProgressArgs();
                            itemArgs.Message = Path.GetFileName(RemoteUri);
                            itemArgs.All = totalFileSize;
                            itemArgs.Current = totalBytesDownloaded;
                            OnDownloadProgress(itemArgs);
                        }
                        fileStream.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx($"read {RemoteUri} error:", ex);
                LogUtility.Log($"DownloadRemote read {RemoteUri} error:"+ex,LogType.Error);
            }
        }
    }
}
