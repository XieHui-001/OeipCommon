using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinLiveManage
{
  public  class NewLoginfo
    {
       //枚举日志类别
        public enum LogType
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
            /// 日志记录工具
            /// </summary>
        public static class LogUtility
        {
            private static readonly Thread LogThread;
            private static readonly ConcurrentQueue<string> LogQueue; //自定义线程安全的Queue
            private static readonly object SyncRoot;
            private static readonly string FilePath;


            /// <summary>
                    /// 因为线程是死循环读取队列,在没有日志数据的时候可能会消耗不必要的资源,所有当队列没有数据的时候用该类控制线程的(继续和暂停)
                    /// </summary>
            private static readonly AutoResetEvent AutoReset = null;


            static LogUtility()
            {
                AutoReset = new AutoResetEvent(false);
                SyncRoot = new object();
                FilePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "NewLog\\";
                LogThread = new Thread(WriteLog);
                LogQueue = new ConcurrentQueue<string>();
                LogThread.Start();
            }

            private static object obj = new object();
            /// <summary>
                    /// 记录日志
                    /// </summary>
                    /// <param name="msg">日志内容</param>
            public static void Log(string msg)
            {
               
                string _msg = string.Format("{0} : {1}", DateTime.Now.ToString("HHHH:mm:ss"), msg);
                LogQueue.Enqueue(msg);
                AutoReset.Set();
                
            }


            /// <summary>
                    /// 记录日志
                    /// </summary>
                    /// <param name="msg">日志内容</param>
                    /// <param name="type">日志类型</param>
            public static void Log(string msg, LogType type)
            {
             
                string _msg = string.Format("{0} {1}: {2}", DateTime.Now.ToString("HHHH:mm:ss"), type, msg);
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
                    _builder.AppendFormat("{0}: {1}{2}", DateTime.Now.ToString("HHHH:mm:ss"), ex.Message, _newLine);
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
                            Console.WriteLine(DateTime.Now.ToString("HHHH:mm:ss") + "---正在写入");
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
                                Console.WriteLine(DateTime.Now.ToString("HHHH:mm:ss") + "---开始追加文件");
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
                                Console.WriteLine(DateTime.Now.ToString("HHHH:mm:ss") + "---写入完毕");
                            }
                            Console.WriteLine("WaitOne等待信号量" + DateTime.Now.ToString("HHHH:mm:ss"));
                            //在这里,线程会被暂停,直到收到信号;
                            AutoReset.WaitOne();
                            Console.WriteLine("收到信号量,开始工作" + DateTime.Now.ToString("HHHH:mm:ss"));
                        }
                    }
            }


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
