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
            AppNoSugarNet.Init(Config.ServerIP, Config.ServerPort);
            while (true)
            {
                Console.ReadLine();
            }
        }
        static void OnUpdateStatus(long resultReciveAllLenght, long resultSendAllLenght)
        {
            Console.Title = $"{Title} Recive:{resultReciveAllLenght} Send:{resultSendAllLenght}";
            Console.WriteLine($"{Title} Recive:{resultReciveAllLenght} Send:{resultSendAllLenght}");
        }
    }

}