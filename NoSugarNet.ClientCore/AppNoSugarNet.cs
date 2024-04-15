using NoSugarNet.ClientCore.Manager;
using NoSugarNet.ClientCore.Network;
using ServerCore.Manager;
using static NoSugarNet.ClientCore.Manager.LogManager;

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
        static NetStatus netStatus;

        #region 委托和事件
        public delegate void OnUpdateStatusHandler(NetStatus Status);
        public static event OnUpdateStatusHandler OnUpdateStatus;
        #endregion

        public static void Init(string IP, int port, OnLogHandler onLog = null)
        {
            log = new LogManager();
            if(onLog != null)
                LogManager.OnLog += onLog;
            networkHelper = new NetworkHelper();
            login = new AppLogin();
            chat = new AppChat();
            local = new AppLocalClient();
            user = new UserDataManager();
            netStatus = new NetStatus();
            if (networkHelper.Init(IP, port))
			{
				_SpeedCheckTimeTimer = new System.Timers.Timer();
				_SpeedCheckTimeTimer.Interval = TimerInterval;
				_SpeedCheckTimeTimer.Elapsed += Checktimer_Elapsed;
				_SpeedCheckTimeTimer.AutoReset = true;
				_SpeedCheckTimeTimer.Enabled = true;
			}
        }

        static void Checktimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            local.GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght);
            local.GetClientCount(out int ClientUserCount, out int TunnelCount);

            NetStatus resutnetStatus = new NetStatus()
            {
                TunnelCount = TunnelCount,
                ClientUserCount = ClientUserCount,
                srcSendAllLenght = resultSendAllLenght,
                srcReciveAllLenght = resultReciveAllLenght,
                srcReciveSecSpeed = (resultReciveAllLenght - netStatus.srcReciveAllLenght) / (TimerInterval / 1000),
                srcSendSecSpeed = (resultSendAllLenght - netStatus.srcSendAllLenght) / (TimerInterval / 1000),
                tSendAllLenght = local.tSendAllLenght,
                tReciveAllLenght = local.tReciveAllLenght,
                tSendSecSpeed = (local.tSendAllLenght - netStatus.tSendAllLenght) / (TimerInterval / 1000),
                tReciveSecSpeed = (local.tReciveAllLenght - netStatus.tReciveAllLenght) / (TimerInterval / 1000),
            };
            netStatus = resutnetStatus;

            OnUpdateStatus?.Invoke(resutnetStatus);
        }
    }
}