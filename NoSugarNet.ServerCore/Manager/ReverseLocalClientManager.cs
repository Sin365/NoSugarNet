using AxibugProtobuf;
using Google.Protobuf;
using NoSugarNet.Adapter;
using NoSugarNet.Adapter.DataHelper;
using NoSugarNet.ServerCore.Common;
using ServerCore.Common;
using ServerCore.NetWork;
using System.Net.Sockets;

namespace ServerCore.Manager
{
    public class ReverseLocalClientManager
    {
        Dictionary<long, ForwardLocalListener> mDictCommKey2LocalListeners = new Dictionary<long, ForwardLocalListener>();

        public long tReciveAllLenght { get; private set; }
        public long tSendAllLenght { get; private set; }

        static long GetCommKey(long Uid, int Tunnel)
        {
            return (Uid * 10000000) + (Tunnel * 10000);
        }

        static long GetUidForCommKey(long CommKey)
        {
            return CommKey / 10000000;
        }

        public void GetCurrLenght(out long resultReciveAllLenght, out long resultSendAllLenght)
        {
            resultReciveAllLenght = 0;
            resultSendAllLenght = 0;
            long[] Keys = mDictCommKey2LocalListeners.Keys.ToArray();
            for (int i = 0; i < Keys.Length; i++)
            {
                //local和转发 收发相反
                resultSendAllLenght += mDictCommKey2LocalListeners[Keys[i]].mReciveAllLenght;
                resultReciveAllLenght += mDictCommKey2LocalListeners[Keys[i]].mSendAllLenght;
            }
        }

        public void GetClientCount(out int ClientUserCount, out int TunnelCount)
        {
            TunnelCount = mDictCommKey2LocalListeners.Count;
            long[] CommIDKeys = mDictCommKey2LocalListeners.Keys.ToArray();
            List<long> TempHadLocalConnetList = new List<long>();
            for (int i = 0; i < CommIDKeys.Length; i++)
            {
                long uid = GetUidForCommKey(CommIDKeys[i]);
                if (!TempHadLocalConnetList.Contains(uid))
                    TempHadLocalConnetList.Add(uid);
            }
            ClientUserCount = TempHadLocalConnetList.Count;
        }

        public ReverseLocalClientManager()
        {
            //注册网络消息
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdClientCfgs, Recive_CmdCfgs);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SReverseConnect, Recive_TunnelC2SConnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SReverseDisconnect, Recive_TunnelC2SDisconnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelC2SReverseData, Recive_TunnelC2SData);
        }

        /// <summary>
        /// 初始化连接
        /// </summary>
        void InitListenerMode(long UID)
        {
            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo _c))
                return;

            //初始化压缩适配器，代表压缩类型
            ServerManager.g_Log.Info("初始化压缩适配器" + _c.e_CompressAdapter);
            foreach (var cfg in _c._cfgs)
            {
                ForwardLocalListener listener = new ForwardLocalListener(256, 1024, cfg.Key, UID);
                ServerManager.g_Log.Info($"开始监听配置 Tunnel:{cfg.Key},Port:{cfg.Value.Port}");
                listener.BandEvent(ServerManager.g_Log.Log, OnLocalConnect, OnLocalDisconnect, OnTunnelDataCallBack);
                listener.StartListener((uint)cfg.Value.Port);
                AddLocalListener(UID, listener);
            }
        }

        #region 连接字典管理
        /// <summary>
        /// 追加监听者
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="serverClient"></param>
        void AddLocalListener(long UID, ForwardLocalListener _listener)
        {
            long Key = GetCommKey(UID, _listener.mTunnelID);
            lock (mDictCommKey2LocalListeners)
            {
                mDictCommKey2LocalListeners[Key] = _listener;
            }
        }
        /// <summary>
        /// 删除监听
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="serverClient"></param>
        void RemoveLocalListener(long UID, ForwardLocalListener _listener)
        {
            long Key = GetCommKey(UID, _listener.mTunnelID);
            lock (mDictCommKey2LocalListeners)
            {
                if (mDictCommKey2LocalListeners.ContainsKey(Key))
                    mDictCommKey2LocalListeners.Remove(Key);
            }
        }
        bool GetLocalListener(long UID, byte tunnelId, out ForwardLocalListener _listener)
        {
            long Key = GetCommKey(UID, tunnelId);
            _listener = null;
            if (!mDictCommKey2LocalListeners.ContainsKey(Key))
                return false;

            _listener = mDictCommKey2LocalListeners[Key];
            return true;
        }
        public void StopAllByUid(long UID)
        {
            lock (mDictCommKey2LocalListeners)
            {
                long[] keys = mDictCommKey2LocalListeners.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    ForwardLocalListener _listener = mDictCommKey2LocalListeners[keys[i]];
                    if (_listener.mUid != UID)
                        continue;
                    _listener.StopAllLocalClient();
                    _listener.StopWithClear();
                    //_listener.Stop();
                    RemoveLocalListener(UID, _listener);
                }
                //服务端得按用户分开
                //mDictCommKey2ServerLocalClients.Clear();
            }
        }
        #endregion


        #region 解析客户端下行数据
        public void Recive_CmdCfgs(Socket sk, byte[] reqData)
        {
            ServerManager.g_Log.Debug("Reverse->Recive_CmdCfgs");
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            Protobuf_Cfgs msg = ProtoBufHelper.DeSerizlize<Protobuf_Cfgs>(reqData);
            Dictionary<byte, Protobuf_Cfgs_Single> tempDictTunnelID2Cfg = new Dictionary<byte, Protobuf_Cfgs_Single>();
            for (int i = 0; i < msg.Cfgs.Count; i++)
            {
                Protobuf_Cfgs_Single cfg = msg.Cfgs[i];
                tempDictTunnelID2Cfg[(byte)cfg.TunnelID] = cfg;
            }
            //设置玩家的设置
            ServerManager.g_ClientMgr.SetUserCfg(_c.UID, (E_CompressAdapter)msg.CompressAdapterType, tempDictTunnelID2Cfg);
            InitListenerMode(_c.UID);
        }
        public void Recive_TunnelC2SConnect(Socket sk, byte[] reqData)
        {
            ServerManager.g_Log.Debug("Reverse->Recive_TunnelC2SConnect");
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            Protobuf_Tunnel_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_Connect>(reqData);

            if (msg.Connected == 1)
                OnRemoteLocalConnect(_c.UID,(byte)msg.TunnelID, (byte)msg.Idx);
            else
                OnRemoteLocalDisconnect(_c.UID, (byte)msg.TunnelID, (byte)msg.Idx);
        }
        public void Recive_TunnelC2SDisconnect(Socket sk, byte[] reqData)
        {
            ServerManager.g_Log.Debug("Reverse->Recive_TunnelC2CDisconnect");
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            Protobuf_Tunnel_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_Connect>(reqData);
            OnRemoteLocalDisconnect(_c.UID,(byte)msg.TunnelID, (byte)msg.Idx);
        }
        public void Recive_TunnelC2SData(Socket sk, byte[] reqData)
        {
            //ServerManager.g_Log.Debug("Reverse->Recive_TunnelC2SData");
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            Protobuf_Tunnel_DATA msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_DATA>(reqData);
            OnRemoteLocalDataCallBack(_c.UID, (byte)msg.TunnelID, (byte)msg.Idx, msg.HunterNetCoreData.ToArray());
        }
        #endregion


        #region 两端本地端口连接事件通知
        /// <summary>
        /// 当客户端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnLocalConnect(long UID, byte tunnelId, byte _Idx)
        {
            ServerManager.g_Log.Debug($"Reverse->OnLocalConnect {UID},{tunnelId},{_Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo client))
                return;
            if (!GetLocalListener(UID, tunnelId, out ForwardLocalListener _listener))
                return;
            if (!client._cfgs.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_Connect()
            {
                TunnelID = tunnelId,
                Idx = _Idx,
                Connected = 1
            });

            //告知给服务端，来自客户端本地的连接建立
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CReverseConnect, (int)ErrorCode.ErrorOk, respData);
        }
        /// <summary>
        /// 当客户端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnLocalDisconnect(long UID, byte tunnelId, byte _Idx)
        {
            ServerManager.g_Log.Debug($"Reverse->OnLocalDisconnect {UID},{tunnelId},{_Idx}");
            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo client))
                return;
            if (!GetLocalListener(UID, tunnelId, out ForwardLocalListener _listener))
                return;
            if (!client._cfgs.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_Disconnect()
            {
                TunnelID = tunnelId,
                Idx = _Idx,
            });

            //告知给服务端，来自客户端本地的连接断开
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CReverseDisconnect, (int)ErrorCode.ErrorOk, respData);
        }

        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="tunnelId"></param>
        public void OnRemoteLocalConnect(long UID, byte tunnelId, byte Idx)
        {
            ServerManager.g_Log.Debug($"Reverse->OnRemoteLocalConnect {UID},{tunnelId},{Idx}");

            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo client))
                return;

            if (!GetLocalListener(UID,tunnelId, out ForwardLocalListener _listener))
                return;

            //维护状态
            _listener.SetRemoteConnectd(Idx, true);
            if (_listener.GetDictMsgQueue(Idx, out List<IdxWithMsg> msglist))
            {
                for (int i = 0; i < msglist.Count; i++)
                {
                    IdxWithMsg msg = msglist[i];
                    //投递给服务端，来自客户端本地的连接数据
                    ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CReverseData, (int)ErrorCode.ErrorOk, msg.data);
                    //发送后回收
                    MsgQueuePool._MsgPool.Enqueue(msg);
                }
            }
        }

        /// <summary>
        /// 当服务端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnRemoteLocalDisconnect(long UID, byte tunnelId, byte Idx)
        {
            ServerManager.g_Log.Debug($"Reverse->OnRemoteLocalDisconnect {UID},{tunnelId},{Idx}");

            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo client))
                return;

            if (!GetLocalListener(UID, tunnelId, out ForwardLocalListener _listener))
                return;

            _listener.SetRemoteConnectd(Idx, false);
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
        public void OnRemoteLocalDataCallBack(long UID, byte tunnelId, byte Idx, byte[] data)
        {
            //ServerManager.g_Log.Debug($"Reverse->OnRemoteLocalDataCallBack {UID},{tunnelId},{Idx},Data长度：{data.Length}");

            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo client))
                return;

            if (!GetLocalListener(UID, tunnelId, out ForwardLocalListener _listener))
                return;

            //记录压缩前数据长度
            tReciveAllLenght += data.Length;
            //解压
            data = CompressAdapterSelector.Adapter(client.e_CompressAdapter).Decompress(data);
            _listener.SendSocketByIdx(Idx, data);
        }
        /// <summary>
        /// 来自客户端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnTunnelDataCallBack(long UID, byte tunnelId, byte Idx, byte[] data)
        {
            //ServerManager.g_Log.Debug($"Reverse->OnTunnelDataCallBack {UID},{tunnelId},{Idx},Data长度：{data.Length}");

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

                    SendDataToRemote(UID,tunnelId, Idx, tempSpanSlien.ToArray());
                }
                return;
            }
            SendDataToRemote(UID, tunnelId, Idx, data);
        }

        void SendDataToRemote(long UID, byte tunnelId, byte Idx, byte[] data)
        {
            //ServerManager.g_Log.Debug($"Reverse->SendDataToRemote {UID},{tunnelId},{Idx},Data长度：{data.Length}");

            if (!ServerManager.g_ClientMgr.GetClientByUID(UID, out ClientInfo client))
                return;

            if (!GetLocalListener(UID, tunnelId, out ForwardLocalListener _listener))
                return;

            //压缩
            data = CompressAdapterSelector.Adapter(client.e_CompressAdapter).Compress(data);
            //记录压缩后数据长度
            tSendAllLenght += data.Length;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_DATA()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                HunterNetCoreData = ByteString.CopyFrom(data)
            });

            //远程未连接，添加到缓存
            if (!_listener.CheckRemoteConnect(Idx))
            {
                _listener.EnqueueIdxWithMsg(Idx, respData);
                return;
            }
            //投递给服务端，来自客户端本地的连接数据
            ServerManager.g_ClientMgr.ClientSend(client, (int)CommandID.CmdTunnelS2CReverseData, (int)ErrorCode.ErrorOk, respData);
        }
        #endregion
    }
}