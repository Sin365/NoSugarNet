//using AxibugProtobuf;
//using Google.Protobuf;
//using NoSugarNet.Adapter;
//using NoSugarNet.Adapter.DataHelper;
//using NoSugarNet.ServerCore.Common;
//using ServerCore.Common;
//using ServerCore.NetWork;
//using System.Net.Sockets;

//namespace ServerCore.Manager
//{
//    public class ReverseLocalClientManager
//    {
//        Dictionary<long, ForwardLocalListener> mDictCommKey2LocalListeners = new Dictionary<long, ForwardLocalListener>();

//        public long tReciveAllLenght { get; private set; }
//        public long tSendAllLenght { get; private set; }

//        static long GetCommKey(long Uid, int Tunnel)
//        {
//            return (Uid * 10000000) + (Tunnel * 10000);
//        }

//        static long GetUidForCommKey(long CommKey)
//        {
//            return CommKey / 10000000;
//        }


//        public ReverseLocalClientManager()
//        {
//            //注册网络消息
//            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SReverseConnect, Recive_TunnelC2SConnect);
//            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SReverseDisconnect, Recive_TunnelC2SDisconnect);
//            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SReverseData, Recive_TunnelC2SData);
//        }


//        /// <summary>
//        /// 初始化连接
//        /// </summary>
//        void InitListenerMode(long UID)
//        {
//            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo _c))
//                return;

//            //初始化压缩适配器，代表压缩类型
//            ServerManager.g_Log.Info("初始化压缩适配器" + _c.e_CompressAdapter);
//            foreach (var cfg in _c._cfgs)
//            {
//                ForwardLocalListener listener = new ForwardLocalListener(256, 1024, cfg.Key, UID);
//                ServerManager.g_Log.Info($"开始监听配置 Tunnel:{cfg.Key},Port:{cfg.Value.Port}");
//                listener.BandEvent(ServerManager.g_Log.Log, OnLocalConnect, OnLocalDisconnect, OnTunnelDataCallBack);
//                listener.StartListener((uint)cfg.Value.Port);
//                AddLocalListener(UID,listener);
//            }
//        }

//        #region 连接字典管理
//        /// <summary>
//        /// 追加监听者
//        /// </summary>
//        /// <param name="tunnelId"></param>
//        /// <param name="serverClient"></param>
//        void AddLocalListener(long UID,ForwardLocalListener _listener)
//        {
//            long Key = GetCommKey(UID, _listener.mTunnelID);
//            lock (mDictCommKey2LocalListeners)
//            {
//                mDictCommKey2LocalListeners[Key] = _listener;
//            }
//        }
//        /// <summary>
//        /// 删除监听
//        /// </summary>
//        /// <param name="tunnelId"></param>
//        /// <param name="serverClient"></param>
//        void RemoveLocalListener(long UID, ForwardLocalListener _listener)
//        {
//            long Key = GetCommKey(UID, _listener.mTunnelID);
//            lock (mDictCommKey2LocalListeners)
//            {
//                if (mDictCommKey2LocalListeners.ContainsKey(Key))
//                    mDictCommKey2LocalListeners.Remove(Key);
//            }
//        }
//        bool GetLocalListener(long UID, byte tunnelId, out ForwardLocalListener _listener)
//        {
//            long Key = GetCommKey(UID, tunnelId);
//            _listener = null;
//            if (!mDictCommKey2LocalListeners.ContainsKey(Key))
//                return false;

//            _listener = mDictCommKey2LocalListeners[Key];
//            return true;
//        }
//        public void StopAllByUid(long UID)
//        {
//            lock (mDictCommKey2LocalListeners)
//            {
//                long[] keys = mDictCommKey2LocalListeners.Keys.ToArray();
//                for (int i = 0; i < keys.Length; i++)
//                {
//                    ForwardLocalListener _listener = mDictCommKey2LocalListeners[keys[i]];
//                    if (_listener.mUid != UID)
//                        continue;
//                    _listener.StopAllLocalClient();
//                    _listener.StopWithClear();
//                    //_listener.Stop();
//                    RemoveLocalListener(UID,_listener);
//                }
//                //服务端得按用户分开
//                //mDictCommKey2ServerLocalClients.Clear();
//            }
//        }
//        #endregion


//        #region 解析客户端下行数据
//        public void Recive_CmdCfgs(Socket sk, byte[] reqData)
//        {
//            ServerManager.g_Log.Debug("Recive_CmdCfgs");
//            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
//            Protobuf_Cfgs msg = ProtoBufHelper.DeSerizlize<Protobuf_Cfgs>(reqData);

//            Dictionary<byte, Protobuf_Cfgs_Single> tempDictTunnelID2Cfg = new Dictionary<byte, Protobuf_Cfgs_Single>();
//            for (int i = 0; i < msg.Cfgs.Count; i++)
//            {
//                Protobuf_Cfgs_Single cfg = msg.Cfgs[i];
//                tempDictTunnelID2Cfg[(byte)cfg.TunnelID] = cfg;
//            }
//            ServerManager.g_ClientMgr.SetUserCfg(_c.UID, (NoSugarNet.Adapter.DataHelper.E_CompressAdapter)msg.CompressAdapterType, tempDictTunnelID2Cfg);
//            InitListenerMode(_c.UID);
//        }
//        public void Recive_TunnelS2CConnect(byte[] reqData)
//        {
//            ServerManager.g_Log.Debug("Recive_TunnelS2CConnect");
//            Protobuf_Tunnel_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_Connect>(reqData);
//            if (msg.Connected == 1)
//                OnServerLocalConnect((byte)msg.TunnelID, (byte)msg.Idx);
//            else
//                OnServerLocalDisconnect((byte)msg.TunnelID, (byte)msg.Idx);
//        }
//        public void Recive_TunnelS2CDisconnect(byte[] reqData)
//        {
//            ServerManager.g_Log.Debug("Recive_TunnelS2CDisconnect");
//            Protobuf_Tunnel_Disconnect msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_Disconnect>(reqData);
//            OnServerLocalDisconnect((byte)msg.TunnelID, (byte)msg.Idx);
//        }
//        public void Recive_TunnelS2CData(byte[] reqData)
//        {
//            //ServerManager.g_Log.Debug("Recive_TunnelS2CData");
//            Protobuf_Tunnel_DATA msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_DATA>(reqData);
//            OnServerLocalDataCallBack((byte)msg.TunnelID, (byte)msg.Idx, msg.HunterNetCoreData.ToArray());
//        }
//        #endregion


//        #region 两端本地端口连接事件通知
//        /// <summary>
//        /// 当客户端本地端口连接
//        /// </summary>
//        /// <param name="uid"></param>
//        /// <param name="tunnelId"></param>
//        public void OnLocalConnect(long UID, byte tunnelId, byte _Idx)
//        {
//            ServerManager.g_Log.Debug($"OnLocalConnect {UID},{tunnelId},{_Idx}");

//            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
//                return;

//            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_Connect()
//            {
//                TunnelID = tunnelId,
//                Idx = _Idx,
//            });

//            //告知给服务端，来自客户端本地的连接建立
//            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardConnect, respData);
//        }
//        /// <summary>
//        /// 当客户端本地端口连接断开
//        /// </summary>
//        /// <param name="uid"></param>
//        /// <param name="tunnelId"></param>
//        public void OnLocalDisconnect(long UID, byte tunnelId, byte _Idx)
//        {
//            AppNoSugarNet.log.Debug($"OnClientLocalDisconnect {tunnelId},{_Idx}");
//            //隧道ID定位投递服务端本地连接
//            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
//                return;

//            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_Disconnect()
//            {
//                TunnelID = tunnelId,
//                Idx = _Idx,
//            });

//            //告知给服务端，来自客户端本地的连接断开
//            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardDisconnect, respData);
//        }

//        /// <summary>
//        /// 当服务端本地端口连接
//        /// </summary>
//        /// <param name="tunnelId"></param>
//        public void OnRemoteLocalConnect(long UID, byte tunnelId, byte Idx)
//        {
//            AppNoSugarNet.log.Debug($"OnServerLocalConnect {tunnelId},{Idx}");
//            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
//                return;
//            //维护状态
//            _listener.SetRemoteConnectd(Idx, true);
//            if (_listener.GetDictMsgQueue(Idx, out List<IdxWithMsg> msglist))
//            {
//                for (int i = 0; i < msglist.Count; i++)
//                {
//                    IdxWithMsg msg = msglist[i];
//                    //投递给服务端，来自客户端本地的连接数据
//                    AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardData, msg.data);
//                    //发送后回收
//                    LocalMsgQueuePool._localMsgPool.Enqueue(msg);
//                }
//            }
//        }
//        /// <summary>
//        /// 当服务端本地端口连接断开
//        /// </summary>
//        /// <param name="uid"></param>
//        /// <param name="tunnelId"></param>
//        public void OnRemoteLocalDisconnect(long UID, byte tunnelId, byte Idx)
//        {
//            AppNoSugarNet.log.Debug($"OnServerLocalDisconnect {tunnelId},{Idx}");
//            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
//                return;
//            _listener.SetRemoteConnectd(Idx, false);
//            _listener.CloseConnectByIdx(Idx);
//        }
//        #endregion

//        #region 数据投递
//        /// <summary>
//        /// 来自服务端本地连接投递的Tunnel数据
//        /// </summary>
//        /// <param name="uid"></param>
//        /// <param name="tunnelId"></param>
//        /// <param name="data"></param>
//        public void OnRemoteLocalDataCallBack(long UID, byte tunnelId, byte Idx, byte[] data)
//        {
//            //AppNoSugarNet.log.Info($"OnServerLocalDataCallBack {tunnelId},{Idx},Data长度：{data.Length}");
//            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
//                return;
//            //记录压缩前数据长度
//            tReciveAllLenght += data.Length;
//            //解压
//            data = mCompressAdapter.Decompress(data);
//            _listener.SendSocketByIdx(Idx, data);
//        }
//        /// <summary>
//        /// 来自客户端本地连接投递的Tunnel数据
//        /// </summary>
//        /// <param name="uid"></param>
//        /// <param name="tunnelId"></param>
//        /// <param name="data"></param>
//        public void OnTunnelDataCallBack(long UID, byte tunnelId, byte Idx, byte[] data)
//        {
//            //AppNoSugarNet.log.Info($"OnClientTunnelDataCallBack {tunnelId},{Idx} data.Length->{data.Length}");

//            int SlienLenght = 1000;
//            //判断数据量大时分包
//            if (data.Length > SlienLenght)
//            {
//                Span<byte> tempSpan = data;
//                Span<byte> tempSpanSlien = null;
//                int PageCount = (int)(data.Length / SlienLenght);
//                if (data.Length % SlienLenght > 0)
//                {
//                    PageCount++;
//                }

//                for (int i = 0; i < PageCount; i++)
//                {
//                    int StartIdx = i * SlienLenght;
//                    if (i != PageCount - 1)//不是最后一个包
//                        tempSpanSlien = tempSpan.Slice(StartIdx, SlienLenght);
//                    else//最后一个
//                        tempSpanSlien = tempSpan.Slice(StartIdx);

//                    SendDataToRemote(tunnelId, Idx, tempSpanSlien.ToArray());
//                }
//                return;
//            }
//            SendDataToRemote(tunnelId, Idx, data);
//        }

//        void SendDataToRemote(long UID, byte tunnelId, byte Idx, byte[] data)
//        {
//            //压缩
//            data = mCompressAdapter.Compress(data);
//            //记录压缩后数据长度
//            tSendAllLenght += data.Length;

//            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_DATA()
//            {
//                TunnelID = tunnelId,
//                Idx = Idx,
//                HunterNetCoreData = ByteString.CopyFrom(data)
//            });

//            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
//                return;

//            //远程未连接，添加到缓存
//            if (!_listener.CheckRemoteConnect(Idx))
//            {
//                _listener.EnqueueIdxWithMsg(Idx, respData);
//                return;
//            }
//            //投递给服务端，来自客户端本地的连接数据
//            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardData, respData);
//        }
//        #endregion
//    }
//}