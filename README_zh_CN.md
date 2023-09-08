## 简介

_Other Languages: [English](README.md)_

本开源示例项目演示了不同场景下，`Unity RTC SDK` 的基本集成逻辑。 项目中每个示例的`MainScene`都是一个独立的场景，可以独立运行。

## 目录结构

```

├─ API-Examples // RTC API Examples，包括基本的登录、消息收发、聊天室等
│  ├─ Examples  // 所有的示例
│  │  ├─ Basic                 		// 演示 基本功能的示例代码
│  │  │  ├─ JoinChannel        		// 演示 RTC 加入音视频房间的示例代码
│  │  │  ├─ JoinAudioChannel   		// 演示 RTC 加入多个音频房间的的示例代码
│  │  │  ├─ JoinMultiChannel   		// 演示 RTC 加入多个音视频房间的的示例代码
│  │  │  │
│  │  ├─ Advanced              		// 演示 高级功能的示例代码
│  │  │  ├─ 3DAudio            		// 演示 空间音效的示例代码
│  │  │  ├─ MultiVideoChat     		// 演示 多人音视频聊天的示例代码
│  │  │  ├─ AudioVolumeIndication       // 演示 音量显示的示例代码
│  │  │  ├─ CustomAudioRender     	// 演示 自定义音频渲染的示例代码
│  │  │  ├─ CustomAudioInput     	// 演示 自定义音频输入的示例代码
│  │  │  ├─ CustomVideoInput     	// 演示 自定义视频输入的示例代码
│  │  │  ├─ DeviceManager     		// 演示 音视频设备管理的示例代码
│  │  │  ├─ LiveStreaming     		// 演示 旁路直播的示例代码
│  │  │  ├─ ScreenShareOnDesktop        // 演示 桌面端共享屏幕的示例代码
│  │  │
├─ ├─ Utils    // 工具类
├─ ├─ Editor    // 编辑器设置目录
│  │  ├─ Builder.cs            // 演示各平台所需要的的编译设置
├─ ├─ Plugins  // 插件文件夹
│  │  ├─ Android               // Android平台
│  │  │  ├─ AndroidManifest.xml
```

## 运行示例项目

### 开发环境要求

在开始运行示例项目之前，请确保开发环境满足以下要求：

| 环境要求 | 说明 |
|--------|--------|
| Unity Editor 版本 | 2019.4.30f1及以上版本 |

### 运行示例项目

1. [**创建应用并获取`App Key`**](https://doc.yunxin.163.com/nertc/docs/DE3NDM0NTI?platform=unity) 。开通必要的功能，如音视频功能等。

2. [**下载Unity RTC SDK**](https://yx-web-nosdn.netease.im/package/1662715423977/nertc-unity-sdk-4.5.907.7z?download=nertc-unity-sdk-4.5.907.7z) 之后，按下述方式集成导入SDK文件，详情可参考开发文档集成。
	1. 把下载到的 SDK 文件`com.netease.game.rtc-*.*.*.tgz`放到`Packages`目录。
	2. 打开`Unity Editor`的`Package Manager`，单击左上角`“+”`图标，单击`"Add Package from tarball..."`，选中`Packages`目录下的`com.netease.game.rtc-*.*.*.tgz`文件，即可完成导入。

3. 导入SDK之后，选择想要运行的场景`MainScene`，点击`Canvas`，给场景绑定的脚本组件填入`APP KEY`、`TOKEN`、`CHANNEL_NAME`、`UID`以及其他必要的信息之后，然后运行程序。

4. 一切就绪之后，你可以参考示例代码的实现，体验SDK功能。



## 联系我们

- [网易云信文档中心](https://doc.yunxin.163.com/DeveloperContest/docs/zAwNTQ0Nzg?platform=unity)
- [API参考](https://doc.yunxin.163.com/docs/interface/NERTC_SDK/V4.5.907/Unity/html/)
- [知识库](https://faq.yunxin.163.com/kb/main/#/)
	 [提交工单](https://app.yunxin.163.com/index#/issue/submit)	
