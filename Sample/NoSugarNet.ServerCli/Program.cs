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

        static void OnUpdateStatus(NetStatus Forward_NetStatus, NetStatus Reverse_NetStatus)
        {
            string info = $"Forward: user:{Forward_NetStatus.ClientUserCount} t:{Forward_NetStatus.TunnelCount} r:{ConvertBytesToKilobytes(Forward_NetStatus.srcReciveSecSpeed)}K/s|{ConvertBytesToKilobytes(Forward_NetStatus.tReciveSecSpeed)}K/s s: {ConvertBytesToKilobytes(Forward_NetStatus.srcSendSecSpeed)}K/s|{ConvertBytesToKilobytes(Forward_NetStatus.tSendSecSpeed)}K/s" +
                $"| Reverse user:{Reverse_NetStatus.ClientUserCount} t:{Reverse_NetStatus.TunnelCount} r:{ConvertBytesToKilobytes(Reverse_NetStatus.srcReciveSecSpeed)}K/s|{ConvertBytesToKilobytes(Reverse_NetStatus.tReciveSecSpeed)}K/s s: {ConvertBytesToKilobytes(Reverse_NetStatus.srcSendSecSpeed)}K/s|{ConvertBytesToKilobytes(Reverse_NetStatus.tSendSecSpeed)}K/s";
            Console.Title = Title + info;
            Console.WriteLine(info);
        }
        static string ConvertBytesToKilobytes(long bytes)
        {
            return Math.Round((double)bytes / 1024, 2).ToString("F2");
        }
    }
}