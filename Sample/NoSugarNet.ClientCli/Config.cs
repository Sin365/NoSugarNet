using System.Text;

namespace NoSugarNet.ClientCli
{
    public static class Config
    {
        public static string ServerIP;
        public static int ServerPort;
        public static bool LoadConfig()
        {
            try
            {
                StreamReader sr = new StreamReader(System.Environment.CurrentDirectory + "//config.cfg", Encoding.Default);
                String line;
                while (!string.IsNullOrEmpty((line = sr.ReadLine())))
                {
                    if (!line.Contains(":"))
                        continue;
                    try
                    {
                        ServerIP = line.Split(':')[0].Trim();
                        ServerPort = Convert.ToInt32(line.Split(':')[1].Trim());
                    }
                    catch
                    {
                        continue;
                    }
                }
                sr.Close();
                if (!string.IsNullOrEmpty(ServerIP))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("配置文件异常：" + ex.ToString());
                return false;
            }
        }
    }
}
