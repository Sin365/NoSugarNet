using NoSugarNet.ClientCore.Manager;
using NoSugarNet.ClientCore.Network;
using ServerCore.Manager;

namespace NoSugarNet.ClientCore
{
    public class AppNoSugarNet
    {
        public static string TokenStr;
        public static long RID = -1;
        public static string IP;
        public static int Port;
        public static LogManager log;
        public static NetworkHelper networkHelper;
        public static AppLogin login;
        public static AppChat chat;
        public static AppLocalClient local;
        public static UserDataManager user;
        public static System.Timers.Timer _SpeedCheckTimeTimer;//速度检测计时器
        public static int TimerInterval = 1000;//计时器间隔

        #region 委托和事件
        public delegate void OnUpdateStatusHandler(long resultReciveAllLenght, long resultSendAllLenght);
        public static event OnUpdateStatusHandler OnUpdateStatus;
        #endregion

        public static void Init(string IP, int port)
        {
            log = new LogManager();
            networkHelper = new NetworkHelper();
            login = new AppLogin();
            chat = new AppChat();
            local = new AppLocalClient();
            user = new UserDataManager();
            networkHelper.Init(IP, port);

            _SpeedCheckTimeTimer = new System.Timers.Timer();
            _SpeedCheckTimeTimer.Interval = TimerInterval;
            _SpeedCheckTimeTimer.Elapsed += Checktimer_Elapsed;
            _SpeedCheckTimeTimer.AutoReset = true;
            _SpeedCheckTimeTimer.Enabled = true;
        }

        static void Checktimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            local.GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght);
            OnUpdateStatus?.Invoke(resultReciveAllLenght, resultSendAllLenght);
        }
    }
}