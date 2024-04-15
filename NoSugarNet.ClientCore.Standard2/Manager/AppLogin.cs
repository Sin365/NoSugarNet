using AxibugProtobuf;
using NoSugarNet.ClientCoreNet.Standard2.Common;
using NoSugarNet.ClientCoreNet.Standard2.Network;

namespace NoSugarNet.ClientCoreNet.Standard2.Manager
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
            AppNoSugarNet.networkHelper.SendToServer((int)CommandID.CmdLogin, ProtoBufHelper.Serizlize(msg));
        }

        public void RecvLoginMsg(byte[] reqData)
        {
            Protobuf_Login_RESP msg = ProtoBufHelper.DeSerizlize<Protobuf_Login_RESP>(reqData);
            if (msg.Status == LoginResultStatus.Ok)
            {
                AppNoSugarNet.log.Info("登录成功");
                AppNoSugarNet.user.InitMainUserData(AppNoSugarNet.user.userdata.Account);
            }
            else
            {
                AppNoSugarNet.log.Info("登录失败");
            }
        }

    }
}
