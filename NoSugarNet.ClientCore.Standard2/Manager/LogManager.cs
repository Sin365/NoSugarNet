﻿namespace NoSugarNet.ClientCoreNet.Standard2.Manager
{
    public class LogManager
    {
        public enum E_LogType:byte
        {
            Info = 0,
            Debug = 1,
            Warning = 2,
            Error = 3,
        }
        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="sk"></param>
        public delegate void OnLogHandler(int debuglv,string msg);

        /// <summary>  
        /// 内部输出
        /// </summary>
        public static event OnLogHandler OnLog;

        public void Info(string str)
        {
            Log(E_LogType.Info, str);
        }

        public void Debug(string str)
        {
            Log(E_LogType.Debug, str);
        }

        public void Warning(string str)
        {
            Log(E_LogType.Warning, str);
        }

        public void Error(string str)
        {
            Log(E_LogType.Error, str);
        }

        public void Log(E_LogType logtype,string str)
        {
            OnLog?.Invoke((int)logtype, str);
        }
    }
}