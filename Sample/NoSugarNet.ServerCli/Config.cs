using NoSugarNet.ServerCore.Common;
using System.Text;

namespace NoSugarNet.ServerCli
{
    public static class Config
    {
        public static Dictionary<byte, TunnelClientData> Cfgs = new Dictionary<byte, TunnelClientData>();
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
                        TunnelClientData cfg = new TunnelClientData()
                        {
                            TunnelId = Convert.ToByte(line.Split(':')[0].Trim()),
                            ServerLocalIP = line.Split(':')[1].Trim(),
                            ServerLocalPort = Convert.ToUInt16(line.Split(':')[2].Trim()),
                            ClientLocalPort = Convert.ToUInt16(line.Split(':')[3].Trim())
                        };
                        Cfgs[cfg.TunnelId] = cfg;
                    }
                    catch
                    {
                        continue;
                    }
                }
                sr.Close();
                if (Cfgs.Count > 0)
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
