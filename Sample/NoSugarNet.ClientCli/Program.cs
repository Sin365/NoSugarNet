using NoSugarNet.ClientCore;
using NoSugarNet.ClientCore.Common;

namespace NoSugarNet.ClientCli
{
    internal class Program
    {
        static string Title = "NoSugarNetClient";
        static void Main(string[] args)
        {
            if (!Config.LoadConfig())
            {
                Console.WriteLine("配置文件错误");
                Console.ReadLine();
                return;
            }
            AppNoSugarNet.OnUpdateStatus += OnUpdateStatus;

            Dictionary<byte, TunnelClientData> dictTunnel = new Dictionary<byte, TunnelClientData>();
            for (int i = 0; i < Config.cfg.TunnelList.Count; i++)
            {
                ConfigDataModel_Single cfgSingle = Config.cfg.TunnelList[i];
                dictTunnel[(byte)i] = new TunnelClientData()
                {
                    TunnelId = (byte)i,
                    LocalTargetIP = cfgSingle.LocalTargetIP,
                    LocalTargetPort = (ushort)cfgSingle.LocalTargetPort,
                    RemoteLocalPort = (ushort)cfgSingle.RemoteLocalPort,
                };
            }

            AppNoSugarNet.Init(dictTunnel, Config.cfg.CompressAdapterType, OnNoSugarNetLog);
            AppNoSugarNet.Connect(Config.cfg.ServerIP, Config.cfg.ServerPort);
            while (true)
            {
                string CommandStr = Console.ReadLine();
                string Command = "";
                Command = ((CommandStr.IndexOf(" ") <= 0) ? CommandStr : CommandStr.Substring(0, CommandStr.IndexOf(" ")));
                string[] CmdArr = CommandStr.Split(' ');
                switch (Command)
                {
                    case "con":
                        AppNoSugarNet.Connect(Config.cfg.ServerIP, Config.cfg.ServerPort);
                        break;
                    case "tlist":
                        AppNoSugarNet.forwardlocal.GetClientCount(out int ClientUserCount, out int TunnelCount);
                        Console.WriteLine($"GetClientCount->{ClientUserCount} TunnelCount->{TunnelCount}");

                        AppNoSugarNet.forwardlocal.GetClientDebugInfo();
                        break;
                    case "stop":
                        AppNoSugarNet.Close();
                        break;
                    default:
                        break;
                }
            }
        }
        static void OnUpdateStatus(NetStatus Forward_NetStatus, NetStatus Reverse_NetStatus)
        {
            string info = $"Forward: t:{Forward_NetStatus.TunnelCount} r:{ConvertBytesToKilobytes(Forward_NetStatus.srcReciveSecSpeed)}K/s|{ConvertBytesToKilobytes(Forward_NetStatus.tReciveSecSpeed)}K/s s: {ConvertBytesToKilobytes(Forward_NetStatus.srcSendSecSpeed)}K/s|{ConvertBytesToKilobytes(Forward_NetStatus.tSendSecSpeed)}K/s" +
                $"| Reverse t:{Reverse_NetStatus.TunnelCount} r:{ConvertBytesToKilobytes(Reverse_NetStatus.srcReciveSecSpeed)}K/s|{ConvertBytesToKilobytes(Reverse_NetStatus.tReciveSecSpeed)}K/s s: {ConvertBytesToKilobytes(Reverse_NetStatus.srcSendSecSpeed)}K/s|{ConvertBytesToKilobytes(Reverse_NetStatus.tSendSecSpeed)}K/s";
            Console.Title = Title + info;
            Console.WriteLine(info);
        }
        static string ConvertBytesToKilobytes(long bytes)
        {
            return Math.Round((double)bytes / 1024, 2).ToString("F2");
        }
        static void OnNoSugarNetLog(int LogLevel, string msg)
        {
            Console.WriteLine(msg);
        }

    }
}