using AxibugProtobuf;
using Google.Protobuf;
using HaoYueNet.ServerNetwork;
using NoSugarNet.ClientCore;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore.Network;
using NoSugarNet.DataHelper;
using System.Net;

namespace ServerCore.Manager
{
    public class AppLocalClient
    {
        Dictionary<byte, Protobuf_Cfgs_Single> mDictTunnelID2Cfg = new Dictionary<byte, Protobuf_Cfgs_Single>();
        Dictionary<byte, LocalListener> mDictTunnelID2Listeners = new Dictionary<byte, LocalListener>();
        CompressAdapter mCompressAdapter;
        E_CompressAdapter compressAdapterType;
        public LocalMsgQueuePool _localMsgPool = new LocalMsgQueuePool(1000);

        public long tReciveAllLenght { get; private set; }
        public long tSendAllLenght { get; private set; }

        public AppLocalClient()
        {
            //注册网络消息
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdCfgs, Recive_CmdCfgs);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CConnect, Recive_TunnelS2CConnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CDisconnect, Recive_TunnelS2CDisconnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CData, Recive_TunnelS2CData);
        }

        public void GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght)
        {
            resultReciveAllLenght = 0;
            resultSendAllLenght = 0;
            byte[] Keys = mDictTunnelID2Listeners.Keys.ToArray();
            for (int i = 0; i < Keys.Length; i++)
            {
                //local和转发 收发相反
                resultSendAllLenght += mDictTunnelID2Listeners[Keys[i]].mReciveAllLenght;
                resultReciveAllLenght += mDictTunnelID2Listeners[Keys[i]].mSendAllLenght;
            }
        }


        public void GetClientCount(out int ClientUserCount, out int TunnelCount)
        {
            TunnelCount = mDictTunnelID2Listeners.Count;
            ClientUserCount = mDictTunnelID2Listeners.Count;
        }

        public void GetClientDebugInfo()
        {
            AppNoSugarNet.log.Debug($"------------ mDictTunnelID2Listeners {mDictTunnelID2Listeners.Count} ------------");
            lock (mDictTunnelID2Listeners)
            {
                foreach (var item in mDictTunnelID2Listeners)
                {
                    var cinfo = item.Value.GetDictIdx2LocalClientInfo();
                    AppNoSugarNet.log.Debug($"----- TunnelID {item.Key} ObjcurrSeed->{item.Value.currSeed} ClientList->{item.Value.ClientList.Count} Idx2LocalClient->{cinfo.Count} -----");
                    
                    foreach (var c in cinfo)
                    {
                        AppNoSugarNet.log.Debug($"----- Idx {c.Key} bRemoteConnect->{c.Value.bRemoteConnect} msgQueue.Count->{c.Value.msgQueue.Count} -----");
                    }
                }
            }
        }

        /// <summary>
        /// 初始化连接，先获取到配置
        /// </summary>
        void InitListenerMode()
        {
            AppNoSugarNet.log.Info("初始化压缩适配器" + compressAdapterType);
            //初始化压缩适配器，代表压缩类型
            mCompressAdapter = new CompressAdapter(compressAdapterType);
            foreach (var cfg in mDictTunnelID2Cfg)
            {
                LocalListener listener = new LocalListener(256, 1024, cfg.Key);
                AppNoSugarNet.log.Info($"开始监听配置 Tunnel:{cfg.Key},Port:{cfg.Value.Port}");
                listener.Init();
                listener.Start(new IPEndPoint(IPAddress.Any.Address, (int)cfg.Value.Port));
                //listener.Init((int)cfg.Value.Port);
                AddLocalListener(listener);
            }

        }

        #region 连接字典管理
        /// <summary>
        /// 追加监听者
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="serverClient"></param>
        void AddLocalListener(LocalListener _listener)
        {
            lock (mDictTunnelID2Listeners)
            {
                mDictTunnelID2Listeners[_listener.mTunnelID] = _listener;
            }
        }
        /// <summary>
        /// 删除监听
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="serverClient"></param>
        void RemoveLocalListener(LocalListener _listener)
        {
            lock (mDictTunnelID2Listeners)
            {
                if (mDictTunnelID2Listeners.ContainsKey(_listener.mTunnelID))
                {
                    mDictTunnelID2Listeners.Remove(_listener.mTunnelID);
                }
            }
        }
        bool GetLocalListener(byte tunnelId,out LocalListener _listener)
        {
            _listener = null;
            if (!mDictTunnelID2Listeners.ContainsKey(tunnelId))
                return false;

            _listener = mDictTunnelID2Listeners[tunnelId];
            return true;
        }
        public void StopAll()
        {
            lock (mDictTunnelID2Listeners) 
            {
                byte[] keys = mDictTunnelID2Listeners.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++) 
                {
                    LocalListener _listener = mDictTunnelID2Listeners[keys[i]];
                    _listener.StopAllLocalClient();
                    _listener.Stop();
                    //_listener.Stop();
                    RemoveLocalListener(_listener);
                }
                mDictTunnelID2Listeners.Clear();
            }
        }
        #endregion

        #region 解析服务端下行数据
        public void Recive_CmdCfgs(byte[] reqData)
        {
            AppNoSugarNet.log.Debug("Recive_CmdCfgs");
            Protobuf_Cfgs msg = ProtoBufHelper.DeSerizlize<Protobuf_Cfgs>(reqData);

            for (int i = 0;i < msg.Cfgs.Count;i++) 
            {
                Protobuf_Cfgs_Single cfg = msg.Cfgs[i];
                mDictTunnelID2Cfg[(byte)cfg.TunnelID] = cfg;
            }
            compressAdapterType = (E_CompressAdapter)msg.CompressAdapterType;
            InitListenerMode();
        }
        public void Recive_TunnelS2CConnect(byte[] reqData)
        {
            AppNoSugarNet.log.Debug("Recive_TunnelS2CConnect");
            Protobuf_S2C_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_Connect>(reqData);
            if(msg.Connected == 1)
                OnServerLocalConnect((byte)msg.TunnelID,(byte)msg.Idx);
            else
                OnServerLocalDisconnect((byte)msg.TunnelID, (byte)msg.Idx);
        }
        public void Recive_TunnelS2CDisconnect(byte[] reqData)
        {
            AppNoSugarNet.log.Debug("Recive_TunnelS2CDisconnect");
            Protobuf_S2C_Disconnect msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_Disconnect>(reqData);
            OnServerLocalDisconnect((byte)msg.TunnelID,(byte)msg.Idx);
        }
        public void Recive_TunnelS2CData(byte[] reqData)
        {
            //AppNoSugarNet.log.Debug("Recive_TunnelS2CData");
            Protobuf_S2C_DATA msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_DATA>(reqData);
            OnServerLocalDataCallBack((byte)msg.TunnelID,(byte)msg.Idx, msg.HunterNetCoreData.ToArray());
        }
        #endregion

        #region 两端本地端口连接事件通知
        /// <summary>
        /// 当客户端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnClientLocalConnect(byte tunnelId,byte _Idx)
        {
            AppNoSugarNet.log.Debug($"OnClientLocalConnect {tunnelId},{_Idx}");
            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_Connect()
            {
                TunnelID = tunnelId,
                Idx = _Idx,
            });
            
            //告知给服务端，来自客户端本地的连接建立
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SConnect, respData);
        }
        /// <summary>
        /// 当客户端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnClientLocalDisconnect(byte tunnelId, byte _Idx)
        {
            AppNoSugarNet.log.Debug($"OnClientLocalDisconnect {tunnelId},{_Idx}");
            //隧道ID定位投递服务端本地连接
            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_Disconnect()
            {
                TunnelID = tunnelId,
                Idx= _Idx,
            });

            //告知给服务端，来自客户端本地的连接断开
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SDisconnect, respData);
        }

        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="tunnelId"></param>
        public void OnServerLocalConnect(byte tunnelId,byte Idx)
        {
            AppNoSugarNet.log.Debug($"OnServerLocalConnect {tunnelId},{Idx}");
            if (!GetLocalListener(tunnelId, out LocalListener _listener))
                return;
            //维护状态
            _listener.SetRemoteConnectd(Idx, true);
            if (_listener.GetDictMsgQueue(Idx, out List<IdxWithMsg> msglist))
            {
                for(int i = 0; i < msglist.Count; i++) 
                {
                    IdxWithMsg msg = msglist[i];
                    //投递给服务端，来自客户端本地的连接数据
                    AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SData, msg.data);
                    //发送后回收
                    AppNoSugarNet.local._localMsgPool.Enqueue(msg);
                }
            }
        }
        /// <summary>
        /// 当服务端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnServerLocalDisconnect(byte tunnelId, byte Idx)
        {
            AppNoSugarNet.log.Debug($"OnServerLocalDisconnect {tunnelId},{Idx}");
            if (!GetLocalListener(tunnelId, out LocalListener _listener))
                return;
            _listener.SetRemoteConnectd(Idx,false);
            _listener.CloseConnectByIdx(Idx);
        }
        #endregion

        #region 数据投递
        /// <summary>
        /// 来自服务端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnServerLocalDataCallBack(byte tunnelId,byte Idx, byte[] data)
        {
            //AppNoSugarNet.log.Info($"OnServerLocalDataCallBack {tunnelId},{Idx},Data长度：{data.Length}");
            if (!GetLocalListener(tunnelId, out LocalListener _listener))
                return;
            //记录压缩前数据长度
            tReciveAllLenght += data.Length;
            //解压
            data = mCompressAdapter.Decompress(data);
            _listener.SendSocketByIdx(Idx,data);
        }
        /// <summary>
        /// 来自客户端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnClientTunnelDataCallBack(byte tunnelId,byte Idx, byte[] data)
        {
            //AppNoSugarNet.log.Info($"OnClientTunnelDataCallBack {tunnelId},{Idx} data.Length->{data.Length}");

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

                    SendDataToRemote(tunnelId, Idx, tempSpanSlien.ToArray());
                }
                return;
            }
            SendDataToRemote(tunnelId, Idx, data);
        }

        void SendDataToRemote(byte tunnelId, byte Idx, byte[] data)
        {
            //压缩
            data = mCompressAdapter.Compress(data);
            //记录压缩后数据长度
            tSendAllLenght += data.Length;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_DATA()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                HunterNetCoreData = ByteString.CopyFrom(data)
            });

            if (!GetLocalListener(tunnelId, out LocalListener _listener))
                return;

            //远程未连接，添加到缓存
            if (!_listener.CheckRemoteConnect(Idx))
            {
                _listener.EnqueueIdxWithMsg(Idx, respData);
                return;
            }
            //投递给服务端，来自客户端本地的连接数据
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SData, respData);
        }
        #endregion

    }
}