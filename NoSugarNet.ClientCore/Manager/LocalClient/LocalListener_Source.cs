//using HaoYueNet.ClientNetwork.OtherMode;
//using HaoYueNet.ServerNetwork;
//using System.Net.Sockets;

//namespace NoSugarNet.ClientCore
//{
//    public class LocalListener_Source : NetworkHelperCore_ListenerMode
//    {
//        public byte mTunnelID;
//        public long mReciveAllLenght;
//        public long mSendAllLenght;
//        public LocalListener_Source(int numConnections, int receiveBufferSize, byte TunnelID)
//            : base()
//        {
//            OnConnected += ClientNumberChange;
//            OnReceive += ReceiveData;
//            OnDisconnected += OnDisconnectClient;
//            OnNetLog += OnShowNetLog;

//            mTunnelID = TunnelID;
//        }

//        private void ClientNumberChange(Socket socket)
//        {
//            AppNoSugarNet.log.Info("Client数发生变化");
//            //增加连接数
//            int Idx = AddDictSocket(socket);
//            if (GetSocketByIdx(Idx, out LocalClientInfo _localClientInf))
//            {
//                AppNoSugarNet.local.OnClientLocalConnect(mTunnelID, (byte)Idx);
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
//                SendToClient(_localClientInfo._socket, data);
//            }
//            //TODO连接前缓存数据
//        }

//        /// <summary>
//        /// 接受包回调
//        /// </summary>
//        /// <param name="CMDID">协议ID</param>
//        /// <param name="ERRCODE">错误编号</param>
//        /// <param name="data">业务数据</param>
//        private void ReceiveData(Socket sk, byte[] data)
//        {
//            DataCallBack(sk, data);
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
//                if (GetMsgQueueByIdx(sk.Handle, out Queue<byte[]> _queue))
//                {
//                    lock (_queue) 
//                    {
//                        _queue.Enqueue(data);
//                        while (_queue.Count > 0)
//                        { 
//                            AppNoSugarNet.local.OnClientTunnelDataCallBack(mTunnelID, (byte)Idx, _queue.Dequeue());
//                        }
//                    }
//                }

//                ////抛出网络数据
//                //AppNoSugarNet.local.OnClientTunnelDataCallBack(mTunnelID, (byte)Idx, data);
//            }
//            catch (Exception ex)
//            {
//                AppNoSugarNet.log.Info("逻辑处理错误：" + ex.ToString());
//            }
//        }

//        public void CloseConnectByIdx(byte Idx)
//        {
//            if (GetSocketByIdx(Idx, out LocalClientInfo _localClientInf))
//            {
//                _localClientInf._socket.Shutdown(SocketShutdown.Both);
//            }
//        }

//        /// <summary>
//        /// 断开连接
//        /// </summary>
//        /// <param name="sk"></param>
//        public void OnDisconnectClient(Socket sk)
//        {
//            AppNoSugarNet.log.Info("断开连接");

//            if (!GetSocketIdxBySocket(sk, out int Idx))
//                return;

//            AppNoSugarNet.local.OnClientLocalDisconnect(mTunnelID, (byte)Idx);
//            RemoveDictSocket(sk);
//        }

//        public void OnShowNetLog(string msg)
//        {
//            AppNoSugarNet.log.Info(msg);
//        }

//        #region 一个轻量级无用户连接管理
//        Dictionary<nint, int> DictSocketHandle2Idx = new Dictionary<nint, int>();
//        Dictionary<nint, Queue<byte[]>> DictSocketHandle2Msg = new Dictionary<nint, Queue<byte[]>>();
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
//                DictSocketHandle2Msg[socket.Handle] = new Queue<byte[]>();
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
                
//                if (DictSocketHandle2Msg.ContainsKey(socket.Handle))
//                    DictSocketHandle2Msg.Remove(socket.Handle);

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

//        bool GetMsgQueueByIdx(nint handle, out Queue<byte[]> _queue)
//        {
//            if (!DictSocketHandle2Msg.ContainsKey(handle))
//            {
//                _queue = null;
//                return false;
//            }

//            _queue = DictSocketHandle2Msg[handle];
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

//        public void StopAll()
//        {
//            lock (DictIdx2LocalClientInfo)
//            {
//                int[] Idxs =  DictIdx2LocalClientInfo.Keys.ToArray();
//                for (int i = 0; i < Idxs.Length; i++)
//                {
//                    CloseConnectByIdx((byte)Idxs[i]);
//                }
//                DictIdx2LocalClientInfo.Clear();
//                FreeIdxs.Clear();
//                mSeedIdx = 0;
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
