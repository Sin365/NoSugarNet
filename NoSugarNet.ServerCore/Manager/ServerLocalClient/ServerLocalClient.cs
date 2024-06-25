//using HaoYueNet.ClientNetwork.OtherMode;
//using ServerCore.Manager;
//using System;
//using System.Security.Cryptography;

//namespace NoSugarNet.ClientCore.Network
//{
//    /// <summary>
//    /// 继承网络库，以支持网络功能
//    /// </summary>
//    public class ServerLocalClient : NetworkHelperCore_SourceMode
//    {
//        public long mUID;
//        public byte mTunnelID;
//        public byte mIdx;
//        public long mReciveAllLenght;
//        public long mSendAllLenght;
//        public ServerLocalClient(long UID,byte TunnelID, byte Idx)
//        {
//            mUID = UID;
//            mTunnelID = TunnelID;
//            mIdx = Idx;
//            //指定接收服务器数据事件
//            OnReceiveData += GetDataCallBack;
//            //断开连接
//            OnClose += OnConnectClose;
//            OnConnected += NetworkConnected;
//            //网络库调试信息输出事件，用于打印网络内容
//            OnLogOut += NetworkDeBugLog;
//        }

//        public void NetworkConnected(bool IsConnect)
//        {
//            NetworkDeBugLog($"NetworkConnected:{IsConnect}");
//            if (IsConnect)
//            {
//                ServerManager.g_Local.OnServerLocalConnect(mUID, mTunnelID, mIdx, this);
//            }
//            else
//            {
//                //连接失败
//                NetworkDeBugLog("连接失败！");
//            }
//        }

//        public void NetworkDeBugLog(string str)
//        {
//            //用于Unity内的输出
//            //Debug.Log("NetCoreDebug >> "+str);
//            Console.WriteLine("NetCoreDebug >> " + str);
//        }

//        /// <summary>
//        /// 接受包回调
//        /// </summary>
//        /// <param name="CMDID">协议ID</param>
//        /// <param name="ERRCODE">错误编号</param>
//        /// <param name="data">业务数据</param>
//        public void GetDataCallBack(byte[] data)
//        {
//            //NetworkDeBugLog("收到消息 数据长度=>" + data.Length);
//            try
//            {
//                //记录接收数据长度
//                mReciveAllLenght += data.Length;
//                //抛出网络数据
//                ServerManager.g_Local.OnServerLocalDataCallBack(mUID, mTunnelID, mIdx, data);
//            }
//            catch (Exception ex)
//            {
//                NetworkDeBugLog("逻辑处理错误：" + ex.ToString());
//            }

//        }

//        /// <summary>
//        /// 关闭连接
//        /// </summary>
//        public void OnConnectClose()
//        {
//            NetworkDeBugLog("OnConnectClose");
//            ServerManager.g_Local.OnServerLocalDisconnect(mUID, mTunnelID,mIdx,this);
//        }
//    }
//}
