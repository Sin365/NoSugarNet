## NoSugarNet

NoSugarNet（无糖网络），一个有效降低任意C/S程序（软件，程序）网络流量传输的网络中间工具。

### 有意义的应用场景

1. 省流需求

	1.1 当您使用按量付费租赁的服务器，客户端本地和服务端代理是压缩通讯可以有效减少通讯数据量。

	1.2 业务程序本身数据压缩率很差或无压缩时，可以大量剩流，同上。
	
	1.3 业务程序包含大量重复数据包的情况。NoSuarNet会标记重复数据，通讯时，仅发送一个数据标识(仅2个字节)，接受端重播。大幅度减少不必要的数据量。
	
2. 突破端口数量限制

	在限制端口的服务器上。只给你开放少量两三个端口的供应商。可以占用一个端口，实际访问无数个(65535个)端口。解决端口限制带来的问题。
	
	例如：个别Minecraft类服务器提供商，往往只提供2~5个端口（远程桌面还占一个）但配置还不错，可以开更多游戏服务端，但是远程桌面，管理，或各种游戏插件诸如网页地图插件等，占用之后。端口就不够用了。
	
	使用NoSugarNet,您就可以突破这个限制。
	
3. 您的程序集成需求

	您可以在你的网关程序，客户端登陆器，代理程序或传输业务层接入NoSugarNet。
	
	本项目除提供轻量控制台直接使用之外，核心逻辑也是DLL程序集，可以接入到您的项目中。


本项目使用，我自构建的HaoYueNet高性能网络库作为基础而开发

[HaoYueNet-Github](https://github.com/Sin365/HaoYueNet "HaoYueNet-Github")

[HaoYueNet-自建Git站点](http://git.axibug.com/sin365/HaoYueNet "HaoYueNet-自建Git站点")

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
{
  "ServerPort": 1000,
  "CompressAdapterType": 1,
  "TunnelList": [
    {
      "ServerLocalTargetIP": "1.2.3.4",
      "ServerLocalTargetPort": 3389,
      "ClientLocalPort": 13389
    },
    {
      "ServerLocalTargetIP": "1.2.3.4",
      "ServerLocalTargetPort": 3306,
      "ClientLocalPort": 13306
    }
  ]
}
```

表示

本代理服务，可以连接代理访问，1.2.3.4:3389 和 1.2.3.5:3306 

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


