using NoSugarNet.ClientCore;

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
            AppNoSugarNet.Init(OnNoSugarNetLog);
            AppNoSugarNet.Connect(Config.ServerIP, Config.ServerPort);
            while (true)
            {
                string CommandStr = Console.ReadLine();
                string Command = "";
                Command = ((CommandStr.IndexOf(" ") <= 0) ? CommandStr : CommandStr.Substring(0, CommandStr.IndexOf(" ")));
                string[] CmdArr = CommandStr.Split(' ');
                switch (Command)
                {
                    case "con":
                        AppNoSugarNet.Connect(Config.ServerIP, Config.ServerPort);
                        break;
                    case "stop":
                        AppNoSugarNet.Close();
                        break;
                    default:
                        break;
                }
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
        static void OnNoSugarNetLog(int LogLevel, string msg)
        {
            Console.WriteLine(msg);
        }

    }
}