using AxibugProtobuf;
using NoSugarNet.ClientCore.Network;
using Google.Protobuf;
using NoSugarNet.ServerCore.Common;
using ServerCore.Common;
using ServerCore.NetWork;
using System.Net.Sockets;

namespace ServerCore.Manager
{
    public class LocalClientManager
    {
        Dictionary<long, ServerLocalClient> mDictCommKey2ServerLocalClients = new Dictionary<long, ServerLocalClient>();
        CompressAdapter mCompressAdapter;

        static long GetCommKey(long Uid, int Tunnel, int Idx)
        {
            return (Uid * 10000000) + (Tunnel * 10000) + Idx;
        }

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
        void AddServerLocalClient(long uid, byte tunnelId,byte Idx, ServerLocalClient serverClient)
        {
            long CommKey = GetCommKey(uid, tunnelId, Idx);
            lock (mDictCommKey2ServerLocalClients)
            {
                mDictCommKey2ServerLocalClients[CommKey] = serverClient;
            }
        }

        /// <summary>
        /// 删除连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        void RemoveServerLocalClient(long uid, byte tunnelId, byte Idx)
        {
            lock (mDictCommKey2ServerLocalClients)
            {
                long CommKey = GetCommKey(uid, tunnelId, Idx);

                if (!mDictCommKey2ServerLocalClients.ContainsKey(CommKey))
                    return;
                mDictCommKey2ServerLocalClients.Remove(CommKey);
            }
        }

        bool GetServerLocalClient(long uid, byte tunnelId, byte Idx,out ServerLocalClient serverLocalClient)
        {
            serverLocalClient = null;

            long CommKey = GetCommKey(uid, tunnelId, Idx);

            if (!mDictCommKey2ServerLocalClients.ContainsKey(CommKey))
                return false;

            serverLocalClient = mDictCommKey2ServerLocalClients[CommKey];
            return true;
        }
        #endregion

        #region 解析客户端上行数据
        public void Recive_TunnelC2SConnect(Socket sk, byte[] reqData)
        {
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            ServerManager.g_Log.Debug("OnTunnelC2SConnect");
            Protobuf_C2S_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_C2S_Connect>(reqData);
            OnClientLocalConnect(_c.UID, (byte)msg.TunnelID, (byte)msg.Idx);
        }

        public void Recive_TunnelC2SDisconnect(Socket sk, byte[] reqData)
        {
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            ServerManager.g_Log.Debug("Recive_TunnelC2SDisconnect");
            Protobuf_C2S_Disconnect msg = ProtoBufHelper.DeSerizlize<Protobuf_C2S_Disconnect>(reqData);
            OnClientLocalDisconnect(_c.UID, (byte)msg.TunnelID,(byte)msg.Idx);
        }
        public void Recive_TunnelC2SData(Socket sk, byte[] reqData)
        {
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            ServerManager.g_Log.Debug("OnTunnelC2SData");
            Protobuf_C2S_DATA msg = ProtoBufHelper.DeSerizlize<Protobuf_C2S_DATA>(reqData);
            OnClientTunnelDataCallBack(_c.UID, (byte)msg.TunnelID, (byte)msg.Idx, msg.HunterNetCoreData.ToArray());
        }
        #endregion

        #region 两端本地端口连接事件通知
        /// <summary>
        /// 当客户端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        void OnClientLocalConnect(long uid, byte tunnelId,int Idx)
        {
            ServerManager.g_Log.Debug($"OnClientLocalConnect {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            if (!Config.Cfgs.ContainsKey(tunnelId))
                return;

            //开一个线程去建立连接
            Thread thread = new Thread(() =>
            {
                //服务器本地局域网连接指定端口
                TunnelClientData tunnelDataCfg = Config.Cfgs[tunnelId];
                ServerLocalClient serverLocalClient = new ServerLocalClient(uid, tunnelId, (byte)Idx);
                //连接成功
                if (!serverLocalClient.Init(tunnelDataCfg.ServerLocalIP, tunnelDataCfg.ServerLocalPort))
                {
                    //TODO告知客户端连接失败

                    byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_Connect()
                    {
                        TunnelID = tunnelId,
                        Idx = (uint)Idx,
                        Connected = 0//失败
                    });
                    //发送给客户端，指定服务端本地端口已连接
                    ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CConnect, (int)ErrorCode.ErrorOk, respData);
                }
            });
            thread.Start();
        }
        /// <summary>
        /// 当客户端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        void OnClientLocalDisconnect(long uid, byte tunnelId,byte Idx)
        {
            ServerManager.g_Log.Debug($"OnClientLocalDisconnect {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            //隧道ID定位投递服务端本地连接
            if (!GetServerLocalClient(uid, tunnelId, Idx, out ServerLocalClient serverLocalClient))
                return;

            //清楚服务器数据
            RemoveServerLocalClient(uid, tunnelId, Idx);
            //断开服务端本地客户端连接
            serverLocalClient.CloseConntect();
        }
        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnServerLocalConnect(long uid, byte tunnelId, byte Idx, ServerLocalClient serverLocalClient)
        {
            ServerManager.g_Log.Debug($"OnServerLocalConnect {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            //添加到服务端本地连接列表
            AddServerLocalClient(uid, tunnelId, Idx, serverLocalClient);

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_Connect()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                Connected = 1
            });
            //发送给客户端，指定服务端本地端口已连接
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CConnect, (int)ErrorCode.ErrorOk, respData);
        }
        /// <summary>
        /// 当服务端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnServerLocalDisconnect(long uid, byte tunnelId, byte Idx, ServerLocalClient serverLocalClient)
        {
            ServerManager.g_Log.Debug($"OnServerLocalDisconnect {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;
            //添加到服务端本地连接列表
            RemoveServerLocalClient(uid, tunnelId, Idx);

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_Disconnect()
            {
                TunnelID = tunnelId,
                Idx= Idx,
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
        public void OnServerLocalDataCallBack(long uid, byte tunnelId,byte Idx, byte[] data)
        {
            ServerManager.g_Log.Debug($"OnServerLocalDataCallBack {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            //压缩
            data = mCompressAdapter.Compress(data);
            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_DATA()
            {
                TunnelID = tunnelId,
                Idx = Idx,
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
        public void OnClientTunnelDataCallBack(long uid, byte tunnelId, byte Idx, byte[] data)
        {
            ServerManager.g_Log.Debug($"OnClientTunnelDataCallBack {uid},{tunnelId},{Idx}");
            //隧道ID定位投递服务端本地连接
            if (!GetServerLocalClient(uid, tunnelId, Idx, out ServerLocalClient serverLocalClient))
                return;

            //解压
            data = mCompressAdapter.Decompress(data);
            //发送给对应服务端本地连接数据
            serverLocalClient.SendToServer(mCompressAdapter.Decompress(data));
        }
        #endregion
    }
}