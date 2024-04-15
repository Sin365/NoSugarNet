namespace NoSugarNet.ClientCore.Manager
{
    public class LogManager
    {
        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="sk"></param>
        public delegate void OnLogHandler(int debuglv,string msg);

        /// <summary>  
        /// 内部输出
        /// </summary>
        public static event OnLogHandler OnLog;

        public void Debug(string str)
        {
            OnLog?.Invoke(0,str);
        }

        public void Warning(string str)
        {
            OnLog?.Invoke(1,str);
        }

        public void Error(string str)
        {
            OnLog?.Invoke(2,str);
        }
    }
}