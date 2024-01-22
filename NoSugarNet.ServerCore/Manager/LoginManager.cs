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

            Protobuf_Cfgs cfgsSP = new Protobuf_Cfgs();
            cfgsSP.Cfgs.Add(new Protobuf_Cfgs_Single { TunnelID = 0, IP = "127.0.0.1", Port = 10001 });
            cfgsSP.Cfgs.Add(new Protobuf_Cfgs_Single { TunnelID = 1, IP = "127.0.0.1", Port = 10002 });
            cfgsSP.Cfgs.Add(new Protobuf_Cfgs_Single { TunnelID = 2, IP = "127.0.0.1", Port = 10003 });
            cfgsSP.Cfgs.Add(new Protobuf_Cfgs_Single { TunnelID = 3, IP = "127.0.0.1", Port = 10004 });

            byte[] respDataCfg = ProtoBufHelper.Serizlize(cfgsSP);
            ServerManager.g_ClientMgr.ClientSend(cinfo, (int)CommandID.CmdCfgs, (int)ErrorCode.ErrorOk, respDataCfg);
        }
    }
}