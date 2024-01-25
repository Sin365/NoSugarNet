using AxibugProtobuf;
using Google.Protobuf;
using NoSugarNet.ClientCore.Network;
using NoSugarNet.DataHelper;
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

        public long tReciveAllLenght { get; private set; }
        public long tSendAllLenght { get;private set; }

        static long GetCommKey(long Uid, int Tunnel, int Idx)
        {
            return (Uid * 10000000) + (Tunnel * 10000) + Idx;
        }

        static long GetUidForCommKey(long CommKey)
        {
            return CommKey / 10000000;
        }

        public LocalClientManager(E_CompressAdapter compressAdapterType)
        {
            ServerManager.g_Log.Debug("初始化压缩适配器" + compressAdapterType);
            //初始化压缩适配器，暂时使用0，代表压缩类型
            mCompressAdapter = new CompressAdapter(compressAdapterType);
            //注册网络消息
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SConnect, Recive_TunnelC2SConnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SDisconnect, Recive_TunnelC2SDisconnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SData, Recive_TunnelC2SData);
        }

        public void GetCurrLenght(out long resultReciveAllLenght,out long resultSendAllLenght)
        {
            resultReciveAllLenght = 0;
            resultSendAllLenght = 0;
            long[] Keys = mDictCommKey2ServerLocalClients.Keys.ToArray();
            for(int i =0; i < Keys.Length; i++) 
            {
                resultReciveAllLenght += mDictCommKey2ServerLocalClients[Keys[i]].mReciveAllLenght;
                resultSendAllLenght += mDictCommKey2ServerLocalClients[Keys[i]].mSendAllLenght;
            }
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

        void CloseServerLocalClient(long uid, byte tunnelId, byte Idx)
        {
            //隧道ID定位投递服务端本地连接
            if (!GetServerLocalClient(uid, tunnelId, Idx, out ServerLocalClient _serverLocalClient))
                return;
            _serverLocalClient.CloseConntect();
            RemoveServerLocalClient(uid, tunnelId, Idx);
        }

        public void GetClientCount(out int ClientUserCount,out int TunnelCount)
        {
            TunnelCount = mDictCommKey2ServerLocalClients.Count;
            long[] CommIDKeys = mDictCommKey2ServerLocalClients.Keys.ToArray();
            List<long> TempHadLocalConnetList = new List<long>();
            for (int i = 0; i < CommIDKeys.Length; i++)
            {
                long uid = GetUidForCommKey(CommIDKeys[i]);
                if(!TempHadLocalConnetList.Contains(uid))
                    TempHadLocalConnetList.Add(uid);
            }
            ClientUserCount = TempHadLocalConnetList.Count;
        }

        public void StopAll(long Uid)
        {
            List<long> TempRemoveCommIDList = new List<long>();
            lock (mDictCommKey2ServerLocalClients)
            {
                long[] CommIDKeys = mDictCommKey2ServerLocalClients.Keys.ToArray();
                for (int i = 0; i < CommIDKeys.Length; i++)
                {
                    long CommID = CommIDKeys[i];
                    long tempUid = GetUidForCommKey(CommID);
                    if (tempUid == Uid)
                        TempRemoveCommIDList.Add(CommID);
                }
            }

            for (int i = 0; i < TempRemoveCommIDList.Count; i++)
            {
                long CommID = TempRemoveCommIDList[i];
                if (!mDictCommKey2ServerLocalClients.ContainsKey(CommID))
                    continue;
                ServerLocalClient _serverLoackClient = mDictCommKey2ServerLocalClients[CommID];
                _serverLoackClient.CloseConntect();
            }
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
            //ServerManager.g_Log.Debug("OnTunnelC2SData");
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
            ServerManager.g_Log.Debug($"OnClientLocalConnect!!!!!! {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            if (!Config.cfgs.ContainsKey(tunnelId))
                return;

            //开一个线程去建立连接
            Thread thread = new Thread(() =>
            {
                //服务器本地局域网连接指定端口
                TunnelClientData tunnelDataCfg = Config.cfgs[tunnelId];
                ServerLocalClient serverLocalClient = new ServerLocalClient(uid, tunnelId, (byte)Idx);
                //连接成功
                if (!serverLocalClient.Init(tunnelDataCfg.ServerLocalTargetIP, tunnelDataCfg.ServerLocalTargetPort))
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
            ServerManager.g_Log.Debug($"OnClientLocalDisconnect,收到客户端断开链接!!!!!! {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            //隧道ID定位投递服务端本地连接
            if (!GetServerLocalClient(uid, tunnelId, Idx, out ServerLocalClient serverLocalClient))
                return;

            //断开服务端本地客户端连接
            CloseServerLocalClient(uid, tunnelId, Idx);
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
            //移除到服务端本地连接列表
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
        /// 来自客户端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnClientTunnelDataCallBack(long uid, byte tunnelId, byte Idx, byte[] data)
        {
            //ServerManager.g_Log.Debug($"OnClientTunnelDataCallBack {uid},{tunnelId},{Idx}");
            //隧道ID定位投递服务端本地连接
            if (!GetServerLocalClient(uid, tunnelId, Idx, out ServerLocalClient serverLocalClient))
                return;
            //记录数据长度
            tSendAllLenght += data.Length;
            //解压
            data = mCompressAdapter.Decompress(data);
            //记录数据长度
            serverLocalClient.mSendAllLenght += data.LongLength;
            //发送给对应服务端本地连接数据
            serverLocalClient.SendToServer(data);
        }
        /// <summary>
        /// 来自服务端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnServerLocalDataCallBack(long uid, byte tunnelId,byte Idx, byte[] data)
        {
            //ServerManager.g_Log.Debug($"OnServerLocalDataCallBack {uid},{tunnelId},{Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;

            int SlienLenght = 1000;
            //判断数据量大时分包
            if (data.Length > SlienLenght)
            {
                Span<byte> tempSpan = data;
                Span<byte> tempSpanSlien = null;
                int PageCount = (int)(data.Length / SlienLenght);
                if (data.Length % SlienLenght > 0)
                {
                    PageCount++;
                }

                for (int i = 0; i < PageCount; i++)
                {
                    int StartIdx = i * SlienLenght;
                    if (i != PageCount - 1)//不是最后一个包
                        tempSpanSlien = tempSpan.Slice(StartIdx, SlienLenght);
                    else//最后一个
                        tempSpanSlien = tempSpan.Slice(StartIdx);

                    SendDataToRemote(uid, tunnelId, Idx, tempSpanSlien.ToArray());
                }
                return;
            }
            SendDataToRemote(uid, tunnelId, Idx, data);
        }

        void SendDataToRemote(long uid, byte tunnelId, byte Idx, byte[] data)
        {
            if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
                return;


            //压缩
            data = mCompressAdapter.Compress(data);
            //记录压缩后数据长度
            tReciveAllLenght += data.Length;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_DATA()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                HunterNetCoreData = ByteString.CopyFrom(data)
            });

            //发送给客户端，指定客户端本地隧道ID
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CData, (int)ErrorCode.ErrorOk, respData);
        }
        #endregion
    }
}