//using HaoYueNet.ClientNetwork.OtherMode;

//namespace NoSugarNet.ClientCore
//{
//    /// <summary>
//    /// 继承网络库，以支持网络功能
//    /// </summary>
//    public class LocalClient : NetworkHelperCore_ListenerMode
//    {
//        public byte mTunnelID;
//        public LocalClient(byte TunnelID)
//        {
//            mTunnelID = TunnelID;
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
//            NetworkDeBugLog($"LocalListener_Connected:{IsConnect}");
//            if (IsConnect)
//            {
//                App.local.OnClientLocalConnect(mTunnelID,this);
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
//            Console.WriteLine("LocalListener_Debug >> " + str);
//        }

//        /// <summary>
//        /// 接受包回调
//        /// </summary>
//        /// <param name="CMDID">协议ID</param>
//        /// <param name="ERRCODE">错误编号</param>
//        /// <param name="data">业务数据</param>
//        public void GetDataCallBack(byte[] data)
//        {
//            NetworkDeBugLog("收到消息 数据长度=>" + data.Length);
//            try
//            {
//                //抛出网络数据
//                App.local.OnClientTunnelDataCallBack(mTunnelID, data);
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
//            NetworkDeBugLog("LocalListener_OnConnectClose");
//            App.local.OnClientLocalDisconnect(mTunnelID,this);
//        }
//    }
//}
