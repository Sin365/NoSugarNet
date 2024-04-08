using NoSugarNet.ServerCore;
using NoSugarNet.ServerCore.Common;
using ServerCore.Manager;

namespace NoSugarNet.ServerCli
{
    internal class Program
    {
        static string Title = "NoSugarNetServer";
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (!Config.LoadConfig())
            {
                Console.WriteLine("配置文件错误");
                Console.ReadLine();
                return;
            }
            Dictionary<byte, TunnelClientData> dictTunnel = new Dictionary<byte, TunnelClientData>();
            for (int i = 0; i < Config.cfg.TunnelList.Count; i++)
            {
                ConfigDataModel_Single cfgSingle = Config.cfg.TunnelList[i];
                dictTunnel[(byte)i] = new TunnelClientData()
                {
                    TunnelId = (byte)i,
                    ServerLocalTargetIP = cfgSingle.ServerLocalTargetIP,
                    ServerLocalTargetPort = (ushort)cfgSingle.ServerLocalTargetPort,
                    ClientLocalPort = (ushort)cfgSingle.ClientLocalPort,
                };
            }

            ServerManager.OnUpdateStatus += OnUpdateStatus;
            ServerManager.InitServer(Config.cfg.ServerPort, dictTunnel,Config.cfg.CompressAdapterType);

            while (true) 
            {
                Console.ReadLine();
            }
        }

        static void OnUpdateStatus(NetStatus netState)
        {
            //string info = $"User:{netState.ClientUserCount}   Tun:{netState.TunnelCount}    rec:{netState.srcReciveAllLenght}|{netState.tReciveAllLenght}   {ConvertBytesToKilobytes(netState.srcReciveSecSpeed)}K/s|{ConvertBytesToKilobytes(netState.tReciveSecSpeed)}K/s   send:{netState.srcSendAllLenght}|{netState.tSendAllLenght} {ConvertBytesToKilobytes(netState.srcSendSecSpeed)}K/s|{ConvertBytesToKilobytes(netState.tSendSecSpeed)}K/s";
            string info = $"User:{netState.ClientUserCount} Tun:{netState.TunnelCount}      rec: {ConvertBytesToKilobytes(netState.srcReciveSecSpeed)}K/s|{ConvertBytesToKilobytes(netState.tReciveSecSpeed)}K/s        send: {ConvertBytesToKilobytes(netState.srcSendSecSpeed)}K/s|{ConvertBytesToKilobytes(netState.tSendSecSpeed)}K/s";
            Console.Title = Title + info;
            Console.WriteLine(info);
        }
        static string ConvertBytesToKilobytes(long bytes)
        {
            return Math.Round((double)bytes / 1024, 2).ToString("F2");
        }
    }
}