using NoSugarNet.DataHelper;
using System.Text;

namespace NoSugarNet.ServerCore.Common
{
    public struct TunnelClientData
    {
        public byte TunnelId;
        public string ServerLocalTargetIP;
        public ushort ServerLocalTargetPort;
        public ushort ClientLocalPort;
    }

    public static class Config
    {
        public static Dictionary<byte, TunnelClientData> cfgs = new Dictionary<byte, TunnelClientData>();
        public static E_CompressAdapter compressAdapterType;
    }
}
