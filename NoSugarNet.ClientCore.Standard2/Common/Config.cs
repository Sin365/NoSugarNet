using NoSugarNet.Adapter.DataHelper;
using System.Collections.Generic;

namespace NoSugarNet.ClientCore.Common
{
    public struct TunnelClientData
    {
        public byte TunnelId;
        public string LocalTargetIP;
        public ushort LocalTargetPort;
        public ushort RemoteLocalPort;
    }
    public static class Config
    {
        public static Dictionary<byte, TunnelClientData> cfgs = new Dictionary<byte, TunnelClientData>();
        public static E_CompressAdapter compressAdapterType;
    }
}
