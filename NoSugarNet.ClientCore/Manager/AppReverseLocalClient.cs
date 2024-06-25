using AxibugProtobuf;
using Google.Protobuf;
using NoSugarNet.Adapter;
using NoSugarNet.Adapter.DataHelper;
using NoSugarNet.ClientCore;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore.Network;

namespace ServerCore.Manager
{
    public class AppReverseLocalClient
    {
        Dictionary<long, BackwardLocalClient> mDictCommKey2LocalClients = new Dictionary<long, BackwardLocalClient>();
        NoSugarNet.Adapter.DataHelper.CompressAdapter mCompressAdapter;
        //public LocalMsgQueuePool _localMsgPool = new LocalMsgQueuePool(1000);

        public long tReciveAllLenght { get; private set; }
        public long tSendAllLenght { get; private set; }

        static long GetCommKey(long Uid, int Tunnel, int Idx)
        {
            return (Uid * 10000000) + (Tunnel * 10000) + Idx;
        }

        static long GetUidForCommKey(long CommKey)
        {
            return CommKey / 10000000;
        }

        public AppReverseLocalClient(NoSugarNet.Adapter.DataHelper.E_CompressAdapter compressAdapterType)
        {
            AppNoSugarNet.log.Debug("初始化压缩适配器" + compressAdapterType);
            //初始化压缩适配器，暂时使用0，代表压缩类型
            mCompressAdapter = new CompressAdapter(compressAdapterType);

            //注册网络消息
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CReverseConnect, Recive_TunnelS2CConnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CReverseDisconnect, Recive_TunnelS2CDisconnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CReverseData, Recive_TunnelS2CData);
        }

        Protobuf_Cfgs _Protobuf_Cfgs = new Protobuf_Cfgs();

        public void Send_ClientCfg()
        {
            AppNoSugarNet.log.Debug("-->Send_ClientCfg");

            _Protobuf_Cfgs.CompressAdapterType = (int)Config.compressAdapterType;
            _Protobuf_Cfgs.Cfgs.Clear();
            foreach (var cfg in Config.cfgs)
            {
                _Protobuf_Cfgs.Cfgs.Add(new Protobuf_Cfgs_Single() { Port = cfg.Value.ClientLocalPort, TunnelID = cfg.Value.TunnelId });
            }
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdClientCfgs, ProtoBufHelper.Serizlize(_Protobuf_Cfgs));
        }


        #region 解析服务端下行数据
        public void Recive_TunnelS2CConnect(byte[] reqData)
        {
            AppNoSugarNet.log.Debug("Recive_TunnelS2CConnect");
            Protobuf_S2C_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_Connect>(reqData);
            if (msg.Connected == 1)
                OnServerLocalConnect((byte)msg.TunnelID, (byte)msg.Idx);
            else
                OnServerLocalDisconnect((byte)msg.TunnelID, (byte)msg.Idx);
        }
        public void Recive_TunnelS2CDisconnect(byte[] reqData)
        {
            AppNoSugarNet.log.Debug("Recive_TunnelS2CDisconnect");
            Protobuf_S2C_Disconnect msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_Disconnect>(reqData);
            OnServerLocalDisconnect((byte)msg.TunnelID, (byte)msg.Idx);
        }
        public void Recive_TunnelS2CData(byte[] reqData)
        {
            //AppNoSugarNet.log.Debug("Recive_TunnelS2CData");
            Protobuf_S2C_DATA msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_DATA>(reqData);
            OnServerTunnelDataCallBack(AppNoSugarNet.user.userdata.UID, (byte)msg.TunnelID, (byte)msg.Idx, msg.HunterNetCoreData.ToArray());
        }
        #endregion


        #region 两端本地端口连接事件通知

        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="tunnelId"></param>
        public void OnServerLocalConnect(byte tunnelId, byte Idx)
        {
            AppNoSugarNet.log.Debug($"OnClientLocalConnect!!!!!! {AppNoSugarNet.user.userdata.UID},{tunnelId},{Idx}");

            if (!Config.cfgs.ContainsKey(tunnelId))
                return;

            //开一个线程去建立连接
            Thread thread = new Thread(() =>
            {
                //服务器本地局域网连接指定端口
                TunnelClientData tunnelDataCfg = Config.cfgs[tunnelId];
                BackwardLocalClient serverLocalClient = new BackwardLocalClient(AppNoSugarNet.user.userdata.UID, tunnelId, (byte)Idx);
                serverLocalClient.BandEvent(AppNoSugarNet.log.Log, OnClientLocalConnect, OnClientLocalDisconnect, OnClientLocalDataCallBack);
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
                    AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelS2CForwardConnect, respData);
                }
            });
            thread.Start();
        }
        /// <summary>
        /// 当服务端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnServerLocalDisconnect(byte tunnelId, byte Idx)
        {
            AppNoSugarNet.log.Debug($"OnServerLocalDisconnect,收到客户端断开链接!!!!!! {AppNoSugarNet.user.userdata.UID},{tunnelId},{Idx}");
            
            //隧道ID定位投递服务端本地连接
            if (!GetClientLocalClient(AppNoSugarNet.user.userdata.UID, tunnelId, Idx, out BackwardLocalClient LocalClient))
                return;

            //断开服务端本地客户端连接
            CloseClientLocalClient(AppNoSugarNet.user.userdata.UID, tunnelId, Idx);
        }


        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnClientLocalConnect(long uid, byte tunnelId, byte Idx, BackwardLocalClient serverLocalClient)
        {
            AppNoSugarNet.log.Debug($"OnServerLocalConnect {uid},{tunnelId},{Idx}");

            //添加到服务端本地连接列表
            AddClientLocalClient(uid, tunnelId, Idx, serverLocalClient);

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_Connect()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                Connected = 1
            });
            //发送给客户端，指定服务端本地端口已连接
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelS2CForwardConnect,  respData);
        }
        /// <summary>
        /// 当服务端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnClientLocalDisconnect(long uid, byte tunnelId, byte Idx, BackwardLocalClient serverLocalClient)
        {
            AppNoSugarNet.log.Debug($"OnServerLocalDisconnect {uid},{tunnelId},{Idx}");
            //移除到服务端本地连接列表
            RemoveClientLocalClient(uid, tunnelId, Idx);

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_Disconnect()
            {
                TunnelID = tunnelId,
                Idx = Idx,
            });
            //发送给客户端，指定服务端本地端口连接已断开
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelS2CForwardDisconnect,  respData);
        }
        #endregion

        #region 连接字典管理
        /// <summary>
        /// 追加连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="serverClient"></param>
        void AddClientLocalClient(long uid, byte tunnelId, byte Idx, BackwardLocalClient serverClient)
        {
            long CommKey = GetCommKey(uid, tunnelId, Idx);
            lock (mDictCommKey2LocalClients)
            {
                mDictCommKey2LocalClients[CommKey] = serverClient;
            }
        }
        /// <summary>
        /// 删除连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        void RemoveClientLocalClient(long uid, byte tunnelId, byte Idx)
        {
            lock (mDictCommKey2LocalClients)
            {
                long CommKey = GetCommKey(uid, tunnelId, Idx);

                if (!mDictCommKey2LocalClients.ContainsKey(CommKey))
                    return;
                mDictCommKey2LocalClients[CommKey].Release();
                mDictCommKey2LocalClients.Remove(CommKey);
            }
        }

        bool GetClientLocalClient(long uid, byte tunnelId, byte Idx, out BackwardLocalClient serverLocalClient)
        {
            serverLocalClient = null;

            long CommKey = GetCommKey(uid, tunnelId, Idx);

            if (!mDictCommKey2LocalClients.ContainsKey(CommKey))
                return false;

            serverLocalClient = mDictCommKey2LocalClients[CommKey];
            return true;
        }

        void CloseClientLocalClient(long uid, byte tunnelId, byte Idx)
        {
            //隧道ID定位投递服务端本地连接
            if (!GetClientLocalClient(uid, tunnelId, Idx, out BackwardLocalClient _LocalClient))
                return;
            _LocalClient.CloseConntect();
            RemoveClientLocalClient(uid, tunnelId, Idx);
        }

        public void GetClientCount(out int ClientUserCount, out int TunnelCount)
        {
            TunnelCount = mDictCommKey2LocalClients.Count;
            long[] CommIDKeys = mDictCommKey2LocalClients.Keys.ToArray();
            List<long> TempHadLocalConnetList = new List<long>();
            for (int i = 0; i < CommIDKeys.Length; i++)
            {
                long uid = GetUidForCommKey(CommIDKeys[i]);
                if (!TempHadLocalConnetList.Contains(uid))
                    TempHadLocalConnetList.Add(uid);
            }
            ClientUserCount = TempHadLocalConnetList.Count;
        }

        public void StopAll(long Uid)
        {
            List<long> TempRemoveCommIDList = new List<long>();
            lock (mDictCommKey2LocalClients)
            {
                long[] CommIDKeys = mDictCommKey2LocalClients.Keys.ToArray();
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
                if (!mDictCommKey2LocalClients.ContainsKey(CommID))
                    continue;
                BackwardLocalClient _serverLoackClient = mDictCommKey2LocalClients[CommID];
                _serverLoackClient.CloseConntect();
            }
        }
        #endregion


        #region 数据投递
        /// <summary>
        /// 来自客户端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnServerTunnelDataCallBack(long uid, byte tunnelId, byte Idx, byte[] data)
        {
            //ServerManager.g_Log.Debug($"OnClientTunnelDataCallBack {uid},{tunnelId},{Idx}");
            //隧道ID定位投递服务端本地连接
            if (!GetClientLocalClient(uid, tunnelId, Idx, out BackwardLocalClient serverLocalClient))
                return;
            //记录数据长度
            tReciveAllLenght += data.Length;
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
        public void OnClientLocalDataCallBack(long uid, byte tunnelId, byte Idx, byte[] data)
        {
            //ServerManager.g_Log.Debug($"OnServerLocalDataCallBack {uid},{tunnelId},{Idx}");
            //if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
            //    return;

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
            //if (!ServerManager.g_ClientMgr.GetClientByUID(uid, out ClientInfo client))
            //    return;

            //压缩
            data = mCompressAdapter.Compress(data);
            //记录压缩后数据长度
            tSendAllLenght += data.Length;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_S2C_DATA()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                HunterNetCoreData = ByteString.CopyFrom(data)
            });

            //发送给客户端，指定客户端本地隧道ID
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelS2CForwardData, respData);
        }
        #endregion
    }
}