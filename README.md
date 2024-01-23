## NoSugarNet

NoSugarNet（无糖网络），一个有效降低任意C/S程序（软件，程序）网络流量传输的网络中间工具。

本项目使用，我自构建的HaoYueNet高性能网络库作为基础而开发
`->[HaoYueNet-Github](https://github.com/Sin365/HaoYueNet)`
`->[HaoYueNet-自建Git站点](http://git.axibug.com/sin365/HaoYueNet)`

## 流程诠释：

【需要代理的程序】<----Localhost本地通讯----->【NoSugarNet.Client】 <----代理通讯-----> 【NoSugarNet.ServerCli】<----服务端本地-----> 【目标服务端程序】

————————————————————

## 服务端 NoSugarNet.ServerCli

其中config.cfg 是配置文件

格式：

```
  <配置编号>:<目标服务端IP>:<目标服务器端口>:<客户端监听的端口>
```

每个配置换行

形如

```
0:1.2.3.4:3389:10001
1:1.2.3.5:3389:10002
```

表示

本代理服务，可以连接代理访问，1.2.3.4:3389 和 1.2.3.5:3389 

配置编号和客户端安排端口会发送到给客户端。

客户端连接服务器获取基本信息后，客户端会开始监听10001和10002

————————————————————

## 客户端 NoSugarNet.ClientCli

其中config.cfg 是配置文件

格式：

```
<代理服务端IP>:<代理服务端Port>
```

*代理端口IP可以改，端口哦固定为1000，因为服务器中转端口写死的1000（笑）


