using AxibugProtobuf;
using NoSugarNet.ClientCoreNet.Standard2.Common;
using NoSugarNet.ClientCoreNet.Standard2.Event;
using NoSugarNet.ClientCoreNet.Standard2.Network;

namespace NoSugarNet.ClientCoreNet.Standard2.Manager
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
