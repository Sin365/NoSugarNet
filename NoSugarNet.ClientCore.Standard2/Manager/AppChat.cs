using AxibugProtobuf;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore.Event;
using NoSugarNet.ClientCore.Network;

namespace NoSugarNet.ClientCore.Manager
{
    public class AppChat
    {
        public AppChat()
        {
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdChatmsg, RecvChatMsg);
        }

        public void SendChatMsg(string ChatMsg)
        {
            Protobuf_ChatMsg msg = new Protobuf_ChatMsg()
            {
                ChatMsg = ChatMsg,
            };
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdChatmsg, ProtoBufHelper.Serizlize(msg));
        }

        public void RecvChatMsg(byte[] reqData)
        {
            Protobuf_ChatMsg_RESP msg = ProtoBufHelper.DeSerizlize<Protobuf_ChatMsg_RESP>(reqData);
            EventSystem.Instance.PostEvent(EEvent.OnChatMsg, msg.NickName, msg.ChatMsg);
        }
    }
}
