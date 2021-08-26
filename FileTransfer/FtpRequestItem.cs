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
    public class FtpRequestItem : RequestItem
    {
     
        private FtpWebRequest CreateWebRequest()
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(new Uri(RemoteUri));
                request.Proxy = null;
                request.UsePassive = true;
                request.UseBinary = true;
                request.Timeout = 4000;

                request.Credentials = new NetworkCredential(UserName, Password);
                return request;
            }
            catch (ArgumentException aex)
            {
                //判断网址不正确后报错
                LogHelper.LogMessageEx("Unable to create a WebRequest for the specified file (ArgumentException): ", aex);
                LogUtility.Log("网址不正确-ERROE: nable to create a WebRequest for the specified file (ArgumentException): " + aex, LogType.Error);
                return null;
            }
            catch (Exception ex)
            {
                //网站错误无法进行创建
                LogHelper.LogMessageEx("Unable to create a WebRequest for Error: ", ex);
                LogUtility.Log("网址无法创建-ERROE:Unable to create a WebRequest for Error: " + ex, LogType.Error);
                return null;
            }
        }

        public override bool IsHaveRemote(int timeout = -1)
        {
            try
            {
                var request = CreateWebRequest();
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Timeout = timeout > 0 ? timeout : 4000;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == FtpStatusCode.OpeningData || response.StatusCode == FtpStatusCode.DataAlreadyOpen)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("check uri error:", ex, OeipLogLevel.OEIP_WARN);
                LogUtility.Log("检查Url错误-check uri error: " + ex, LogType.Warning);
            }
            return false;
        }

        public override string ReadRemote()
        {
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                var request = CreateWebRequest();
                var sizerequest = CreateWebRequest();
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;
                using (var stream = request.GetResponse().GetResponseStream())
                {
                    if (stream == null)
                    {
                        LogHelper.LogMessage(RemoteUri + " no data.");
                        LogUtility.Log("远程服务器:Url"+RemoteUri + "no data ", LogType.Warning);
                        return string.Empty;
                    }
                    long fileSize;
                    using (var sizeResponse = (FtpWebResponse)sizerequest.GetResponse())
                    {
                        fileSize = sizeResponse.ContentLength;
                    }
                    if (fileSize < bufferSize)
                    {
                        var smallBuffer = new byte[fileSize];
                        stream.Read(smallBuffer, 0, smallBuffer.Length);
                        stringBuilder.Append(Encoding.UTF8.GetString(smallBuffer, 0, smallBuffer.Length));
                    }
                    else
                    {
                        var buffer = new byte[bufferSize];
                        while (true)
                        {
                            int bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                break;
                            }
                            stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx($"read {RemoteUri} error:", ex, OeipLogLevel.OEIP_WARN);
                LogUtility.Log($"读取:Read {RemoteUri} error:"+ex, LogType.Warning);
            }
            return stringBuilder.ToString();
        }

        public override void DownloadRemote(string localPath, long totalSize = 0, long contentOffset = 0)
        {
            try
            {
                var request = CreateWebRequest();
                var sizerequest = CreateWebRequest();
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.ContentOffset = contentOffset;

                sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;
                using (var contentStream = request.GetResponse().GetResponseStream())
                {
                    if (contentStream == null)
                    {
                        LogHelper.LogMessage(RemoteUri + " no data.", OeipLogLevel.OEIP_WARN);
                        return;
                    }
                    var fileSize = contentOffset;
                    using (var sizereader = (FtpWebResponse)sizerequest.GetResponse())
                    {
                        fileSize += sizereader.ContentLength;
                    }
                    using (var fileStream = contentOffset > 0 ?
                        new FileStream(localPath, FileMode.Append) : new FileStream(localPath, FileMode.Create))
                    {
                        fileStream.Position = contentOffset;
                        var totalBytesDownloaded = contentOffset;

                        if (fileSize < bufferSize)
                        {
                            var smallBuffer = new byte[fileSize];
                            contentStream.Read(smallBuffer, 0, smallBuffer.Length);
                            fileStream.Write(smallBuffer, 0, smallBuffer.Length);
                            totalBytesDownloaded += smallBuffer.Length;
                            ProgressArgs itemArgs = new ProgressArgs();
                            itemArgs.Message = Path.GetFileName(RemoteUri);
                            itemArgs.All = fileSize;
                            itemArgs.Current = totalBytesDownloaded;
                            OnDownloadProgress(itemArgs);
                        }
                        else
                        {
                            var buffer = new byte[bufferSize];
                            while (true)
                            {
                                var bytesRead = contentStream.Read(buffer, 0, buffer.Length);
                                if (bytesRead == 0)
                                {
                                    break;
                                }
                                fileStream.Write(buffer, 0, bytesRead);
                                totalBytesDownloaded += bytesRead;
                                // Report download progress
                                ProgressArgs itemArgs = new ProgressArgs();
                                itemArgs.Message = Path.GetFileName(RemoteUri);
                                itemArgs.All = fileSize;
                                itemArgs.Current = totalBytesDownloaded;
                                OnDownloadProgress(itemArgs);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx($"Read {RemoteUri} error:", ex);
                LogUtility.Log($"读取:Read {RemoteUri} error:" + ex, LogType.Warning);
            }
        }

        public override bool CreateDirectory()
        {
            try
            {
                var request = CreateWebRequest();
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Timeout = 4000;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("create directory error:", ex);
                LogUtility.Log($"创建目录失败: create directory error: " + ex, LogType.Error);
            }
            return false;
        }
        //删除FTP文件
        public override bool Delete()
        {
            try
            {
                var request = CreateWebRequest();
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Timeout = 4000;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("delete file error:", ex);
                LogUtility.Log("删除FTP文件错误: delete file error: " + ex, LogType.Error);
            }
            return false;
        }

        public override bool Upload(string localPath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(localPath);

                var request = CreateWebRequest();
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.KeepAlive = false;
                request.UseBinary = true;
                request.ContentLength = fileInfo.Length;
                request.Timeout = 4000;
                byte[] buff = new byte[bufferSize];
                using (Stream stream = request.GetRequestStream())
                {
                    using (FileStream fs = fileInfo.OpenRead())
                    {
                        int contentLen = fs.Read(buff, 0, bufferSize);
                        while (contentLen != 0)
                        {
                            stream.Write(buff, 0, contentLen);
                            contentLen = fs.Read(buff, 0, bufferSize);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("upload file error:", ex);
                LogUtility.Log("FTP上传文件错误: upload file error: " + ex, LogType.Error);
            }
            return false;
        }
    }
}
