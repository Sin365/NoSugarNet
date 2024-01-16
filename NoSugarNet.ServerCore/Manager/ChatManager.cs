﻿using AxibugProtobuf;
using ServerCore.Common;
using ServerCore.NetWork;
using System.Net.Sockets;

namespace ServerCore.Manager
{
    public class ChatManager
    {
        public ChatManager()
        {
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdChatmsg, RecvPlayerChatMsg);
        }

        public void RecvPlayerChatMsg(Socket sk, byte[] reqData)
        {
            ClientInfo _c = ServerManager.g_ClientMgr.GetClientForSocket(sk);
            ServerManager.g_Log.Debug("收到聊天消息请求");
            Protobuf_ChatMsg msg = ProtoBufHelper.DeSerizlize<Protobuf_ChatMsg>(reqData);
            byte[] respData = ProtoBufHelper.Serizlize(new Protobuf_ChatMsg_RESP()
            {
                ChatMsg = msg.ChatMsg,
                NickName = _c.Account,
                Date = Helper.GetNowTimeStamp()
            });
            ServerManager.g_ClientMgr.ClientSendALL((int)CommandID.CmdChatmsg, (int)ErrorCode.ErrorOk, respData);
        }
    }
}