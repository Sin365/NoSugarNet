using NoSugarNet.ClientCore;

namespace NoSugarNet.ClientCli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!Config.LoadConfig())
            {
                Console.WriteLine("配置文件错误");
                Console.ReadLine();
                return;
            }
            AppNoSugarNet.Init(Config.ServerIP, Config.ServerPort);
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}