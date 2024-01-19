using AxibugProtobuf;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore.Network;

namespace NoSugarNet.ClientCore.Manager
{
    public class AppLogin
    {
        public AppLogin()
        {
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdLogin, RecvLoginMsg);
        }
        public void Login(string Account)
        {
            Protobuf_Login msg = new Protobuf_Login()
            {
                LoginType = 0,
                Account = Account,
            };
            App.networkHelper.SendToServer((int)CommandID.CmdLogin, ProtoBufHelper.Serizlize(msg));
        }

        public void RecvLoginMsg(byte[] reqData)
        {
            Protobuf_Login_RESP msg = ProtoBufHelper.DeSerizlize<Protobuf_Login_RESP>(reqData);
            if (msg.Status == LoginResultStatus.Ok)
            {
                App.log.Debug("登录成功");
                App.user.InitMainUserData(App.user.userdata.Account);
            }
            else
            {
                App.log.Debug("登录失败");
            }
        }

    }
}
