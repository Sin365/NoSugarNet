using AxibugProtobuf;
using NoSugarNet.ClientCore.Common;
using NoSugarNet.ClientCore.Network;

namespace NoSugarNet.ClientCore.Manager
{
    public class AppLogin
    {
        static string LastLoginGuid = "";
        public AppLogin()
        {
            NetMsg.Instance.RegNetMsgEvent((int)CommandID.CmdLogin, RecvLoginMsg);
        }

        public void Login()
        {
            AppNoSugarNet.log.Debug("-->Login");
            if(string.IsNullOrEmpty(LastLoginGuid))
                LastLoginGuid = Guid.NewGuid().ToString();

            AppNoSugarNet.user.userdata.Account = LastLoginGuid;
            Protobuf_Login msg = new Protobuf_Login()
            {
                LoginType = 0,
                Account = AppNoSugarNet.user.userdata.Account,
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
