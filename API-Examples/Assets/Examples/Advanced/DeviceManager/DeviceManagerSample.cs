using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class DeviceManagerSample : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("You need firstly create you app and fetch your APP_KEY.View detail to https://doc.yunxin.163.com/nertc/docs/TA0ODQ2NjI?platform=unity")]
        public string APP_KEY = "YOUR APP KEY";

        [SerializeField]
        [Tooltip("You need request a token in the safe mode or set null in the debugging mode.View detail to https://doc.yunxin.163.com/nertc/docs/jAzNzY2MDQ?platform=unity")]
        public string TOKEN = "";

        [SerializeField]
        [Tooltip("You need input your real channel name.e.g \"10000\"")]
        public string CHANNEL_NAME = "YOUR CHANNEL NAME";

        [SerializeField]
        [Tooltip("UID is optional. The default value is 0. If the uid is not specified (set to 0), the SDK automatically assigns a random uid and returns the uid in the callback of onJoinChannel.")]
        public ulong UID = 0;

        [Header("Log Output")]
        public Text _logText;

        [Header("Set Local Video Canvas")]
        public RawImage localVideoCanvas;

        [Header("Set Remote User Video Canvas")]
        public RawImage remoteVideoCanvas;

        [Header("Enumerate audio/video devices")]
        public Dropdown audioRecordDeviceList;
        public Dropdown audioPlaybackDeviceList;
        public Dropdown videoCaptureDeviceList;

        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();


        Dictionary<int, string> _audioRecordDevices = new Dictionary<int, string>();
        Dictionary<int, string> _audioPlaybackDevices = new Dictionary<int, string>();
        Dictionary<int, string> _videoCaptureDevices = new Dictionary<int, string>();

        void Start()
        {
            _logger = new Logger(_logText);
            PermissionHelper.RequestMicroPhonePermission();
            PermissionHelper.RequestCameraPermission();

            _logger.Log($"Start");

            //You should initialize Dispatcher on main thread.
            _ = Dispatcher.Current;

            //listen rtcEgine events
            BindEvent();

            //initialize rtcEngine
            if (InitRtcEngine())
            {
                JoinChannel();
            }
        }

        private bool InitRtcEngine()
        {
            var context = new RtcEngineContext();
            context.appKey = APP_KEY;
            context.logPath = Application.persistentDataPath;
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
            context.context = activity;
#endif
            int result = _rtcEngine.Initialize(context);
            if(result != (int)RtcErrorCode.kNERtcNoError)
            {
                _logger.LogWarning($"RtcEngine Initialize Failed : {result}");
                return false;
            }

            _logger.Log($"RtcEngine Initialize Success");

            //Enables local audio and local video capture.
            _rtcEngine.EnableLocalAudio(true);
            _rtcEngine.EnableLocalVideo(true);

            //Sets local views.This method is used to set the display information about the local video. The method is applicable for only local
            //users.Remote users are not affected.
            var canvas = new RtcVideoCanvas {
                callback = new VideoFrameCallback(OnTexture2DVideoFrame),
            };
            _rtcEngine.SetupLocalVideoCanvas(canvas);

            //Get all audio recording devcies
            GetAudioRecordDevices();

            //Get all audio play back devices
            GetAudioPlaybackDevices();

            //Get all video capture devices
            GetVideoCaptureDevices();

            //Preset the current device
            PresetDevices();
            return true;
        }

        private void BindEvent()
        {
            /* All events are not called on main thread.If you want to update UI，you should schedule a task to the main thread.
            * You can do it like this:
            * private void OnJoinChannelHandler(ulong cid, ulong uid, RtcErrorCode result, ulong elapsed) {
            *   Dispatcher.QueueOnMainThread(() => { 
            *       //update UI here
            *   });
            * }
            */
            _rtcEngine.OnJoinChannel = OnJoinChannelHandler;
            _rtcEngine.OnLeaveChannel = OnLeaveChannelHandler;
            _rtcEngine.OnUserJoined = OnUserJoinedHandler;
            _rtcEngine.OnUserLeft = OnUserLeftHandler;
            _rtcEngine.OnUserAudioStart = OnUserAudioStartHandler;
            _rtcEngine.OnUserAudioStop = OnUserAudioStopHandler;
            _rtcEngine.OnUserVideoStart = OnUserVideoStartHandler;
            _rtcEngine.OnUserVideoStop = OnUserVideoStopHandler;
            _rtcEngine.OnAudioDeviceRoutingDidChange = OnAudioDeviceRoutingDidChangeHandler;
            _rtcEngine.OnAudioDefaultDeviceChanged = OnAudioDefaultDeviceChangedHandler;
            _rtcEngine.OnAudioDeviceStateChanged = OnAudioDeviceStateChangedHandler;
            _rtcEngine.OnVideoDeviceStateChanged = OnVideoDeviceStateChangedHandler;
        }

        private void JoinChannel()
        {
            /* Joins a channel of audio and video call..If the specified room does not exist when you join the room, a room with the specified name is automatically created in
            * the server provided by CommsEase.token The certification signature used in authentication (NERTC Token). Valid values:
            *    - Null. You can set the value to null in the debugging mode. We recommend you change to the default safe
            *        mode before your product is officially launched.
            *    - NERTC Token acquired. In safe mode, the acquired token must be specified. If the specified token is
            *        invalid, users are unable to join a channel. We recommend that you use the safe mode.
            */
            int result = _rtcEngine.JoinChannel(TOKEN, CHANNEL_NAME, UID);
            if (result != (int)RtcErrorCode.kNERtcNoError) 
            {
                //if failure
                _logger.LogWarning($"RtcEngine JoinChannel Failed : {result}");
                return;
            }

            _logger.LogWarning($"RtcEngine JoinChannel result : {result}");
        }

        private void GetAudioRecordDevices()
        {
            audioRecordDeviceList.ClearOptions();
            var collection = _rtcEngine.AudioDeviceManager.EnumerateRecordDevices();
            if(collection == null)
            {
                _logger.LogWarning($"Not find audio record device!");
                return;
            }

            var list = new List<string>();
            for(int idx = 0; idx < collection.GetCount(); idx++)
            {
                string deviceId = null;
                string deviceName = null;
                int result = collection.GetDevice((ushort)idx, out deviceName, out deviceId);
                if(result != (int)RtcErrorCode.kNERtcNoError)
                {
                    continue;
                }

                _audioRecordDevices[idx] = deviceId;
                list.Add($"{idx}|{deviceName}");
            }

            audioRecordDeviceList.AddOptions(list);
        }

        private void GetAudioPlaybackDevices()
        {
            audioPlaybackDeviceList.ClearOptions();
            var collection = _rtcEngine.AudioDeviceManager.EnumeratePlayoutDevices();
            if (collection == null)
            {
                _logger.LogWarning($"Not find audio record device!");
                return;
            }

            var list = new List<string>();
            for (int idx = 0; idx < collection.GetCount(); idx++)
            {
                string deviceId = null;
                string deviceName = null;
                int result = collection.GetDevice((ushort)idx, out deviceName, out deviceId);
                if (result != (int)RtcErrorCode.kNERtcNoError)
                {
                    continue;
                }

                _audioPlaybackDevices[idx] = deviceId;
                list.Add($"{idx}|{deviceName}");
            }

            audioPlaybackDeviceList.AddOptions(list);
        }

        private void GetVideoCaptureDevices()
        {
            videoCaptureDeviceList.ClearOptions();
            var collection = _rtcEngine.VideoDeviceManager.EnumerateCaptureDevices();
            if (collection == null)
            {
                _logger.LogWarning($"Not find audio record device!");
                return;
            }

            var list = new List<string>();
            for (int idx = 0; idx < collection.GetCount(); idx++)
            {
                string deviceId = null;
                string deviceName = null;
                int result = collection.GetDevice((ushort)idx, out deviceName, out deviceId);
                if (result != (int)RtcErrorCode.kNERtcNoError)
                {
                    continue;
                }

                _videoCaptureDevices[idx] = deviceId;
                list.Add($"{idx}|{deviceName}");
            }

            videoCaptureDeviceList.AddOptions(list);
        }

        private void PresetDevices()
        {
            //Select the first device by default.Of course,you can select others.
            if (_audioRecordDevices.Count > 0)
            {
                _rtcEngine.AudioDeviceManager.SetRecordDevice(_audioRecordDevices[0]);
            }

            if (_audioPlaybackDevices.Count > 0)
            {
                _rtcEngine.AudioDeviceManager.SetPlayoutDevice(_audioPlaybackDevices[0]);
            }

            if (_videoCaptureDevices.Count > 0)
            {
                _rtcEngine.VideoDeviceManager.SetDevice(_videoCaptureDevices[0]);
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void OnJoinChannelClicked()
        {
            JoinChannel();
        }

        public void OnLeaveChannelClicked()
        {
            int result = _rtcEngine.LeaveChannel();
            _logger.LogWarning($"RtcEngine LeaveChannel result : {result}");
        }

        public void OnDropDownAudioRecordDeviceListValueChanged(int value)
        {
            _logger.Log($"OnDropDownAudioRecordDeviceListValueChanged {value}");

            string deviceId = null;
            _audioRecordDevices.TryGetValue(value, out deviceId);
            if (string.IsNullOrEmpty(deviceId))
            {
                _logger.Log($"No device exist!");
                return;
            }

            _rtcEngine.AudioDeviceManager.SetRecordDevice(deviceId);
        }

        public void OnDropDownAudioPlaybackDeviceListValueChanged(int value)
        {
            _logger.Log($"OnDropDownAudioPlaybackDeviceListValueChanged {value}");

            string deviceId = null;
            _audioPlaybackDevices.TryGetValue(value, out deviceId);
            if (string.IsNullOrEmpty(deviceId))
            {
                _logger.Log($"No device exist!");
                return;
            }

            _rtcEngine.AudioDeviceManager.SetPlayoutDevice(deviceId);
        }

        public void OnDropDownVideoCaptureDeviceListValueChanged(int value)
        {
            _logger.Log($"OnDropDownVideoCaptureDeviceListValueChanged {value}");

            string deviceId = null;
            _videoCaptureDevices.TryGetValue(value, out deviceId);
            if (string.IsNullOrEmpty(deviceId))
            {
                _logger.Log($"No device exist!");
                return;
            }

            _rtcEngine.VideoDeviceManager.SetDevice(deviceId);
        }

        #region Engine Events
        private void OnJoinChannelHandler(ulong cid, ulong uid, RtcErrorCode result, ulong elapsed)
        {
            _logger.Log($"OnJoinChannel cid - {cid}, uid- {uid},result - {result}, elapsed - {elapsed}");
        }
        private void OnLeaveChannelHandler(RtcErrorCode result)
        {
            _logger.Log($"OnLeaveChannel result - {result}");
        }
        private void OnUserJoinedHandler(ulong uid, string userName)
        {
            _logger.Log($"OnUserJoined uid - {uid},userName - {userName}");
        }
        private void OnUserLeftHandler(ulong uid, RtcSessionLeaveReason reason)
        {
            _logger.Log($"OnUserLeft uid - {uid},reason - {reason}");

            //remove video canvas after user left
            _rtcEngine.SetupRemoteVideoCanvas(uid, null);
        }
        private void OnUserAudioStartHandler(ulong uid)
        {
            _logger.Log($"OnUserAudioStart uid - {uid}");
        }
        private void OnUserAudioStopHandler(ulong uid)
        {
            _logger.Log($"OnUserAudioStop uid - {uid}");
        }
        private void OnUserVideoStartHandler(ulong uid, RtcVideoProfileType maxProfile)
        {
            _logger.Log($"OnUserVideoStart uid - {uid},maxProfile - {maxProfile}");

            //You should set remote user canvas firstly and subscribe user video stream if need retrieve video stream of the remote user .
            var canvas = new RtcVideoCanvas
            {
                callback = new VideoFrameCallback(OnTexture2DVideoFrame),
            };
            _rtcEngine.SetupRemoteVideoCanvas(uid, canvas);
            _rtcEngine.SubscribeRemoteVideoStream(uid, RtcRemoteVideoStreamType.kNERtcRemoteVideoStreamTypeHigh, true);
        }

        private void OnUserVideoStopHandler(ulong uid)
        {
            _logger.Log($"OnUserVideoStop uid - {uid}");
        }

        public void OnTexture2DVideoFrame(ulong uid, Texture2D texture, RtcVideoRotation rotation)
        {
            if (uid == 0)
            {
                if(localVideoCanvas != null)
                {
                    localVideoCanvas.texture = texture;
                    localVideoCanvas.transform.localRotation = Quaternion.Euler(0, 0, -(float)rotation);
                }
                return;
            }

            if(remoteVideoCanvas != null)
            {
                remoteVideoCanvas.texture = texture;
                remoteVideoCanvas.transform.localRotation = Quaternion.Euler(0, 0, -(float)rotation);
            }
        }

        private void OnAudioDeviceRoutingDidChangeHandler(RtcAudioOutputRouting routing)
        {
            _logger.Log($"OnAudioDeviceRoutingDidChangeHandler routing - {routing}");
        }

        private void OnAudioDefaultDeviceChangedHandler(string deviceId, RtcAudioDeviceType deviceType)
        {
            _logger.Log($"OnAudioDefaultDeviceChangedHandler deviceId - {deviceId},deviceType - {deviceType}");
        }
        private void OnAudioDeviceStateChangedHandler(string deviceId, RtcAudioDeviceType deviceType, RtcAudioDeviceState deviceState)
        {
            _logger.Log($"OnAudioDeviceStateChangedHandler deviceId - {deviceId},deviceType - {deviceType}, deviceState - {deviceState}");
            //TODO:You should update your device list when the event is triggered.
        }

        private void OnVideoDeviceStateChangedHandler(string deviceId, RtcVideoDeviceType deviceType, RtcVideoDeviceState deviceState)
        {
            _logger.Log($"OnVideoDeviceStateChangedHandler deviceId - {deviceId},deviceType - {deviceType}, deviceState - {deviceState}");
            //TODO:You should update your device list when the event is triggered.
        }
        #endregion

        public void OnApplicationQuit()
        {
            _logger.Log("OnApplicationQuit");

            //you must release engine object when the app will be quit.
            //If you need use IRtcEngine again after release ,you can be Initialize again.
            //In this,you need call leave channel and Release engine resources.
            _rtcEngine.LeaveChannel();
            _rtcEngine.Release(true);
        }
    }
}
