using HaoYueNet.ClientNetwork.OtherMode;
using System;

namespace NoSugarNet.Adapter
{
    /// <summary>
    /// 继承网络库，以支持网络功能
    /// </summary>
    public class BackwardLocalClient : NetworkHelperCore_SourceMode
    {
        public long mUID;
        public byte mTunnelID;
        public byte mIdx;
        public long mReciveAllLenght;
        public long mSendAllLenght;

        public delegate void OnBackwardLogOutHandler(int LogLevel, string Msg);
        public delegate void OnBackwardConnectHandler(long uid, byte tunnelId, byte Idx, BackwardLocalClient serverLocalClient);
        public delegate void OnBackwardDisconnectHandler(long uid, byte tunnelId, byte Idx, BackwardLocalClient serverLocalClient);
        public delegate void OnBackwardDataCallBackHandler(long uid, byte tunnelId, byte Idx, byte[] data);

        public event OnBackwardLogOutHandler OnBackwardLogOut;
        public event OnBackwardConnectHandler OnBackwardConnect;
        public event OnBackwardDisconnectHandler OnBackwardDisconnect;
        public event OnBackwardDataCallBackHandler OnBackwardDataCallBack;

        public BackwardLocalClient(long UID,byte TunnelID, byte Idx)
        {
            mUID = UID;
            mTunnelID = TunnelID;
            mIdx = Idx;
            //指定接收服务器数据事件
            OnReceiveData += GetDataCallBack;
            //断开连接
            OnClose += OnConnectClose;
            OnConnected += NetworkConnected;
            //网络库调试信息输出事件，用于打印网络内容
            OnLogOut += NetworkDeBugLog;
        }

        public void BandEvent(
            OnBackwardLogOutHandler _OnBackwardLogOut,
            OnBackwardConnectHandler _OnConnect,
            OnBackwardDisconnectHandler _OnDisconnect,
            OnBackwardDataCallBackHandler _OnDataCallBack
            )
        {
            OnBackwardLogOut += _OnBackwardLogOut;
            OnBackwardConnect += _OnConnect;
            OnBackwardDisconnect += _OnDisconnect;
            OnBackwardDataCallBack += _OnDataCallBack;
        }

        public void NetworkConnected(bool IsConnect)
        {
            NetworkDeBugLog($"NetworkConnected:{IsConnect}");
            if (IsConnect)
            {
                OnBackwardConnect?.Invoke(mUID, mTunnelID, mIdx, this);
            }
            else
            {
                //连接失败
                NetworkDeBugLog("连接失败！");
            }
        }

        public void NetworkDeBugLog(string str)
        {
            OnBackwardLogOut?.Invoke(1 ,"NetCoreDebug >> " + str);
        }

        /// <summary>
        /// 接受包回调
        /// </summary>
        /// <param name="CMDID">协议ID</param>
        /// <param name="ERRCODE">错误编号</param>
        /// <param name="data">业务数据</param>
        public void GetDataCallBack(byte[] data)
        {
            //NetworkDeBugLog("收到消息 数据长度=>" + data.Length);
            try
            {
                //记录接收数据长度
                mReciveAllLenght += data.Length;
                //抛出网络数据
                OnBackwardDataCallBack?.Invoke(mUID, mTunnelID, mIdx, data);
            }
            catch (Exception ex)
            {
                NetworkDeBugLog("逻辑处理错误：" + ex.ToString());
            }

        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        void OnConnectClose()
        {
            NetworkDeBugLog("OnConnectClose");
            OnBackwardDisconnect?.Invoke(mUID, mTunnelID,mIdx,this);
        }

        public void Release()
        {
            OnBackwardLogOut -= OnBackwardLogOut;
            OnBackwardConnect -= OnBackwardConnect;
            OnBackwardDisconnect -= OnBackwardDisconnect;
            OnBackwardDataCallBack -= OnBackwardDataCallBack;
        }
    }
}
