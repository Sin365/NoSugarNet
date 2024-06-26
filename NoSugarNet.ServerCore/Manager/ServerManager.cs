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
        public static ForwardLocalClientManager g_ForwardLocal;
        public static ReverseLocalClientManager g_ReverseLocal;
        public static IOCPNetWork g_SocketMgr;
        public static System.Timers.Timer _SpeedCheckTimeTimer;//速度检测计时器
        public static int TimerInterval = 1000;//计时器间隔
        static long mLastReciveAllLenght = 0;
        static long mSendAllLenght = 0;
        static NetStatus Forward_NetStatus;
        static NetStatus Reverse_NetStatus;
        #region 委托和事件
        public delegate void OnUpdateStatusHandler(NetStatus ForwardStatus, NetStatus ReverseStatus);
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
            g_ForwardLocal = new ForwardLocalClientManager((E_CompressAdapter)compressAdapterType);
            g_ReverseLocal = new ReverseLocalClientManager();
            //g_SocketMgr = new IOCPNetWork(1024, 1024);
            g_SocketMgr = new IOCPNetWork(1024, 4096);

            Forward_NetStatus = new NetStatus();
            Reverse_NetStatus = new NetStatus();

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
            {

                g_ForwardLocal.GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght);
                g_ForwardLocal.GetClientCount(out int ClientUserCount, out int TunnelCount);
                NetStatus resutnetStatus = new NetStatus()
                {
                    TunnelCount = TunnelCount,
                    ClientUserCount = ClientUserCount,
                    srcSendAllLenght = resultSendAllLenght,
                    srcReciveAllLenght = resultReciveAllLenght,
                    srcReciveSecSpeed = (resultReciveAllLenght - Forward_NetStatus.srcReciveAllLenght) / (TimerInterval / 1000),
                    srcSendSecSpeed = (resultSendAllLenght - Forward_NetStatus.srcSendAllLenght) / (TimerInterval / 1000),
                    tSendAllLenght = g_ForwardLocal.tSendAllLenght,
                    tReciveAllLenght = g_ForwardLocal.tReciveAllLenght,
                    tSendSecSpeed = (g_ForwardLocal.tSendAllLenght - Forward_NetStatus.tSendAllLenght) / (TimerInterval / 1000),
                    tReciveSecSpeed = (g_ForwardLocal.tReciveAllLenght - Forward_NetStatus.tReciveAllLenght) / (TimerInterval / 1000),
                };
                Forward_NetStatus = resutnetStatus;
            }

            {

                g_ReverseLocal.GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght);
                g_ReverseLocal.GetClientCount(out int ClientUserCount, out int TunnelCount);
                NetStatus resutnetStatus = new NetStatus()
                {
                    TunnelCount = TunnelCount,
                    ClientUserCount = ClientUserCount,
                    srcSendAllLenght = resultSendAllLenght,
                    srcReciveAllLenght = resultReciveAllLenght,
                    srcReciveSecSpeed = (resultReciveAllLenght - Reverse_NetStatus.srcReciveAllLenght) / (TimerInterval / 1000),
                    srcSendSecSpeed = (resultSendAllLenght - Reverse_NetStatus.srcSendAllLenght) / (TimerInterval / 1000),
                    tSendAllLenght = g_ForwardLocal.tSendAllLenght,
                    tReciveAllLenght = g_ForwardLocal.tReciveAllLenght,
                    tSendSecSpeed = (g_ForwardLocal.tSendAllLenght - Reverse_NetStatus.tSendAllLenght) / (TimerInterval / 1000),
                    tReciveSecSpeed = (g_ForwardLocal.tReciveAllLenght - Reverse_NetStatus.tReciveAllLenght) / (TimerInterval / 1000),
                };
                Reverse_NetStatus = resutnetStatus;
            }

            OnUpdateStatus?.Invoke(Forward_NetStatus, Reverse_NetStatus);
        }

    }
}