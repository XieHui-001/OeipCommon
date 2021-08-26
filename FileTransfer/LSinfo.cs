using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OeipCommon.FileTransfer
{
   public class LSinfo
    {
        private static object obj = new object();
        /// <summary>
            /// 日志类型枚举
            /// </summary>
        public void CleanFile()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "log";
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            //.AddDays(-10)
            {
                if (file.LastWriteTime < DateTime.Now.AddDays(-10))
                {
                    file.Delete();
                }
            }
        }

        public enum LogType_E
        {
            /// <summary>
                    /// 一般输出
                    /// </summary>
            Trace,


            /// <summary>
                    /// 警告
                    /// </summary>
            Warning,


            /// <summary>
                    /// 错误
                    /// </summary>
            Error,


            /// <summary>
                    /// SQL语句
                    /// </summary>
            SQL
        }


        /// <summary>
            /// 课件名称ID获取
            /// </summary>
        public static class Lsinfo
        {
            private static readonly Thread LogThread;
            private static readonly ConcurrentQueue<string> LogQueue; //自定义线程安全的Queue
            private static readonly object SyncRoot;
            private static readonly string FilePath;


            /// <summary>
                    /// 因为线程是死循环读取队列,在没有日志数据的时候可能会消耗不必要的资源,所有当队列没有数据的时候用该类控制线程的(继续和暂停)
                    /// </summary>
            private static readonly AutoResetEvent AutoReset = null;


            static Lsinfo()
            {
                AutoReset = new AutoResetEvent(false);
                SyncRoot = new object();
                FilePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "LsInfo\\";
                LogThread = new Thread(WriteLog);
                LogQueue = new ConcurrentQueue<string>();
                LogThread.Start();
            }
            private static void DeleteBlank_Download_Failed()
            {
                lock (obj)
                {
                    try
                    {
                        string currentDirs = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string selflogs1 = Path.Combine(currentDirs, "Lsinfo/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                        var lines = File.ReadAllLines(selflogs1).Where(arg => !string.IsNullOrWhiteSpace(arg));
                        File.WriteAllLines(selflogs1, lines);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            /// <summary>
                    /// 记录日志
                    /// </summary>
                    /// <param name="msg">日志内容</param>
            public static void Log(string msg)
            {
              
                string _msg = string.Format("{0} : {1}", DateTime.Now.ToString("HH:mm:ss"), msg);
                DeleteBlank_Download_Failed();
                LogQueue.Enqueue(msg);
                AutoReset.Set();
                
            }

            /// <summary>
                    /// 记录日志
                    /// </summary>
                    /// <param name="msg">日志内容</param>
                    /// <param name="type">日志类型</param>
            public static void Log(string msg, LogType_E type)
            {
                    /*  string _msg = string.Format("{0} {1}: {2}", DateTime.Now.ToString("HH:mm:ss"), type, msg);*/
                    string _msg = string.Format("{0}", msg);
                    DeleteBlank_Download_Failed();
                    LogQueue.Enqueue(_msg);
                    AutoReset.Set();
                
            }


            /// <summary>
                    /// 记录日志
                    /// </summary>
                    /// <param name="ex">异常</param>
            public static void Log(Exception ex)
            {
                if (ex != null)
                {
                    string _newLine = string.Empty; //Environment.NewLine;
                    StringBuilder _builder = new StringBuilder();
                    _builder.AppendFormat("{0}: {1}{2}", DateTime.Now.ToString("HH:mm:ss"), ex.Message, _newLine);
                    _builder.AppendFormat("{0}{1}", ex.GetType(), _newLine);
                    _builder.AppendFormat("{0}{1}", ex.Source, _newLine);
                    _builder.AppendFormat("{0}{1}", ex.TargetSite, _newLine);
                    _builder.AppendFormat("{0}{1}", ex.StackTrace, _newLine);
                    LogQueue.Enqueue(_builder.ToString());
                    AutoReset.Set();
                }
            }


            /// <summary>
                    /// 写入日志
                    /// </summary>
            private static void WriteLog()
            {
                    StringBuilder strBuilder = new StringBuilder();
                    while (true)
                    {
                        if (LogQueue.Count() > 0)
                        {
                            string _msg;
                            Console.WriteLine(DateTime.Now.ToString("yyyyMMddhhmmssfff") + "---正在写入");
                            LogQueue.TryDequeue(out _msg);
                            if (!string.IsNullOrWhiteSpace(_msg))
                            {
                                //字符串拼接
                                strBuilder.Append(_msg).AppendLine();
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(strBuilder.ToString()))
                            {
                                Console.WriteLine(DateTime.Now.ToString("yyyyMMddhhmmssfff") + "---开始追加文件");
                                Monitor.Enter(SyncRoot);
                                if (!CreateDirectory()) continue;
                                string _path = string.Format("{0}{1}.log", FilePath, DateTime.Now.ToString("yyyy-MM-dd"));
                                Monitor.Exit(SyncRoot);
                                lock (SyncRoot)
                                {
                                    if (CreateFile(_path))
                                        ProcessWriteLog(_path, strBuilder.ToString()); //写入日志到文本
                                }
                                strBuilder.Clear();
                                Console.WriteLine(DateTime.Now.ToString("yyyyMMddhhmmssfff") + "---写入完毕");
                            }
                            Console.WriteLine("WaitOne等待信号量" + DateTime.Now.ToString("yyyyMMddhhmmssfff"));
                            //在这里,线程会被暂停,直到收到信号;
                            AutoReset.WaitOne();
                            Console.WriteLine("收到信号量,开始工作" + DateTime.Now.ToString("yyyyMMddhhmmssfff"));
                        }
                    }            }


            /// <summary>
                    /// 写入文件
                    /// </summary>
                    /// <param name="path">文件路径返回文件名</param>
                    /// <param name="msg">写入内容</param>
            private static void ProcessWriteLog(string path, string msg)
            {
                try
                {
                    StreamWriter _sw = File.AppendText(path);
                    //_sw.BaseStream.Seek(1, SeekOrigin.Current);
                    _sw.WriteLine(msg);
                    _sw.Flush();
                    _sw.Close();
                    _sw.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("写入日志失败，原因:{0}", ex.Message));
                }
            }


            /// <summary>
                    /// 创建文件
                    /// </summary>
                    /// <param name="path"></param>
                    /// <returns></returns>
            private static bool CreateFile(string path)
            {
                bool _result = true;
                try
                {
                    if (!File.Exists(path))
                    {
                        FileStream _files = File.Create(path);
                        _files.Close();
                        _files.Dispose();
                    }
                }
                catch (Exception)
                {
                    _result = false;
                }
                return _result;
            }


            /// <summary>
                    /// 创建文件夹
                    /// </summary>
                    /// <returns></returns>
            private static bool CreateDirectory()
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
