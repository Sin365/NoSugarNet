using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace NoSugarNet.ClientCli
{

    public class ConfigDataModel
    {
        public string ServerIP { get; set; }
        public int ServerPort { get; set; }
        public int CompressAdapterType { get; set; }
        public List<ConfigDataModel_Single> TunnelList { get; set; }
    }

    public class ConfigDataModel_Single
    {
        public string LocalTargetIP { get; set; }
        public int LocalTargetPort { get; set; }
        public int RemoteLocalPort { get; set; }
    }

    public static class Config
    {
        public static ConfigDataModel cfg;
        public static bool LoadConfig()
        {
            try
            {
                string path = System.Environment.CurrentDirectory + "//config.cfg";
                if (!File.Exists(path))
                {
                    ConfigDataModel sampleCfg = new ConfigDataModel
                    {
                        ServerIP = "127.0.0.1",
                        ServerPort = 1000,
                        TunnelList = new List<ConfigDataModel_Single>()
                        {
                            new ConfigDataModel_Single(){ LocalTargetIP = "127.0.0.1",LocalTargetPort=3389,RemoteLocalPort = 20001},
                            new ConfigDataModel_Single(){ LocalTargetIP = "127.0.0.1",LocalTargetPort=3389,RemoteLocalPort = 20002}
                        }
                    };

                    string jsonString = JsonSerializer.Serialize(sampleCfg, new JsonSerializerOptions()
                    {
                        // 整齐打印
                        WriteIndented = true,
                        //重新编码，解决中文乱码问题
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                    });
                    System.IO.File.WriteAllText(path, jsonString, Encoding.UTF8);

                    Console.WriteLine("未找到配置，已生成模板，请浏览" + path);
                    return false;
                }
                StreamReader sr = new StreamReader(path, Encoding.Default);
                String jsonstr = sr.ReadToEnd();
                cfg = JsonSerializer.Deserialize<ConfigDataModel>(jsonstr);
                sr.Close();
                if (cfg?.TunnelList.Count > 0)
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
