using ServerCore.Manager;

namespace NoSugarNet.ServerCli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerManager.InitServer(1000);
            while (true) 
            {
                Console.ReadLine();
            }
        }
    }
}