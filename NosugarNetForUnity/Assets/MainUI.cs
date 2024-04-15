using NoSugarNet.ClientCoreNet4x;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : MonoBehaviour
{

    public Button btnStart;
    public InputField inputIP;
    public InputField inputPort;
    public Text textLog;

    // Start is called before the first frame update
    void Start()
    {
        btnStart.onClick.AddListener(InitNoSugarNetClient);
        textLog.text = "-";
        AddLog("123");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void InitNoSugarNetClient()
    {
        if (string.IsNullOrEmpty(inputIP.text))
        {
            OnNoSugarNetLog(0,"配置错误");
            return;
        }
        AppNoSugarNet.OnUpdateStatus += OnUpdateStatus;
        AppNoSugarNet.Init(inputIP.text, Convert.ToInt32(inputPort.text), OnNoSugarNetLog);
    }


    static void OnUpdateStatus(NetStatus netState)
    {
        string info = $"User:{netState.ClientUserCount} Tun:{netState.TunnelCount}      rec: {ConvertBytesToKilobytes(netState.srcReciveSecSpeed)}K/s|{ConvertBytesToKilobytes(netState.tReciveSecSpeed)}K/s        send: {ConvertBytesToKilobytes(netState.srcSendSecSpeed)}K/s|{ConvertBytesToKilobytes(netState.tSendSecSpeed)}K/s";
        OnNoSugarNetLog(0,info);
    }
    static string ConvertBytesToKilobytes(long bytes)
    {
        return Math.Round((double)bytes / 1024, 2).ToString("F2");
    }
    static void OnNoSugarNetLog(int LogLevel, string msg)
    {
        Debug.Log(msg);
        AddLog(msg);
    }

    static string logText = "";

    void OnGUI()
    {
        // 创建一个新的GUIStyle
        GUIStyle style = new GUIStyle();

        // 设置字体大小
        style.fontSize = 24;
        GUI.TextField(new Rect(10, 300, Screen.width - 20, 1000), logText, style);
    }

    static void AddLog(string msg)
    {
        // 添加新的日志信息到文本框
        logText += string.Format("{0}\n", System.DateTime.Now.ToString("HH:mm:ss> ") + msg);

        // 限制文本框显示的最大行数
        if (logText.Split('\n').Length > 50)
        {
            string[] lines = logText.Split('\n');
            logText = "";
            for (int i = lines.Length - 50; i < lines.Length; i++)
            {
                logText += lines[i] + "\n";
            }
        }
    }
}
