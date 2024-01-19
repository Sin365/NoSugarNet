using HaoYueNet.ServerNetwork;
using NoSugarNet.ClientCore.Network;
using System.Net.Sockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NoSugarNet.ClientCore
{
    public class LocalListener : TcpSaeaServer_SourceMode
    {
        public byte mTunnelID;
        public LocalListener(int numConnections, int receiveBufferSize, byte TunnelID)
            : base(numConnections, receiveBufferSize)
        {
            OnClientNumberChange += ClientNumberChange;
            OnReceive += ReceiveData;
            OnDisconnected += OnDisconnect;
            OnNetLog += OnShowNetLog;

            mTunnelID = TunnelID;
        }

        private void ClientNumberChange(int num, AsyncUserToken token)
        {
            Console.WriteLine("Client数发生变化");
            //增加连接数
            if (num > 0)
            {
                int Idx = AddDictSocket(token.Socket);
                if (GetSocketByIdx(Idx, out Socket _socket))
                {
                    App.local.OnClientLocalConnect(mTunnelID, (byte)Idx);
                }
            }
        }

        /// <summary>
        /// 通过下标发送
        /// </summary>
        /// <param name="Idx"></param>
        /// <param name="data"></param>
        public void SendSocketByIdx(int Idx, byte[] data)
        {
            if (GetSocketByIdx(Idx, out Socket _socket))
            {
                SendToSocket(_socket, data);
            }
            //TODO连接前缓存数据
        }

        /// <summary>
        /// 接受包回调
        /// </summary>
        /// <param name="CMDID">协议ID</param>
        /// <param name="ERRCODE">错误编号</param>
        /// <param name="data">业务数据</param>
        private void ReceiveData(AsyncUserToken token, byte[] data)
        {
            DataCallBack(token.Socket, data);
        }

        public void DataCallBack(Socket sk, byte[] data)
        {
            App.log.Debug("收到消息 数据长度=>" + data.Length);

            if (!GetSocketIdxBySocket(sk, out int Idx))
                return;
            try
            {
                //抛出网络数据
                App.local.OnClientTunnelDataCallBack(mTunnelID, (byte)Idx, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("逻辑处理错误：" + ex.ToString());
            }
        }

        public void CloseConnectByIdx(byte Idx)
        {
            if (GetSocketByIdx(Idx, out Socket _socket))
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="sk"></param>
        public void OnDisconnect(AsyncUserToken token)
        {
            Console.WriteLine("断开连接");

            if (!GetSocketIdxBySocket(token.Socket, out int Idx))
                return;

            App.local.OnClientLocalConnect(mTunnelID, (byte)Idx);
            RemoveDictSocket(token.Socket);
        }

        public void OnShowNetLog(string msg)
        {
            App.log.Debug(msg);
        }

        #region 一个轻量级无用户连接管理
        Dictionary<nint, int> DictSocketHandle2Idx = new Dictionary<nint, int>();
        Dictionary<int, Socket> DictIdx2Socket = new Dictionary<int, Socket>();
        int mSeedIdx = 0;
        List<int> FreeIdxs = new List<int>();
        int GetNextIdx()
        {
            if (FreeIdxs.Count > 0)
            {
                int Idx = FreeIdxs[0];
                FreeIdxs.RemoveAt(0);
                return Idx;
            }
            return mSeedIdx++;
        }

        /// <summary>
        /// 追加Socket返回下标
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public int AddDictSocket(Socket socket)
        {
            if (socket == null)
                return -1;
            lock (DictSocketHandle2Idx)
            {
                int Idx = GetNextIdx();
                DictSocketHandle2Idx[socket.Handle] = Idx;
                DictIdx2Socket[Idx] = socket;
                return Idx;
            }
        }

        public void RemoveDictSocket(Socket socket)
        {
            if (socket == null)
                return;
            lock (DictSocketHandle2Idx)
            {
                if (!DictSocketHandle2Idx.ContainsKey(socket.Handle))
                    return;
                int Idx = DictSocketHandle2Idx[socket.Handle];
                FreeIdxs.Add(Idx);
                if (DictIdx2Socket.ContainsKey(Idx))
                    DictIdx2Socket.Remove(Idx);
                DictSocketHandle2Idx.Remove(socket.Handle);
            }
        }

        public bool GetSocketByIdx(int Idx, out Socket _socket)
        {
            if (!DictIdx2Socket.ContainsKey(Idx))
            {
                _socket = null;
                return false;
            }

            _socket = DictIdx2Socket[Idx];
            return true;
        }

        public bool GetSocketIdxBySocket(Socket _socket, out int Idx)
        {
            if (_socket == null)
            {
                Idx = -1;
                return false;
            }

            if (!DictSocketHandle2Idx.ContainsKey(_socket.Handle))
            {
                Idx = -1;
                return false;
            }

            Idx = DictSocketHandle2Idx[_socket.Handle];
            return true;
        }
        #endregion
    }
}
