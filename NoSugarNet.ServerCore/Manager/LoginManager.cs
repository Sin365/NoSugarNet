﻿using AxibugProtobuf;
using NoSugarNet.ServerCore.Common;
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
            byte[] keys = Config.Cfgs.Keys.ToArray();
            for (int i = 0; i < Config.Cfgs.Count; i++) 
            {
                TunnelClientData cfg = Config.Cfgs[keys[i]];
                cfgsSP.Cfgs.Add(new Protobuf_Cfgs_Single() { TunnelID = cfg.TunnelId, Port = cfg.ClientLocalPort });
            }

            byte[] respDataCfg = ProtoBufHelper.Serizlize(cfgsSP);
            ServerManager.g_ClientMgr.ClientSend(cinfo, (int)CommandID.CmdCfgs, (int)ErrorCode.ErrorOk, respDataCfg);
        }
    }
}