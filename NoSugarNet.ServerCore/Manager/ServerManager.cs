using NoSugarNet.Adapter.DataHelper;
using NoSugarNet.ServerCore;
using NoSugarNet.ServerCore.Common;
using ServerCore.NetWork;
using System.Net;

namespace ServerCore.Manager
{

    public static class ServerManager
    {
        public static ClientManager g_ClientMgr;
        public static LogManager g_Log;
        public static LoginManager g_Login;
        public static ChatManager g_Chat;
        public static LocalClientManager g_Local;
        public static IOCPNetWork g_SocketMgr;
        public static System.Timers.Timer _SpeedCheckTimeTimer;//速度检测计时器
        public static int TimerInterval = 1000;//计时器间隔
        static long mLastReciveAllLenght = 0;
        static long mSendAllLenght = 0;
        static NetStatus netStatus;
        #region 委托和事件
        public delegate void OnUpdateStatusHandler(NetStatus Status);
        public static event OnUpdateStatusHandler OnUpdateStatus;
        #endregion

        public static void InitServer(int port, Dictionary<byte, TunnelClientData> cfgs,int compressAdapterType = 1)
        {
            Config.cfgs = cfgs;
            Config.compressAdapterType = (E_CompressAdapter)compressAdapterType;
            g_ClientMgr = new ClientManager();
            g_ClientMgr.Init(45000, 120);
            g_Log = new LogManager();
            g_Login = new LoginManager();
            g_Chat = new ChatManager();
            g_Local = new LocalClientManager((E_CompressAdapter)compressAdapterType);
            //g_SocketMgr = new IOCPNetWork(1024, 1024);
            g_SocketMgr = new IOCPNetWork(1024, 4096);

            netStatus = new NetStatus();

            g_SocketMgr.Init();
            g_SocketMgr.Start(new IPEndPoint(IPAddress.Any.Address, port));
            Console.WriteLine("Succeed!");

            _SpeedCheckTimeTimer = new System.Timers.Timer();
            _SpeedCheckTimeTimer.Interval = TimerInterval;
            _SpeedCheckTimeTimer.Elapsed += Checktimer_Elapsed;
            _SpeedCheckTimeTimer.AutoReset = true;
            _SpeedCheckTimeTimer.Enabled = true;
        }

        static void Checktimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            g_Local.GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght);
            g_Local.GetClientCount(out int ClientUserCount, out int TunnelCount);
            NetStatus resutnetStatus = new NetStatus()
            {
                TunnelCount = TunnelCount,
                ClientUserCount = ClientUserCount,
                srcSendAllLenght = resultSendAllLenght,
                srcReciveAllLenght = resultReciveAllLenght,
                srcReciveSecSpeed = (resultReciveAllLenght - netStatus.srcReciveAllLenght) / (TimerInterval / 1000),
                srcSendSecSpeed = (resultSendAllLenght - netStatus.srcSendAllLenght) / (TimerInterval / 1000),
                tSendAllLenght = g_Local.tSendAllLenght,
                tReciveAllLenght = g_Local.tReciveAllLenght,
                tSendSecSpeed = (g_Local.tSendAllLenght - netStatus.tSendAllLenght) / (TimerInterval / 1000),
                tReciveSecSpeed = (g_Local.tReciveAllLenght - netStatus.tReciveAllLenght) / (TimerInterval / 1000),
            };
            netStatus = resutnetStatus;
            OnUpdateStatus?.Invoke(resutnetStatus);
        }

    }
}