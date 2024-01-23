using ServerCore.Manager;

namespace NoSugarNet.ServerCli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (!Config.LoadConfig())
            {
                Console.WriteLine("配置文件错误");
                Console.ReadLine();
                return;
            }

            ServerManager.InitServer(1000,Config.Cfgs);
            while (true) 
            {
                Console.ReadLine();
            }
        }
    }
}