﻿//using HaoYueNet.ServerNetwork;
//using System.Net.Sockets;

//namespace NoSugarNet.ClientCore
//{
//    public class LocalListener : TcpSaeaServer_SourceMode
//    {
//        public byte mTunnelID;
//        public long mReciveAllLenght;
//        public long mSendAllLenght;
//        public long currSeed;
//        static long Seed;

//        public LocalListener(int numConnections, int receiveBufferSize, byte TunnelID)
//            : base(numConnections, receiveBufferSize)
//        {
//            OnClientNumberChange += ClientNumberChange;
//            OnReceive += ReceiveData;
//            OnDisconnected += OnDisconnect;
//            OnNetLog += OnShowNetLog;

//            mTunnelID = TunnelID;

//            currSeed = Seed++;
//        }

//        private void ClientNumberChange(int num, AsyncUserToken token)
//        {
//            AppNoSugarNet.log.Info("Client数发生变化");
//            //增加连接数stsc
//            if (num > 0)
//            {
//                int Idx = AddDictSocket(token.Socket);
//                if (GetSocketByIdx(Idx, out LocalClientInfo _localClientInf))
//                {
//                    AppNoSugarNet.local.OnClientLocalConnect(mTunnelID, (byte)Idx);
//                }
//            }
//        }

//        /// <summary>
//        /// 通过下标发送
//        /// </summary>
//        /// <param name="Idx"></param>
//        /// <param name="data"></param>
//        public void SendSocketByIdx(int Idx, byte[] data)
//        {
//            if (GetSocketByIdx(Idx, out LocalClientInfo _localClientInfo))
//            {
//                mSendAllLenght += data.Length;
//                SendToSocket(_localClientInfo._socket, data);
//            }
//            //TODO连接前缓存数据
//        }

//        /// <summary>
//        /// 接受包回调
//        /// </summary>
//        /// <param name="CMDID">协议ID</param>
//        /// <param name="ERRCODE">错误编号</param>
//        /// <param name="data">业务数据</param>
//        private void ReceiveData(AsyncUserToken token, byte[] data)
//        {
//            DataCallBack(token.Socket, data);
//        }

//        public void DataCallBack(Socket sk, byte[] data)
//        {
//            //AppNoSugarNet.log.Info("收到消息 数据长度=>" + data.Length);
//            //记录接受长度
//            mReciveAllLenght += data.Length;
//            if (!GetSocketIdxBySocket(sk, out int Idx))
//                return;
//            try
//            {
//                //抛出网络数据
//                AppNoSugarNet.local.OnClientTunnelDataCallBack(mTunnelID, (byte)Idx, data);
//            }
//            catch (Exception ex)
//            {
//                AppNoSugarNet.log.Info("逻辑处理错误：" + ex.ToString());
//            }
//        }

//        public void CloseConnectByIdx(byte Idx)
//        {
//            if (GetSocketByIdx(Idx, out LocalClientInfo _localClientInfo))
//            {
//                //把未发送消息队列回收了
//                while (_localClientInfo.msgQueue.Count > 0)
//                {
//                    IdxWithMsg msg = _localClientInfo.msgQueue.Dequeue();
//                    AppNoSugarNet.local._localMsgPool.Enqueue(msg);
//                }

//                _localClientInfo._socket.Shutdown(SocketShutdown.Both);
//            }
//        }

//        /// <summary>
//        /// 断开连接
//        /// </summary>
//        /// <param name="sk"></param>
//        public void OnDisconnect(AsyncUserToken token)
//        {
//            AppNoSugarNet.log.Info("断开连接");

//            if (!GetSocketIdxBySocket(token.Socket, out int Idx))
//                return;

//            AppNoSugarNet.local.OnClientLocalDisconnect(mTunnelID, (byte)Idx);
//            RemoveDictSocket(token.Socket);
//        }

//        public void OnShowNetLog(string msg)
//        {
//            AppNoSugarNet.log.Info(msg);
//        }

//        #region 一个轻量级无用户连接管理
//        Dictionary<IntPtr, int> DictSocketHandle2Idx = new Dictionary<IntPtr, int>();
//        Dictionary<int, LocalClientInfo> DictIdx2LocalClientInfo = new Dictionary<int, LocalClientInfo>();
//        int mSeedIdx = 0;
//        List<int> FreeIdxs = new List<int>();
//        public class LocalClientInfo
//        {
//            public Socket _socket;
//            public bool bRemoteConnect;
//            public bool bLocalConnect => _socket.Connected;
//            public Queue<IdxWithMsg> msgQueue = new Queue<IdxWithMsg>();
//        }

//        public Dictionary<int, LocalClientInfo> GetDictIdx2LocalClientInfo()
//        {
//            return DictIdx2LocalClientInfo;
//        }

//        int GetNextIdx()
//        {
//            if (FreeIdxs.Count > 0)
//            {
//                int Idx = FreeIdxs[0];
//                FreeIdxs.RemoveAt(0);
//                return Idx;
//            }
//            return mSeedIdx++;
//        }

//        void ResetFree()
//        {
//            FreeIdxs.Clear();
//            mSeedIdx = 0;
//        }

//        /// <summary>
//        /// 追加Socket返回下标
//        /// </summary>
//        /// <param name="socket"></param>
//        /// <returns></returns>
//        public int AddDictSocket(Socket socket)
//        {
//            if (socket == null)
//                return -1;
//            lock (DictSocketHandle2Idx)
//            {
//                int Idx = GetNextIdx();
//                DictSocketHandle2Idx[socket.Handle] = Idx;
//                DictIdx2LocalClientInfo[Idx] = new LocalClientInfo() { _socket = socket,bRemoteConnect = false};
//                AppNoSugarNet.log.Debug($"AddDictSocket mTunnelID->{mTunnelID} Idx->{Idx} socket.Handle{socket.Handle}");
//                return Idx;
//            }
//        }

//        public void RemoveDictSocket(Socket socket)
//        {
//            if (socket == null)
//                return;
//            lock (DictSocketHandle2Idx)
//            {
//                if (!DictSocketHandle2Idx.ContainsKey(socket.Handle))
//                    return;
//                int Idx = DictSocketHandle2Idx[socket.Handle];
//                FreeIdxs.Add(Idx);
//                if (DictIdx2LocalClientInfo.ContainsKey(Idx))
//                    DictIdx2LocalClientInfo.Remove(Idx);
//                DictSocketHandle2Idx.Remove(socket.Handle);
//                AppNoSugarNet.log.Debug($"RemoveDictSocket mTunnelID->{mTunnelID} Idx->{Idx} socket.Handle{socket.Handle}");
//            }
//        }

//        bool GetSocketByIdx(int Idx, out LocalClientInfo _localClientInfo)
//        {
//            if (!DictIdx2LocalClientInfo.ContainsKey(Idx))
//            {
//                _localClientInfo = null;
//                return false;
//            }

//            _localClientInfo = DictIdx2LocalClientInfo[Idx];
//            return true;
//        }

//        public bool GetSocketIdxBySocket(Socket _socket, out int Idx)
//        {
//            if (_socket == null)
//            {
//                Idx = -1;
//                return false;
//            }

//            if (!DictSocketHandle2Idx.ContainsKey(_socket.Handle))
//            {
//                Idx = -1;
//                return false;
//            }

//            Idx = DictSocketHandle2Idx[_socket.Handle];
//            return true;
//        }

//        public bool CheckRemoteConnect(int Idx)
//        {
//            if (!GetSocketByIdx(Idx, out LocalClientInfo _localClientInfo))
//                return false;
//            return _localClientInfo.bRemoteConnect;
//        }

//        public void SetRemoteConnectd(int Idx,bool bConnected)
//        {
//            if (!GetSocketByIdx(Idx, out LocalClientInfo _localClientInfo))
//                return;
//            if (bConnected)
//                AppNoSugarNet.log.Info("远端本地连接已连接！！！！");
//            else
//                AppNoSugarNet.log.Info("远端本地连接已断开连接！！！！");
//            _localClientInfo.bRemoteConnect = bConnected;
//        }

//        public void StopAllLocalClient()
//        {
//            lock (DictIdx2LocalClientInfo)
//            {
//                int[] Idxs =  DictIdx2LocalClientInfo.Keys.ToArray();
//                for (int i = 0; i < Idxs.Length; i++)
//                {
//                    CloseConnectByIdx((byte)Idxs[i]);
//                }
//                DictIdx2LocalClientInfo.Clear();
//                DictSocketHandle2Idx.Clear();
//                ResetFree();

//                //清理事件
//                OnClientNumberChange -= ClientNumberChange;
//                OnReceive -= ReceiveData;
//                OnDisconnected -= OnDisconnect;
//                OnNetLog -= OnShowNetLog;
//            }
//        }

//        #endregion


//        #region 缓存
//        public void EnqueueIdxWithMsg(byte Idx, byte[] data)
//        {
//            if (!GetSocketByIdx(Idx, out LocalClientInfo _localClientInfo))
//                return;

//            IdxWithMsg Msg = AppNoSugarNet.local._localMsgPool.Dequeue();
//            Msg.Idx = Idx;
//            Msg.data = data;
//            _localClientInfo.msgQueue.Enqueue(Msg);
//        }
//        public bool GetDictMsgQueue(byte Idx,out List<IdxWithMsg> MsgList)
//        {
//            if (!GetSocketByIdx(Idx, out LocalClientInfo _localClientInfo) || _localClientInfo.msgQueue.Count < 1)
//            {
//                MsgList = null;
//                return false;
//            }

//            MsgList = new List<IdxWithMsg>();
//            lock (_localClientInfo.msgQueue)
//            {
//                while (_localClientInfo.msgQueue.Count > 0)
//                {
//                    IdxWithMsg msg = _localClientInfo.msgQueue.Dequeue();
//                    MsgList.Add(msg);
//                }
//                return true;
//            }
//        }
//        #endregion
//    }
//}
