using NoSugarNet.ServerCore.Common;
using ServerCore.NetWork;
using ServerCore.Common;
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

        public static void InitServer(int port, Dictionary<byte, TunnelClientData> cfgs)
        {
            Config.Cfgs = cfgs;
            g_ClientMgr = new ClientManager();
            g_ClientMgr.Init(45000, 120);
            g_Log = new LogManager();
            g_Login = new LoginManager();
            g_Chat = new ChatManager();
            g_Local = new LocalClientManager();
            //g_SocketMgr = new IOCPNetWork(1024, 1024);
            g_SocketMgr = new IOCPNetWork(1024, 4096);
            g_SocketMgr.Init();
            g_SocketMgr.Start(new IPEndPoint(IPAddress.Any.Address, port));
            Console.WriteLine("Succeed!");
        }
    }
}