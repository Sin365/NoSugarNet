﻿using AxibugProtobuf;
using NoSugarNet.ClientCore.Network;
using Google.Protobuf;
using System.Net.Sockets;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using NoSugarNet.ClientCore.Manager;
using System.Net;

namespace ServerCore.Manager
{
    public class AppLocalClient
    {

        Dictionary<byte, Protobuf_Cfgs_Single> mDictTunnelID2Cfg = new Dictionary<byte, Protobuf_Cfgs_Single>();
        Dictionary<byte, LocalListener> mDictTunnelID2Listeners = new Dictionary<byte, LocalListener>();
        CompressAdapter mCompressAdapter;

        public AppLocalClient() 
        {
            //初始化压缩适配器，暂时使用0，代表压缩类型
            mCompressAdapter = new CompressAdapter(0);
            //注册网络消息
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdCfgs, Recive_CmdCfgs);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CConnect, Recive_TunnelS2CConnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CDisconnect, Recive_TunnelS2CDisconnect);
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdTunnelS2CData, Recive_TunnelS2CData);
        }

        /// <summary>
        /// 初始化连接，先获取到配置
        /// </summary>
        void InitListenerMode()
        {
            foreach (var cfg in mDictTunnelID2Cfg)
            {
                LocalListener listener = new LocalListener(1024, 1024, cfg.Key);
                listener.Init();
                listener.Start(new IPEndPoint(IPAddress.Any.Address, (int)cfg.Value.Port));
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
                    mDictTunnelID2Listeners[_listener.mTunnelID] = _listener;
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
        #endregion

        #region 解析服务端下行数据
        public void Recive_CmdCfgs(byte[] reqData)
        {
            App.log.Debug("Recive_CmdCfgs");
            Protobuf_Cfgs msg = ProtoBufHelper.DeSerizlize<Protobuf_Cfgs>(reqData);

            for (int i = 0;msg.Cfgs.Count > 0;i++) 
            {
                Protobuf_Cfgs_Single cfg = msg.Cfgs[i];
                mDictTunnelID2Cfg[(byte)cfg.TunnelID] = cfg;
            }
            InitListenerMode();
        }

        public void Recive_TunnelS2CConnect(byte[] reqData)
        {
            App.log.Debug("Recive_TunnelS2CConnect");
            Protobuf_S2C_Connect msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_Connect>(reqData);
            OnServerLocalConnect((byte)msg.TunnelID,(byte)msg.Idx);
        }
        public void Recive_TunnelS2CDisconnect(byte[] reqData)
        {
            App.log.Debug("Recive_TunnelS2CDisconnect");
            Protobuf_S2C_Disconnect msg = ProtoBufHelper.DeSerizlize<Protobuf_S2C_Disconnect>(reqData);
            OnServerLocalDisconnect((byte)msg.TunnelID,(byte)msg.Idx);
        }
        public void Recive_TunnelS2CData(byte[] reqData)
        {
            App.log.Debug("Recive_TunnelS2CData");
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
            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_Connect()
            {
                TunnelID = tunnelId,
                Idx = _Idx,
            });
            
            //告知给服务端，来自客户端本地的连接建立
            App.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SConnect, respData);
        }

        /// <summary>
        /// 当客户端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnClientLocalDisconnect(byte tunnelId, byte _Idx)
        {
            //隧道ID定位投递服务端本地连接
            if (!mDictTunnelID2Cfg.ContainsKey(tunnelId))
                return;

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_Disconnect()
            {
                TunnelID = tunnelId,
                Idx= _Idx,
            });
            //告知给服务端，来自客户端本地的连接断开
            App.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SDisconnect, respData);
        }

        /// <summary>
        /// 当服务端本地端口连接
        /// </summary>
        /// <param name="tunnelId"></param>
        public void OnServerLocalConnect(byte tunnelId,byte Idx)
        {
            if (!GetLocalListener(tunnelId, out LocalListener _listener))
                return;
            //TODO 维护本地状态
        }

        /// <summary>
        /// 当服务端本地端口连接断开
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tunnelId"></param>
        public void OnServerLocalDisconnect(byte tunnelId, byte Idx)
        {
            if (!GetLocalListener(tunnelId, out LocalListener _listener))
                return;
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
            if (!GetLocalListener(tunnelId, out LocalListener _listener))
                return;

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
            //压缩
            data = mCompressAdapter.Compress(data);
            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_C2S_DATA()
            {
                TunnelID = tunnelId,
                Idx = Idx,
                HunterNetCoreData = ByteString.CopyFrom(data)
            });

            //投递给服务端，来自客户端本地的连接数据
            App.networkHelper.SendToServer((int)CommandID.CmdTunnelC2SData, respData);
        }
        #endregion
    }
}