﻿using NoSugarNet.ServerCore;
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
            ServerManager.InitServer(Config.cfg.ServerPort, dictTunnel);

            while (true) 
            {
                Console.ReadLine();
            }
        }

        static void OnUpdateStatus(NetStatus netState)
        {
            string info = $"{Title} RecLen:{netState.ReciveAllLenght} SendLen:{netState.SendAllLenght} tUserNum:{netState.ClientUserCount} tTunnelNum:{netState.TunnelCount} recSpeed:{ConvertBytesToKilobytes(netState.ReciveSecSpeed)}K/s sendSpeed:{ConvertBytesToKilobytes(netState.SendSecSpeed)}K/s";
            Console.Title = info;
            Console.WriteLine(info);
        }

        private static double ConvertBytesToKilobytes(long bytes)
        {
            return Math.Round((double)bytes / 1024, 2);
        }
    }
}