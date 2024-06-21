﻿using AxibugProtobuf;
using Google.Protobuf;
using HaoYueNet.ClientNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoSugarNet.ClientCore.Network
{
    /// <summary>
    /// 继承网络库，以支持网络功能
    /// </summary>
    public class NetworkHelper : NetworkHelperCore
    {
        public NetworkHelper()
        {
            //指定接收服务器数据事件
            OnReceiveData += GetDataCallBack;
            //断开连接
            OnClose += OnConnectClose;
            OnConnected += NetworkConnected;
            //网络库调试信息输出事件，用于打印网络内容
            OnLogOut += NetworkDeBugLog;
        }

        public delegate void OnReConnectedHandler();
        /// <summary>
        /// 重连成功事件
        /// </summary>
        public event OnReConnectedHandler OnReConnected;
        /// <summary>
        /// 是否自动重连
        /// </summary>
        public bool bAutoReConnect = true;
        /// <summary>
        /// 重连尝试时间
        /// </summary>
        const int ReConnectTryTime = 1000;

        public void NetworkConnected(bool IsConnect)
        {
            NetworkDeBugLog($"NetworkConnected:{IsConnect}");
            if (IsConnect)
            {
                //从未登录过
                if (!AppNoSugarNet.user.IsLoggedIn)
                {
                    //首次登录
                    AppNoSugarNet.login.Login();
                }
            }
            else
            {
                //连接失败
                NetworkDeBugLog("连接失败！");

                //停止所有
                AppNoSugarNet.local.StopAll();

                //自动重连开关
                if (bAutoReConnect)
                    ReConnect();
            }
        }

        public void NetworkDeBugLog(string str)
        {
            //用于Unity内的输出
            //Debug.Log("NetCoreDebug >> "+str);
            AppNoSugarNet.log.Info("NetCoreDebug >> " + str);
        }

        /// <summary>
        /// 接受包回调
        /// </summary>
        /// <param name="CMDID">协议ID</param>
        /// <param name="ERRCODE">错误编号</param>
        /// <param name="data">业务数据</param>
        public void GetDataCallBack(int CMDID, int ERRCODE, byte[] data)
        {
            //NetworkDeBugLog("收到消息 CMDID =>" + CMDID + " ERRCODE =>" + ERRCODE + " 数据长度=>" + data.Length);
            try
            {
                //抛出网络数据
                NetMsg.Instance.PostNetMsgEvent(CMDID, data);
            }
            catch (Exception ex)
            {
                NetworkDeBugLog("逻辑处理错误：" + ex.ToString());
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void OnConnectClose()
        {
            NetworkDeBugLog("OnConnectClose");

            //停止所有
            AppNoSugarNet.local.StopAll();

            //自动重连开关
            if (bAutoReConnect)
                ReConnect();
        }


        bool bInReConnecting = false;
        /// <summary>
        /// 自动重连
        /// </summary>
        void ReConnect()
        {
            if (bInReConnecting)
                return;
            bInReConnecting = true;

            bool bflagDone = false;
            do
            {
                //等待时间
                Thread.Sleep(ReConnectTryTime);
                AppNoSugarNet.log.Info($"尝试自动重连{LastConnectIP}:{LastConnectPort}……");
                //第一步
                if (Init(LastConnectIP, LastConnectPort))
                {
                    AppNoSugarNet.log.Info($"自动重连成功!");
                    bflagDone = true;
                    AppNoSugarNet.log.Info($"触发重连后的自动逻辑!");
                    OnReConnected?.Invoke();
                }
            } while (!bflagDone);
            bInReConnecting = false;
        }
    }
}
