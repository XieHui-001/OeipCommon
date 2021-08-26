using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static OeipCommon.FileTransfer.Download_failed;
using static OeipCommon.FileTransfer.LSinfo;
using static WinLiveManage.NewLoginfo;
using System.Runtime.InteropServices;
namespace OeipCommon.FileTransfer
{

    [Serializable]
    public class ProgressArgs
    {
        public string Message { get; set; }
        public float All { get; set; }
        public float Current { get; set; }
    }

    [Serializable]
    public enum ProgressType
    {
        File,
        FileList,
        Verify,
        Upload,
        UploadList,
    }
    /// <summary>
    /// 下载文件列表
    /// </summary>
    public class RequestList
    {
        private TransferList ItemList = new TransferList();
        //下载文件列表的网站根目录
        public string RemoteAddress { get; private set; } = string.Empty;
        //文件保存目录的根目录
        public string LocalDirectory { get; private set; } = string.Empty;
        //游戏下载过程中用于比较的临时文件，一般在LocalDirectory的上级中
        public string LocalBaseDirectory { get; set; } = string.Empty;
        //下载列表里的附加路径
        public string RemoteListParent { get; set; } = string.Empty;
        //远程机器里的下载列表文件路径
        public string RemoteDownloadList { get; set; } = string.Empty;
        //当前机器下载列表存放路径
        public string LocalDownloadList { get; set; } = string.Empty;
        public string LocalListCheck { get; set; } = string.Empty;
        //远程文件列表的MD5值，用来确定下载文件列表是否正确
        public string RemoteListCheck { get; set; } = string.Empty;
        //EXE路径
        public string EXEpathStr { get; set; } = string.Empty;
        //远程版本的相对路径
        public string RemoteVersion { get; set; } = string.Empty;

        //如果文件下载有误，最大更新次数
        public int DowndownCount { get; set; } = 5;

        //记录下载过程中的文件
        private string DownloadIndex
        {
            get
            {
                return Path.Combine(LocalBaseDirectory, "downloadindex.txt");
            }
        }

        private string DownLoadExe {
            get {
                return Path.Combine(LocalBaseDirectory + "\\Win64\\", "iVRealEdc.exe");
            }
        }
        private string DownLoadExe_
        {
            get
            {
                return Path.Combine(LocalBaseDirectory + "\\Win64\\", "AI_Demo.exe");
            }
        }
        private string DownLoadExe_S {
            get
            {
                return Path.Combine(LocalBaseDirectory + "\\Win64\\", "iVRealAI.exe");
            }
        }
        private string DownLoadAIpath {
            get {
                return Path.Combine(LocalBaseDirectory + "\\Win64\\", "iVRealAI");
            }
        }
        private string DownLoadiVRealEdc
        {
            get
            {
                return Path.Combine(LocalBaseDirectory + "\\Win64\\", "iVRealEdc");
            }
        }
        private string DownLoadiAI_Demo
        {
            get
            {
                return Path.Combine(LocalBaseDirectory + "\\Win64\\", "AI_Demo");
            }
            //Engine
        }
        private string DownLoadEngine
        {
            get
            {
                return Path.Combine(LocalBaseDirectory + "\\Win64\\", "Engine");
            }
            //Engine
        }

        /// <summary>
        /// false表示是文件具体进度,true表示整个文件夹具体进度
        /// </summary>
        public event Action<ProgressArgs, ProgressType> OnDownloadProgressEvent;

        private RequestItem requestItem = null;
        private object obj = new object();
        public RequestItem GetRequestItem()
        {
            Uri uri = new Uri(RemoteAddress);
            switch (uri.Scheme.ToLowerInvariant())
            {
                case "ftp":
                    return new FtpRequestItem();
                case "http":
                case "https":
                    return new HttpRequestItem();
                default:
                    throw new ArgumentException($"{RemoteAddress} not support.");
            }
        }

        public void SetDirectory(string rootAddress, string rootDirectory)
        {
            RemoteAddress = rootAddress.TrimEnd(' ', '\t', '\n');
            LocalDirectory = rootDirectory;
            requestItem = GetRequestItem();
            requestItem.OnDownloadProgressEvent += RequestItem_OnDownloadProgressEvent;
            if (!Directory.Exists(LocalDirectory))
            {
                Directory.CreateDirectory(LocalDirectory);
            }
            LocalBaseDirectory = Directory.GetParent(rootDirectory).FullName;
        }

        private void RequestItem_OnDownloadProgressEvent(ProgressArgs obj)
        {
            OnDownloadProgressEvent?.Invoke(obj, ProgressType.File);
        }

        //得到远程服务器所需求下载的文件列表
        public bool GetRemoteTransferList()
        {
            //测试变量
            string TestcheckList = "";
            //如果有检查
            string checkList = string.Empty;
            bool bCheck = true;
            if (!string.IsNullOrEmpty(RemoteListCheck))
            {
                requestItem.SetPath(RemoteAddress, RemoteListCheck);
                //下载文件列表的MD5值
                for (int i = 0; i < DowndownCount; i++)
                {
                    checkList = requestItem.ReadRemote().Trim();
                    if (checkList.Length != 32)
                    {
                        //记录MD5值为32位
                        LogHelper.LogMessage("file list check md5 not 32.", OeipLogLevel.OEIP_WARN);
                        //再尝试次
                        checkList = requestItem.ReadRemote().Trim();
                        bCheck = false;
                    }

                    else
                    {
                        LogHelper.LogMessage("get file list md5 success.");
                        bCheck = true;
                        break;
                    }
                }
                //保存实时更新 下载包体的服务器MD5值
                // Download_Error.Log("SeverHash:" + checkList);
            }
            if (bCheck)
            {
                //下载文件列表
                requestItem.SetPath(RemoteAddress, RemoteDownloadList);
                //保存在本地
                if (string.IsNullOrEmpty(LocalDownloadList))
                    LocalDownloadList = RemoteDownloadList;
                string localPath = Path.Combine(LocalBaseDirectory, LocalDownloadList);
                requestItem.DownloadRemote(localPath);
                //检查本地下载过后MD5值，并与服务器的比对是否正确
                if (!string.IsNullOrEmpty(checkList))
                {
                    for (int i = 0; i < DowndownCount; i++)
                    {
                        var localCheck = TransferHelper.GetStreamHash(localPath);
                        //  本地下载后MD5数值对比服务器不正确  进行重新下载
                        TestcheckList = localCheck;
                        if (localCheck != checkList)
                        {
                            LogHelper.LogMessage("file list check md5 not remote.", OeipLogLevel.OEIP_WARN);
                            requestItem.DownloadRemote(localPath);
                            bCheck = false;
                        }
                        // 本地MD5对比服务器正确    并记录MD5数值
                        else
                        {
                            LogHelper.LogMessage("check file list md5 success.");
                            bCheck = true;
                            break;
                        }
                    }
                }
                /*  Download_Error.Log("本地存放路径:" + localPath + ":ServeHash:"+ checkList);*/
                ItemList.LoadTransferItems(localPath);
            }
            return bCheck;
        }

        //获取远程版本
        public Version GetRemoteVersion()
        {
            requestItem.SetPath(RemoteAddress, RemoteVersion);
            string versionStr = requestItem.ReadRemote();
            if (Version.TryParse(versionStr, out Version version))
            {
                return version;
            }
            //解析远程版本失败
            LogHelper.LogMessage("failed to parse the remote version:" + versionStr);
            LogUtility.Log("解析远程版本失败--failed to parse the remote version:" + versionStr, LogType.Warning);
            return null;
        }
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int connectionDescription, int reservedValue);
        private bool IsConnected()
        {

            int I = 0;

            bool state = InternetGetConnectedState(out I, 0);

            return state;

        }
        public bool DownloadList()
        {
            try
            {
                lock (obj)
                {
                    ProgressArgs arg = new ProgressArgs();
                    arg.All = ItemList.TransferItems.Count;
                    int index = 0;
                    foreach (var fileItem in ItemList.TransferItems)
                    {
                        if (IsConnected())
                        {
                            arg.Current = index++;
                            arg.Message = Path.GetFileName(fileItem.RelativePath);
                            OnDownloadProgressEvent?.Invoke(arg, ProgressType.FileList);
                            DownloadItem(fileItem);
                        }
                        else {
                            writeItem("网络断线下载不完整");
                            break;
                        }
                    }
                    //字符串截取 定位开始坐标
                    int To_coordinate = EmpStr.IndexOf("D");
                    //字符串截取 定位结束坐标
                    int End_coordinates = EmpStr.IndexOf("Win64");
                    //整合字符串
                    var fullStr = EmpStr.Substring(To_coordinate, End_coordinates - To_coordinate);
                    var fullSs = fullStr + "Win64";
                    int length_st = Directory.GetFiles(fullSs + "\\", "*.*", SearchOption.AllDirectories).Length;
                    var SumPath = 0;
                    if (GetSeverPath.Count() <= length_st)
                    {
                        for (var i = 0; i < GetSeverPath.Count(); i++)
                        {
                            string PJpath = fullSs + "\\";
                            string dgPath = PJpath + GetSeverPath[i].ToString();
                            if (File.Exists(dgPath))
                            {
                                SumPath = 1;
                            }
                            else
                            {
                                writeItem(dgPath + ":下载不完整");
                                break;
                            }
                        }
                        if (SumPath >= 1)
                        {
                            //循环遍历   后 截取 fullSs 里面的包体名称 进行比较后可以得到是否全部存在
                            writeItem(string.Empty);
                            SumPath = 0;
                            GetSeverPath.ToList().Clear();
                        }
                    }
                    else
                    {
                        writeItem("本地文件与服务器存在差异");
                        GetSeverPath.ToList().Clear();
                    }
                    /*}
                    //下载完成后传入 downloadindex.txt文件为空
                    /*  Lsinfo.Log("完成");*/
                    /*  writeItem(string.Empty);*/
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("download list error:", ex);
                LogUtility.Log("download list error:" + ex, LogType.Warning);
                return false;
            }
        }

        /// <summary>
        /// 加载MD5   
        /// </summary>
        /// <param name="fileName">文件路径</param>
        /// <returns></returns>
        private static String ComputeMD5(String fileName)
        {
            String hashMD5 = String.Empty;
            //检查文件是否存在，如果文件存在则进行计算，否则返回空值
            if (System.IO.File.Exists(fileName))
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    //计算文件的MD5值
                    System.Security.Cryptography.MD5 calculator = System.Security.Cryptography.MD5.Create();
                    Byte[] buffer = calculator.ComputeHash(fs);
                    calculator.Clear();
                    //将字节数组转换成十六进制的字符串形式
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        stringBuilder.Append(buffer[i].ToString("x2"));
                    }
                    hashMD5 = stringBuilder.ToString();
                }//关闭文件流
            }//结束计算
            return hashMD5;
        }
        private List<string> GetSeverPath = new List<string>();
        private string EmpStr = string.Empty;
      
        public void DownloadItem(TransferItem fileItem)
        {
            try {
               
                string loaclHash_Ls = "";
                var localPath = Path.Combine(LocalDirectory, fileItem.RelativePath);
                var fileInfo = new FileInfo(localPath);
                EmpStr = fileInfo.ToString();
                GetSeverPath.Add(fileItem.RelativePath);
                if (string.IsNullOrEmpty(localPath))
                    throw new ArgumentException($"{localPath} is null");
                requestItem.SetPath(Path.Combine(RemoteAddress, RemoteListParent), fileItem.RelativePath);
                var directoryName = Path.GetDirectoryName(localPath);
                //如果没有这个目录，创建这个目录
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                //这个文件会记录当前下载文件    Game目录下，保存downloadindex.txt文件
                writeItem(fileItem.ToString());
                //如果文件存在
                if (File.Exists(localPath))
                {
                    if (IsConnected())
                    {
                        //如果文件不是一样大
                        if (fileInfo.Length != fileItem.Size)
                        {
                            Console.WriteLine("左边:" + fileInfo.Length + "右边:" + fileItem.Size);
                            //如果文件不一样大删除原有文件
                            File.Delete(localPath);
                            //下载新文件
                            requestItem.DownloadRemote(localPath, fileItem.Size);
                            //使用本地文件与服务器文件比较大小
                            if (fileItem.Size != fileInfo.Length)
                            {
                                LogHelper.LogMessage("第一次检测文件大小不同进行二次下载");
                                //如果文件不一样大删除原有文件
                                File.Delete(localPath);
                                //下载新文件
                                requestItem.DownloadRemote(localPath, fileItem.Size);
                            }
                        }
                        else
                        {
                            //文件虽然一样大，不过hash值不同，
                            string localHash = TransferHelper.GetStreamHash(localPath);
                            loaclHash_Ls = localHash;
                            if (localHash != fileItem.Hash)
                            {
                                Console.WriteLine("服务器的哈希" + localHash + "本地文件哈希" + fileItem.Hash);
                                LogHelper.LogMessage($"file {Path.GetFileName(fileItem.RelativePath)} hash not same remote.");
                                LogUtility.Log($"file {Path.GetFileName(fileItem.RelativePath)} hash not same remote.", LogType.Warning);
                                //哈希值不相等删除原有文件
                                File.Delete(localPath);
                                //下载新文件
                                requestItem.DownloadRemote(localPath, fileItem.Size);
                                Console.WriteLine("下载新文件的大小:" + fileItem.Size);
                            }
                            else if (fileItem.Hash != localHash)
                            {
                                //哈希值不相等删除原有文件
                                File.Delete(localPath);
                                LogUtility.Log("本地文件与服务器哈希值不一致进行第二次下载", LogType.Error);
                                //下载新文件
                                requestItem.DownloadRemote(localPath, fileItem.Size);
                            }
                        }
                    }
                    else {
                        writeItem("网络问题导致文件下载失败");
                    }

                    /*  Download_Error.Log("SeverLength:" + fileInfo.Length + ":SeverSize:" + fileItem.Size + ":LocalHash:" + loaclHash_Ls+":Sever:"+ fileItem.Hash);*/
                }
                //直接下载
                else
                {
                        requestItem.DownloadRemote(localPath, fileItem.Size);
                }
            }
            catch { }
        }

        //验证文件完整性
        public void Verify()
        {
            lock (obj) {
                try
                {
                    ProgressArgs arg = new ProgressArgs();
                    arg.All = ItemList.TransferItems.Count;
                    int index = 0;
                    //检查下载错误的文件
                    List<TransferItem> brokenFiles = new List<TransferItem>();
                    foreach (var fileItem in ItemList.TransferItems)
                    {
                        if (!fileItem.IsFileIntegrityIntact(LocalDirectory))
                        {
                            LogHelper.LogMessage($"file {Path.GetFileName(fileItem.RelativePath)} md5 not same remote.");
                            LogUtility.Log($"file {Path.GetFileName(fileItem.RelativePath)} md5 not same remote.", LogType.Warning);
                            //下载错误的文件
                            brokenFiles.Add(fileItem);
                        }
                        arg.Current = index++;
                        arg.Message = Path.GetFileName(fileItem.RelativePath);
                        OnDownloadProgressEvent?.Invoke(arg, ProgressType.Verify);
                    }
                    index = 0;
                    arg.All = brokenFiles.Count;
                    //错误文件重新下载
                    foreach (var fileItem in brokenFiles)
                    {
                        //最多重复下载DowndownCount次
                        for (int i = 0; i < DowndownCount; i++)
                        {
                            LogHelper.LogMessage($"file {Path.GetFileName(fileItem.RelativePath)} again download.");
                            LogUtility.Log($"file {Path.GetFileName(fileItem.RelativePath)} again download.", LogType.Warning);
                            DownloadItem(fileItem);
                            //判断已经下载完成没有问题后保存MD5值退出
                            if (fileItem.IsFileIntegrityIntact(LocalDirectory))
                            {
                                LogHelper.LogMessage($"file {Path.GetFileName(fileItem.RelativePath)} md5 same.");
                                LogUtility.Log($"file {Path.GetFileName(fileItem.RelativePath)} md5 same.", LogType.Trace);
                                break;
                            }
                            //有问题在进行重复下载
                            else {
                                LogHelper.LogMessage($"file {Path.GetFileName(fileItem.RelativePath)} again download.");
                                LogUtility.Log($"file {Path.GetFileName(fileItem.RelativePath)} again download.", LogType.Warning);
                                DownloadItem(fileItem);
                                break;
                            }
                        }
                        arg.Current = index++;
                        arg.Message = Path.GetFileName(fileItem.RelativePath);
                        OnDownloadProgressEvent?.Invoke(arg, ProgressType.FileList);
                    }
                    writeItem(string.Empty);
                }
                catch (Exception ex)
                {
                    //验证错误
                    LogHelper.LogMessageEx("Verify game error", ex);
                    LogUtility.Log("验证游戏失败-Verify game error" + ex, LogType.Error);
                }
            }
        }

        public async Task<bool> Upload(Func<string, bool> filterFunc)
        {
            try
            {
                //先同步创建服务器上的文件夹
                if (!createDirectory())
                {
                    return false;
                }
                string uploadListPath = Path.Combine(LocalBaseDirectory, LocalDownloadList);
                var TokenSource = new CancellationTokenSource();
                //生成上传文件列表与文件列表的MD5值
                await GenerateTransferListAsync(TokenSource.Token, filterFunc);
                ItemList.LoadTransferItems(uploadListPath);
                //服务器文件列表
                TransferList remoteList = new TransferList();
                requestItem.SetPath(RemoteAddress, RemoteDownloadList);
                string remoteListStr = requestItem.ReadRemote();
                //判断服务器文件列表不为空，并显示出来
                if (!string.IsNullOrEmpty(remoteListStr))
                {
                    byte[] array = Encoding.UTF8.GetBytes(remoteListStr);
                    MemoryStream stream = new MemoryStream(array);
                    remoteList.LoadTransferItems(stream);
                }
                //比较本地与服务器文件列表差异
                List<TransferItem> uploadList = new List<TransferItem>();
                foreach (var entry in ItemList.TransferItems)
                {
                    if (!remoteList.TransferItems.Contains(entry))
                    {
                        uploadList.Add(entry);
                    }
                }
                //开始上传文件
                ProgressArgs args = new ProgressArgs();
                args.All = uploadList.Count;
                var fileIndex = 0;
                foreach (var fileItem in uploadList)
                {
                    requestItem.SetPath(Path.Combine(RemoteAddress, RemoteListParent), fileItem.RelativePath);
                    var localPath = Path.Combine(LocalDirectory, fileItem.RelativePath);
                    var haveItem = remoteList.TransferItems.FirstOrDefault(p => p.RelativePath == fileItem.RelativePath);
                    if (haveItem != null)
                    {
                        requestItem.Delete();
                    }
                    requestItem.Upload(localPath);
                    args.Current = fileIndex++;
                    args.Message = Path.GetFileName(localPath);
                    OnDownloadProgressEvent?.Invoke(args, ProgressType.UploadList);
                }
                if (uploadList.Count > 0)
                {
                    //上传本地的列表文件
                    requestItem.SetPath(RemoteAddress, RemoteDownloadList);
                    if (requestItem.IsHaveRemote())
                    {
                        requestItem.Delete();
                    }
                    requestItem.Upload(Path.Combine(LocalBaseDirectory, LocalDownloadList));
                    if (!string.IsNullOrEmpty(RemoteListCheck))
                    {
                        //上传本地的列表文件的MD5
                        requestItem.SetPath(RemoteAddress, RemoteListCheck);
                        if (requestItem.IsHaveRemote())
                        {
                            requestItem.Delete();
                        }
                        requestItem.Upload(Path.Combine(LocalBaseDirectory, LocalListCheck));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("upload file list error:", ex);
                LogUtility.Log("上传文件错误---upload file list error:" + ex, LogType.Error);
                return false;
            }
        }
        public Task GenerateTransferListAsync(CancellationToken ct, Func<string, bool> filterFunc)
        {
            string uploadList = Path.Combine(LocalBaseDirectory, LocalDownloadList);
            string checkUploadList = Path.Combine(LocalBaseDirectory, LocalListCheck);
            return Task.Run(async () =>
            {
                var uploadPaths = new List<string>(Directory
                    .EnumerateFiles(this.LocalDirectory, "*", SearchOption.AllDirectories)
                    .Where(p => !filterFunc(p)));
                ProgressArgs args = new ProgressArgs();
                args.All = uploadPaths.Count;
                using (var tw = new StreamWriter(File.Create(uploadList, 4096, FileOptions.Asynchronous)))
                {
                    var completedFiles = 0;
                    foreach (var filePath in uploadPaths)
                    {
                        ct.ThrowIfCancellationRequested();

                        var newEntry = TransferHelper.CreateEntryForFile(LocalDirectory, filePath);

                        await tw.WriteLineAsync(newEntry.ToString());
                        await tw.FlushAsync();
                        args.Message = Path.GetFileName(filePath);
                        args.Current = completedFiles++;
                        OnDownloadProgressEvent?.Invoke(args, ProgressType.Upload);
                    }
                }
                if (!string.IsNullOrEmpty(RemoteListCheck))
                {
                    await CreateManifestChecksumAsync(uploadList, checkUploadList);
                }
            }, ct);
        }

        private async Task CreateManifestChecksumAsync(string manifestPath, string manifestChecksumPath)
        {
            // Create a checksum file for the manifest.
            using (var manifestStream = File.OpenRead(manifestPath))
            {
                var manifestHash = TransferHelper.GetStreamHash(manifestStream);

                using (var checksumStream = File.Create(manifestChecksumPath, 4096, FileOptions.Asynchronous))
                {
                    using (var tw = new StreamWriter(checksumStream))
                    {
                        await tw.WriteLineAsync(manifestHash);
                        await tw.FlushAsync();
                        tw.Close();
                    }
                }
            }
        }

        private bool createDirectory()
        {
            bool bCreate = true;
            //根目录是否存在
            requestItem.SetPath(RemoteAddress, string.Empty);
            if (!requestItem.IsHaveRemote())
            {
                bCreate = requestItem.CreateDirectory();
                if (!bCreate)
                {
                    return bCreate;
                }
            }
            //根目录下映射目录是否存在
            string[] paths = RemoteListParent.Split('/');
            for (int i = 0; i < paths.Length; i++)
            {
                string path = string.Empty;
                for (int j = 0; j <= i; j++)
                {
                    path += paths[j] + '/';
                }
                requestItem.SetPath(RemoteAddress, path);
                if (!requestItem.IsHaveRemote())
                {
                    bCreate = requestItem.CreateDirectory();
                    if (!bCreate)
                    {
                        return bCreate;
                    }
                }
            }
            //对应上传路径下的路径是否存在
            var dirs = Directory.GetDirectories(LocalDirectory, "*", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                var fullPath = Path.GetFullPath(dir).Replace(Path.DirectorySeparatorChar, '/');
                var halfPath = Path.GetFullPath(LocalDirectory).Replace(Path.DirectorySeparatorChar, '/');
                var relativePath = fullPath.Replace(halfPath, "").TrimStart('/');
                requestItem.SetPath(RemoteAddress, Path.Combine(RemoteListParent, relativePath));
                if (!requestItem.IsHaveRemote())
                {
                    bCreate = requestItem.CreateDirectory();
                    if (!bCreate)
                    {
                        return bCreate;
                    }
                }
            }
            return true;
        }

        private void writeItem(string item)
        {
            if (!Directory.Exists(LocalBaseDirectory))
                return;
            File.WriteAllText(DownloadIndex, item);
        }

        public bool IsComplete()
        {
            if (File.Exists(DownloadIndex))
            {
                string item = File.ReadAllText(DownloadIndex);
                return string.IsNullOrEmpty(item);
            }
            return true;
        }

        public bool ISPath() {
            if (File.Exists(DownLoadExe) || File.Exists(DownLoadExe_) || File.Exists(DownLoadExe_S)) {
                return true;
            }
            else {
                return false;
            }
        }
        public bool IsGame_Path() {
            if (Directory.Exists(DownLoadAIpath)&&Directory.Exists(DownLoadEngine) || Directory.Exists(DownLoadiVRealEdc) && Directory.Exists(DownLoadEngine) || Directory.Exists(DownLoadiAI_Demo) && Directory.Exists(DownLoadEngine))
            {
                return true;
            }
            else {
                return false;
            }
        }
        public bool IsHash()
        {
            if (ComputeMD5(LocalDownloadList) != RemoteListCheck)
            {
                return false;
            }
            else{
                return true;
            }
        }
    }
}
