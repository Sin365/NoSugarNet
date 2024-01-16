using AxibugProtobuf;
using ServerCore.Common;
using ServerCore.NetWork;
using System.Net.Sockets;

namespace ServerCore.Manager
{
    public class LoginManager
    {
        public LoginManager()
        {
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdLogin, UserLogin);
        }

        public void UserLogin(Socket _socket, byte[] reqData)
        {
            ServerManager.g_Log.Debug("收到新的登录请求");
            Protobuf_Login msg = ProtoBufHelper.DeSerizlize<Protobuf_Login>(reqData);
            ClientInfo cinfo = ServerManager.g_ClientMgr.JoinNewClient(msg, _socket);

            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_Login_RESP()
            {
                Status = LoginResultStatus.Ok,
                RegDate = "",
                LastLoginDate = "",
                Token = ""
            });

            ServerManager.g_ClientMgr.ClientSend(cinfo, (int)CommandID.CmdLogin, (int)ErrorCode.ErrorOk, respData);
        }
    }
}