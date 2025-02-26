﻿using AxibugProtobuf;
using Google.Protobuf;
using NoSugarNet.Adapter;
using NoSugarNet.Adapter.DataHelper;
using NoSugarNet.ClientCore;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerCore.Manager
{
    public class AppForwardLocalClient
    {
        Dictionary<byte, Protobuf_Cfgs_Single> mDictTunnelID2Cfg = new Dictionary<byte, Protobuf_Cfgs_Single>();
        Dictionary<byte, ForwardLocalListener> mDictTunnelID2Listeners = new Dictionary<byte, ForwardLocalListener>();
        NoSugarNet.Adapter.DataHelper.E_CompressAdapter compressAdapterType;

        public long tReciveAllLenght { get; private set; }
        public long tSendAllLenght { get; private set; }

        public AppForwardLocalClient()
        {
            //注册网络消息
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdServerCfgs, Recive_CmdCfgs);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CForwardConnect, Recive_TunnelS2CConnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CForwardDisconnect, Recive_TunnelS2CDisconnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CForwardData, Recive_TunnelS2CData);
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
            ////初始化压缩适配器，代表压缩类型
            //mCompressAdapter = new NoSugarNet.Adapter.DataHelper.CompressAdapter(compressAdapterType);
            foreach (var cfg in mDictTunnelID2Cfg)
            {
                ForwardLocalListener listener = new ForwardLocalListener(256, 1024, cfg.Key,AppNoSugarNet.user.userdata.UID);
                AppNoSugarNet.log.Info($"开始监听配置 Tunnel:{cfg.Key},Port:{cfg.Value.Port}");
                listener.BandEvent(AppNoSugarNet.log.Log, OnClientLocalConnect, OnClientLocalDisconnect, OnClientTunnelDataCallBack);
                listener.StartListener((uint)cfg.Value.Port);
                //listener.Init();
                //listener.Start(new IPEndPoint(IPAddress.Any.Address, (int)cfg.Value.Port));
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
        void AddLocalListener(ForwardLocalListener _listener)
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
        void RemoveLocalListener(ForwardLocalListener _listener)
        {
            lock (mDictTunnelID2Listeners)
            {
                if (mDictTunnelID2Listeners.ContainsKey(_listener.mTunnelID))
                {
                    mDictTunnelID2Listeners.Remove(_listener.mTunnelID);
                }
            }
        }
        bool GetLocalListener(byte tunnelId,out ForwardLocalListener _listener)
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
                    ForwardLocalListener _listener = mDictTunnelID2Listeners[keys[i]];
                    _listener.StopAllLocalClient();
                    _listener.StopWithClear();
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
            AppNoSugarNet.log.Debug("Forward->Recive_CmdCfgs");
            Protobuf_Cfgs msg = ProtoBufHelper.DeSerizlize<Protobuf_Cfgs>(reqData);

            for (int i = 0;i < msg.Cfgs.Count;i++) 
            {
                Protobuf_Cfgs_Single cfg = msg.Cfgs[i];
                mDictTunnelID2Cfg[(byte)cfg.TunnelID] = cfg;
            }
            compressAdapterType = (NoSugarNet.Adapter.DataHelper.E_CompressAdapter)msg.CompressAdapterType;
            InitListenerMode();
        }
        public void Recive_TunnelS2CConnect(byte[] reqData)
        {
            AppNoSugarNet.log.Debug("Forward->Recive_TunnelS2CConnect");
            Protobuf_Tunnel_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_Connect>(reqData);
            if(msg.Connected == 1)
                OnRemoteLocalConnect((byte)msg.TunnelID,(byte)msg.Idx);
            else
                OnRemoteLocalDisconnect((byte)msg.TunnelID, (byte)msg.Idx);
        }
        public void Recive_TunnelS2CDisconnect(byte[] reqData)
        {
            AppNoSugarNet.log.Debug("Forward->Recive_TunnelS2CDisconnect");
            Protobuf_Tunnel_Disconnect msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_Disconnect>(reqData);
            OnRemoteLocalDisconnect((byte)msg.TunnelID,(byte)msg.Idx);
        }
        public void Recive_TunnelS2CData(byte[] reqData)
        {
            Protobuf_Tunnel_DATA msg = ProtoBufHelper.DeSerizlize<Protobuf_Tunnel_DATA>(reqData);
            OnRemoteLocalDataCallBack((byte)msg.TunnelID,(byte)msg.Idx, msg.HunterNetCoreData.ToArray());
        }
        #endregion

        #region 两端本地端口连接事件通知
        /// <summary>
        /// 当客户端本地端口连接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnClientLocalConnect(long UID, byte tunnelId,byte _Idx)
        {
            AppNoSugarNet.log.Debug($"Forward->OnClientLocalConnect {tunnelId},{_Idx}");
            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_Connect()
            {
                TunnelID = tunnelId,
                Idx = _Idx,
            });
            
            //告知给服务端，来自客户端本地的连接建立
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardConnect, respData);
        }
        /// <summary>
        /// 当客户端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnClientLocalDisconnect(long UID, byte tunnelId, byte _Idx)
        {
            AppNoSugarNet.log.Debug($"Forward->OnClientLocalDisconnect {tunnelId},{_Idx}");
            //隧道ID定位投递服务端本地连接
            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_Disconnect()
            {
                TunnelID = tunnelId,
                Idx= _Idx,
            });

            //告知给服务端，来自客户端本地的连接断开
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardDisconnect, respData);
        }

        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="tunnelId"></param>
        public void OnRemoteLocalConnect(byte tunnelId,byte Idx)
        {
            AppNoSugarNet.log.Debug($"Forward->OnRemoteLocalConnect {tunnelId},{Idx}");
            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
                return;
            //维护状态
            _listener.SetRemoteConnectd(Idx, true);
            if (_listener.GetDictMsgQueue(Idx, out List<IdxWithMsg> msglist))
            {
                for(int i = 0; i < msglist.Count; i++) 
                {
                    IdxWithMsg msg = msglist[i];
                    //投递给服务端，来自客户端本地的连接数据
                    AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardData, msg.data);
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
        public void OnRemoteLocalDisconnect(byte tunnelId, byte Idx)
        {
            AppNoSugarNet.log.Debug($"Forward->OnRemoteLocalDisconnect {tunnelId},{Idx}");
            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
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
        public void OnRemoteLocalDataCallBack(byte tunnelId,byte Idx, byte[] data)
        {
            //AppNoSugarNet.log.Info($"OnRemoteLocalDataCallBack {tunnelId},{Idx},Data长度：{data.Length}");
            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
                return;
            //记录压缩前数据长度
            tReciveAllLenght += data.Length;
            //解压
            data = CompressAdapterSelector.Adapter(compressAdapterType).Decompress(data);
            _listener.SendSocketByIdx(Idx,data);
        }
        /// <summary>
        /// 来自客户端本地连接投递的Tunnel数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        /// <param name="data"></param>
        public void OnClientTunnelDataCallBack(long UID, byte tunnelId,byte Idx, byte[] data)
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
            data = CompressAdapterSelector.Adapter(compressAdapterType).Compress(data);
            //记录压缩后数据长度
            tSendAllLenght += data.Length;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Tunnel_DATA()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                HunterNetCoreData = ByteString.CopyFrom(data)
            });

            if (!GetLocalListener(tunnelId, out ForwardLocalListener _listener))
                return;

            //远程未连接，添加到缓存
            if (!_listener.CheckRemoteConnect(Idx))
            {
                _listener.EnqueueIdxWithMsg(Idx, respData);
                return;
            }
            //投递给服务端，来自客户端本地的连接数据
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SForwardData, respData);
        }
        #endregion

    }
}