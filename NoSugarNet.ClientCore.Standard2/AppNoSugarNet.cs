using NoSugarNet.Adapter.DataHelper;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore.Manager;
using NoSugarNet.ClientCore.Network;
using ServerCore.Manager;
using System.Collections.Generic;
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
        public static AppForwardLocalClient forwardlocal;
        public static AppReverseLocalClient reverselocal;
        public static UserDataManager user;
        public static System.Timers.Timer _SpeedCheckTimeTimer;//速度检测计时器
        public static int TimerInterval = 1000;//计时器间隔
        static NetStatus Forward_NetStatus;
        static NetStatus Reverse_NetStatus;

        #region 委托和事件
        public delegate void OnUpdateStatusHandler(NetStatus ForwardStatus, NetStatus ReverseStatus);
        public static event OnUpdateStatusHandler OnUpdateStatus;
        #endregion

        public static void Init(Dictionary<byte, TunnelClientData> cfgs, int compressAdapterType = 0,OnLogHandler onLog = null)
        {
            Config.cfgs = cfgs;
            Config.compressAdapterType = (E_CompressAdapter)compressAdapterType;

            log = new LogManager();
            if(onLog != null)
                LogManager.OnLog += onLog;
            networkHelper = new NetworkHelper();
            login = new AppLogin();
            chat = new AppChat();
            forwardlocal = new AppForwardLocalClient();
            reverselocal = new AppReverseLocalClient(Config.compressAdapterType);
            user = new UserDataManager();
            Forward_NetStatus = new NetStatus();
            Reverse_NetStatus = new NetStatus();
            _SpeedCheckTimeTimer = new System.Timers.Timer();
            _SpeedCheckTimeTimer.Interval = TimerInterval;
            _SpeedCheckTimeTimer.Elapsed += Checktimer_Elapsed;
            _SpeedCheckTimeTimer.AutoReset = true;
        }

        public static void Connect(string IP, int port)
        {
            if (networkHelper.Init(IP, port))
                _SpeedCheckTimeTimer.Enabled = true;
            else
                _SpeedCheckTimeTimer.Enabled = false;
        }

        public static void Close()
        {
            forwardlocal.StopAll();
            networkHelper.CloseConntect();
            AppNoSugarNet.log.Info("停止");
            _SpeedCheckTimeTimer.Enabled = false;
        }

        static void Checktimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            {
                forwardlocal.GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght);
                forwardlocal.GetClientCount(out int ClientUserCount, out int TunnelCount);
                NetStatus resutnetStatus = new NetStatus()
                {
                    TunnelCount = TunnelCount,
                    ClientUserCount = ClientUserCount,
                    srcSendAllLenght = resultSendAllLenght,
                    srcReciveAllLenght = resultReciveAllLenght,
                    srcReciveSecSpeed = (resultReciveAllLenght - Forward_NetStatus.srcReciveAllLenght) / (TimerInterval / 1000),
                    srcSendSecSpeed = (resultSendAllLenght - Forward_NetStatus.srcSendAllLenght) / (TimerInterval / 1000),
                    tSendAllLenght = forwardlocal.tSendAllLenght,
                    tReciveAllLenght = forwardlocal.tReciveAllLenght,
                    tSendSecSpeed = (forwardlocal.tSendAllLenght - Forward_NetStatus.tSendAllLenght) / (TimerInterval / 1000),
                    tReciveSecSpeed = (forwardlocal.tReciveAllLenght - Forward_NetStatus.tReciveAllLenght) / (TimerInterval / 1000),
                };
                Forward_NetStatus = resutnetStatus;
            }

            {
                reverselocal.GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght);
                reverselocal.GetClientCount(out int ClientUserCount, out int TunnelCount);
                NetStatus resutnetStatus = new NetStatus()
                {
                    TunnelCount = TunnelCount,
                    ClientUserCount = ClientUserCount,
                    srcSendAllLenght = resultSendAllLenght,
                    srcReciveAllLenght = resultReciveAllLenght,
                    srcReciveSecSpeed = (resultReciveAllLenght - Reverse_NetStatus.srcReciveAllLenght) / (TimerInterval / 1000),
                    srcSendSecSpeed = (resultSendAllLenght - Reverse_NetStatus.srcSendAllLenght) / (TimerInterval / 1000),
                    tSendAllLenght = reverselocal.tSendAllLenght,
                    tReciveAllLenght = reverselocal.tReciveAllLenght,
                    tSendSecSpeed = (reverselocal.tSendAllLenght - Reverse_NetStatus.tSendAllLenght) / (TimerInterval / 1000),
                    tReciveSecSpeed = (reverselocal.tReciveAllLenght - Reverse_NetStatus.tReciveAllLenght) / (TimerInterval / 1000),
                };
                Reverse_NetStatus = resutnetStatus;
            }

            OnUpdateStatus?.Invoke(Forward_NetStatus , Reverse_NetStatus);
        }
    }
}