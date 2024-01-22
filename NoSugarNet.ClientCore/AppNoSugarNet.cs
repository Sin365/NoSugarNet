using NoSugarNet.ClientCore.Manager;
using NoSugarNet.ClientCore.Network;
using ServerCore.Manager;

namespace NoSugarNet.ClientCore
{
    public class AppNoSugarNet
    {
        public static string TokenStr;
        public static long RID = -1;
        public static string IP;
        public static int Port;
        public static LogManager log;
        public static NetworkHelper networkHelper;
        public static AppLogin login;
        public static AppChat chat;
        public static AppLocalClient local;
        public static UserDataManager user;

        public static void Init(string IP, int port)
        {
            log = new LogManager();
            networkHelper = new NetworkHelper();
            login = new AppLogin();
            chat = new AppChat();
            local = new AppLocalClient();
            user = new UserDataManager();
            networkHelper.Init(IP, port);
        }
    }
}