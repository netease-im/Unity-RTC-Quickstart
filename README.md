## Overview

_Other Languages: [简体中文](README_zh_CN.md)_

The open source project shows the basic integration logic of `Unity RTC SDK` in different scenes. Each `MainScene` in the example is independent.

## Folder structure

```

├─ API-Examples // RTC API Examples, basic audio and video call, joining multiple rooms, and spatial sound.
│  ├─ Examples  // Examples
│  │  ├─ Basic                 		// Basic features
│  │  │  ├─ JoinChannel        		// Sample code for joining an RTC channel
│  │  │  ├─ JoinAudioChannel   		// Sample code for joining an audio channel
│  │  │  ├─ JoinMultiChannel   		// Sample code for joining multiple RTC channels
│  │  │  │
│  │  ├─ Advanced              		// Advanced features
│  │  │  ├─ 3DAudio            		// Sample code for spatial sound
│  │  │  ├─ MultiVideoChat     		// Sample code for group audio and video call
│  │  │  ├─ AudioVolumeIndication       // Sample code for audio volume indication
│  │  │  ├─ CustomAudioRender     	// Sample code for the external audio render
│  │  │  ├─ CustomAudioInput     	// Sample code for the external audio input
│  │  │  ├─ CustomVideoInput     	// Sample code for the external video input
│  │  │  ├─ DeviceManager     		// Sample code for managering audio/video devices
│  │  │  ├─ LiveStreaming     		// Sample code for live-streaming
│  │  │  ├─ ScreenShareOnDesktop        // Sample code for sharing screens on the desktop
│  │  │
├─ ├─ Utils    // Utilities
├─ ├─ Editor    // Editor settings
│  │  ├─ Builder.cs            // Builder settings for platforms
├─ ├─ Plugins  // Plugin folder
│  │  ├─ Android               // Android platform
│  │  │  ├─ AndroidManifest.xml
```

## Run the demo project

### Development environment requirements

Before starting the demo project, make sure your development environment meets the following requirements:

| Environment | Description |
|--------|--------|
| Unity Editor | 2019.4.30f1 or later |

### Run the demo app

1. [**Create a project and get `App Key`**](https://doc.yunxin.163.com/nertc/docs/DE3NDM0NTI?platform=unity). Activate required services, such as Audio & Video Call.

2. [**Download the Unity RTC SDK**](https://yx-web-nosdn.netease.im/package/1662715423977/nertc-unity-sdk-4.5.907.7z?download=nertc-unity-sdk-4.5.907.7z). Integrate the SDK following the procedure below. For more information, see the Integrate SDK for Unity.
	1. Move `com.netease.game.rtc-*.*.*.tgz` in the SDK to the `Packages` folder.
	2. Open `Package Manager` in the `Unity Editor`. Click`"+"` icon. Click `"Add Package from tarball..."` and select and import `com.netease.game.rtc-*.*.*.tgz` in the `Packages` folder.

3. After you import the SDK, select a `MainScene` you want to run and click `Canvas`. Specify `APP KEY`, `TOKEN`, `CHANNEL_NAME`, `UID`, and other required information in the script bound to the scene, then run the app.

4. To build specific features, you can refer to the sample code in the SDK.

## Contact us

- [CommsEase Documentation](https://doc.commsease.com/en/DeveloperContest/docs/home-page?platform=undefined)
- [API Reference](https://doc.commsease.com/docs/interface/NERTC_SDK/en/Latest/Unity/index.html)

