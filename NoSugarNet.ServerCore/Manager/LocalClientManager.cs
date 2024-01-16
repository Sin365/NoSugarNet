using AxibugProtobuf;
using ClientCore.Network;
using Google.Protobuf;
using NoSugarNet.ServerCore.Common;
using ServerCore.Common;
using ServerCore.NetWork;
using System.Net.Sockets;

namespace ServerCore.Manager
{
    public class LocalClientManager
    {
        struct TunnelClientData
        {
            public string IP;
            public ushort Port;
        }

        Dictionary<byte, TunnelClientData> mDictTunnelID2Cfg = new Dictionary<byte, TunnelClientData>();
        Dictionary<long, Dictionary<byte, ServerLocalClient>> mDictUid2ServerLocalClients = new Dictionary<long, Dictionary<byte, ServerLocalClient>>();
        CompressAdapter mCompressAdapter;

        public LocalClientManager() 
        {
            //初始化压缩适配器，暂时使用0，代表压缩类型
            mCompressAdapter = new CompressAdapter(0);
            //注册网络消息
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SConnect, Recive_TunnelC2SConnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SDisconnect, Recive_TunnelC2SDisconnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SData, Recive_TunnelC2SData);
        }

        #region 连接字典管理
        /// <summary>
        /// 追加连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="serverClient"></param>
        void AddServerLocalClient(long uid, byte tunnelId, ServerLocalClient serverClient)
        {
            lock (mDictUid2ServerLocalClients)
            {
                if (!mDictUid2ServerLocalClients.ContainsKey(uid))
                    mDictUid2ServerLocalClients[uid] = new Dictionary<byte, ServerLocalClient>();

                mDictUid2ServerLocalClients[uid][tunnelId] = serverClient;
            }
        }
        /// <summary>
        /// 删除连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        void RemoveServerLocalClient(long uid, byte tunnelId)
        {
            lock (mDictUid2ServerLocalClients)
            {
                if (!mDictUid2ServerLocalClients.ContainsKey(uid))
                    return;

                if (!mDictUid2ServerLocalClients[uid].ContainsKey(tunnelId))
                    return;

                mDictUid2ServerLocalClients[uid].Remove(tunnelId);

                if (mDictUid2ServerLocalClients[uid].Count < 1)
                    mDictUid2ServerLocalClients.Remove(uid);
            }
        }
        bool GetServerLocalClient(long uid, byte tunnelId,out ServerLocalClient serverLocalClient)
        {
            serverLocalClient = null;
            if (!mDictUid2ServerLocalClients.ContainsKey(uid))
                return false;

            if (!mDictUid2ServerLocalClients[uid].ContainsKey(tunnelId))
                return false;

            serverLocalClient = mDictUid2ServerLocalClients[uid][tunnelId];
            return true;
        }
        #endregion


        #region 解析客户端上行数据
        public void Recive_TunnelC2SConnect(Socket sk, byte[] reqData)
        {
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            ServerManager.g_Log.Debug("OnTunnelC2SConnect");
            Protobuf_C2S_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_C2S_Connect>(reqData);
            OnClientLocalConnect(_c.UID, (byte)msg.TunnelID);
        }
        public void Recive_TunnelC2SDisconnect(Socket sk, byte[] reqData)
        {
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            ServerManager.g_Log.Debug("Recive_TunnelC2SDisconnect");
            Protobuf_C2S_Disconnect msg = ProtoBufHelper.DeSerizlize<Protobuf_C2S_Disconnect>(reqData);
            OnClientLocalDisconnect(_c.UID, (byte)msg.TunnelID);
        }
        public void Recive_TunnelC2SData(Socket sk, byte[] reqData)
        {
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            ServerManager.g_Log.Debug("OnTunnelC2SData");
            Protobuf_C2S_DATA msg = ProtoBufHelper.DeSerizlize<Protobuf_C2S_DATA>(reqData);
            OnClientTunnelDataCallBack(_c.UID, (byte)msg.TunnelID, msg.HunterNetCoreData.ToArray());
        }
        #endregion


        #region 两端本地端口连接事件通知
        /// <summary>
        /// 当客户端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        void OnClientLocalConnect(long uid, byte tunnelId)
        {
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
                return;

            //开一个线程去建立连接
            Thread thread = new Thread(() =>
            {
                //服务器本地局域网连接指定端口
                TunnelClientData tunnelDataCfg = mDictTunnelID2Cfg[tunnelId];
                ServerLocalClient serverLocalClient = new ServerLocalClient(tunnelId);
                //连接成功
                if (!serverLocalClient.Init(tunnelDataCfg.IP, tunnelDataCfg.Port))
                {
                    //连接失败
                    //TODO告知客户端连接失败
                }
            });
            thread.Start();
        }
        /// <summary>
        /// 当客户端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        void OnClientLocalDisconnect(long uid, byte tunnelId)
        {
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            //隧道ID定位投递服务端本地连接
            if (!GetServerLocalClient(uid, tunnelId, out ServerLocalClient serverLocalClient))
                return;

            //清楚服务器数据
            RemoveServerLocalClient(uid, tunnelId);
            //断开服务端本地客户端连接
            serverLocalClient.CloseConntect();
        }
        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnServerLocalConnect(long uid, byte tunnelId, ServerLocalClient serverLocalClient)
        {
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            //添加到服务端本地连接列表
            AddServerLocalClient(uid, tunnelId, serverLocalClient);

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_Connect()
            {
                TunnelID = tunnelId,
            });
            //发送给客户端，指定服务端本地端口已连接
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CConnect, (int)ErrorCode.ErrorOk, respData);
        }
        /// <summary>
        /// 当服务端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnServerLocalDisconnect(long uid, byte tunnelId, ServerLocalClient serverLocalClient)
        {
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;
            //添加到服务端本地连接列表
            RemoveServerLocalClient(uid, tunnelId);

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_Disconnect()
            {
                TunnelID = tunnelId,
            });
            //发送给客户端，指定服务端本地端口连接已断开
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CDisconnect, (int)ErrorCode.ErrorOk, respData);
        }
        #endregion

        #region 数据投递
        /// <summary>
        /// 来自服务端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnServerLocalDataCallBack(long uid, byte tunnelId, byte[] data)
        {
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            //压缩
            data = mCompressAdapter.Compress(data);
            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_DATA()
            {
                TunnelID = tunnelId,
                HunterNetCoreData = ByteString.CopyFrom(data)
            });

            //发送给客户端，指定客户端本地隧道ID
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CData, (int)ErrorCode.ErrorOk, respData);
        }
        /// <summary>
        /// 来自客户端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnClientTunnelDataCallBack(long uid, byte tunnelId, byte[] data)
        {
            //隧道ID定位投递服务端本地连接
            if (!GetServerLocalClient(uid, tunnelId, out ServerLocalClient serverLocalClient))
                return;

            //解压
            data = mCompressAdapter.Decompress(data);
            //发送给对应服务端本地连接数据
            serverLocalClient.SendToServer(mCompressAdapter.Decompress(data));
        }
        #endregion
    }
}