using System.Text;

namespace NoSugarNet.ServerCore.Common
{
    public struct TunnelClientData
    {
        public byte TunnelId;
        public string ServerLocalIP;
        public ushort ServerLocalPort;
        public ushort ClientLocalPort;
    }

    public static class Config
    {
        public static Dictionary<byte, TunnelClientData> Cfgs = new Dictionary<byte, TunnelClientData>();
    }
}
