using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class ScreenShareOnDesktopSample : MonoBehaviour
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

        [Header("Set local or remote user canvas")]
        public RawImage localSubstreamVideoCanvas;
        public RawImage remoteSubstreamVideoCanvas;

        [Header("Set the drop list for windows or displays")]
        public Dropdown windowList;
        public Dropdown dispalyList;
        public Toggle shareWindow;

        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private Dictionary<uint, ScreenShareHelper.MONITORINFO> _displayInfos = new Dictionary<uint, ScreenShareHelper.MONITORINFO>();
#endif

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

            PrepareShareShare();
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

            //Sets local views.This method is used to set the display information about the local video. The method is applicable for only local
            //users.Remote users are not affected.
            var canvas = new RtcVideoCanvas {
                callback = new VideoFrameCallback(OnTexture2DSubstreamVideoFrame),
            };
            _rtcEngine.SetupLocalVideoCanvas(RtcVideoStreamType.kNERTCVideoStreamSub, canvas);
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
            _rtcEngine.OnScreenCaptureStatusChanged = OnScreenCaptureStatusChangedHandler;
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

        private void PrepareShareShare()
        {
            windowList.ClearOptions();
            dispalyList.ClearOptions();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _displayInfos.Clear();
            var windowInfoList = ScreenShareHelper.GetDesktopWindowInfo();
            if(windowList != null)
            {
                windowList.AddOptions(windowInfoList.Select(w => new Dropdown.OptionData($"{w.Value}|{w.Key}")).ToList());
            }

            var displayInfoList = ScreenShareHelper.GetWinDisplayInfo();
            foreach(var d in displayInfoList)
            {
                _displayInfos.Add(d.MonitorInfo.flags, d.MonitorInfo);
            }
            dispalyList.AddOptions(displayInfoList.Select(di=>new Dropdown.OptionData($"Display:{di.MonitorInfo.flags}")).ToList());
#endif
        }

        private void StartScreenShareByDisplay()
        {
            _logger.Log($"StartScreenShareByDisplay");

            if(dispalyList.options.Count == 0)
            {
                return;
            }
            var option = dispalyList.options[dispalyList.value].text;
            var displayId = System.Convert.ToUInt32(option.Replace("Display:", ""));

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var mi = _displayInfos[displayId];

            //Before you want to share you screen,you should setup lcoal view.
            //And you need set the region of the monitor.
            var screenRect = new RtcRectangle
            {
                x = mi.monitor.top,
                y = mi.monitor.left,
                width = mi.monitor.right - mi.monitor.left,
                height = mi.monitor.bottom - mi.monitor.top
            };

            //Set the region you want to share
            var regionRect = new RtcRectangle
            {
                x = 0,
                y = 0,
                width = 0,
                height = 0
            };
            //set other parameters
            var screenParams = new RtcScreenCaptureParameters
            {
                profile = RtcScreenProfileType.kNERtcScreenProfileCustom,
                dimensions = new RtcVideoDimensions { width = 1920, height = 1080 },
                frameRate = 10,
                bitrate = 0,
                captureMouseCursor = true,
                windowFocus = true,
                prefer = RtcSubStreamContentPrefer.kNERtcSubStreamContentPreferDetails,
            };

            int result = _rtcEngine.StartScreenCaptureByScreenRect(screenRect, regionRect, screenParams);
            if (result != (int)RtcErrorCode.kNERtcNoError)
            {
                //failure
                _logger.LogError($"StartScreenCaptureByScreenRect failed : {result}");
            }
#endif
        }

        private void StartScreenShareByWindow()
        {
            _logger.Log($"StartScreenShareByWindow");
            if (windowList.options.Count == 0)
            {
                return;
            }
            var option = windowList.options[windowList.value].text;
            var winId = System.Convert.ToInt64(option.Split('|').ElementAt(0));
            IntPtr windowId = new IntPtr(winId);

            //Set the region you want to share
            var regionRect = new RtcRectangle
            {
                x = 0,
                y = 0,
                width = 0,
                height = 0
            };
            //set other parameters
            var screenParams = new RtcScreenCaptureParameters
            {
                profile = RtcScreenProfileType.kNERtcScreenProfileCustom,
                dimensions = new RtcVideoDimensions { width = 1920, height = 1080 },
                frameRate = 10,
                bitrate = 0,
                captureMouseCursor = true,
                windowFocus = true,
                prefer = RtcSubStreamContentPrefer.kNERtcSubStreamContentPreferDetails,
            };

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            int result = _rtcEngine.StartScreenCaptureByWindowId(windowId, regionRect, screenParams);
            if (result != (int)RtcErrorCode.kNERtcNoError)
            {
                //failure
                _logger.LogError($"StartScreenCaptureByWindowId failed : {result}");
            }
#endif
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void OnStartScreenShareClicked()
        {
            OnStopScreenShareClicked();
            if (shareWindow.isOn)
            {
                StartScreenShareByWindow();
                return;
            }
            StartScreenShareByDisplay();
        }

        public void OnPauseScreenShareClicked()
        {
            int result = _rtcEngine.PauseScreenCapture();
            _logger.LogWarning($"RtcEngine PauseScreenCapture result : {result}");
        }

        public void OnResumeScreenShareClicked()
        {
            int result = _rtcEngine.ResumeScreenCapture();
            _logger.LogWarning($"RtcEngine ResumeScreenCapture result : {result}");
        }

        public void OnStopScreenShareClicked()
        {
            int result = _rtcEngine.StopScreenCapture();
            _logger.LogWarning($"RtcEngine StopScreenCapture result : {result}");
        }

        public void OnDropDownWindowListValueChanged(int value)
        {
            OnStartScreenShareClicked();
        }
        public void OnDropDownDisplayInfoListValueChanged(int value)
        {
            OnStartScreenShareClicked();
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
        private void OnUserJoinedHandler(ulong uid, string userName, RtcUserJoinExtraInfo customInfo)
        {
            _logger.Log($"OnUserJoined uid - {uid},userName - {userName}");
        }
        private void OnUserLeftHandler(ulong uid, RtcSessionLeaveReason reason, RtcUserJoinExtraInfo customInfo)
        {
            _logger.Log($"OnUserLeft uid - {uid},reason - {reason}");

            //remove video canvas after user left
            _rtcEngine.SetupRemoteVideoCanvas(uid, RtcVideoStreamType.kNERTCVideoStreamSub, null);
        }
        private void OnUserAudioStartHandler(RtcAudioStreamType type, ulong uid)
        {
            _logger.Log($"OnUserAudioStart type - {type} ,uid - {uid}");
        }
        private void OnUserAudioStopHandler(RtcAudioStreamType type, ulong uid)
        {
            _logger.Log($"OnUserAudioStop type - {type} ,uid - {uid}");
        }
        private void OnUserVideoStartHandler(RtcVideoStreamType type, ulong uid, RtcVideoProfileType maxProfile)
        {
            _logger.Log($"OnUserVideoStart uid - {uid}, type - {type} ,maxProfile - {maxProfile}");

            //You should set remote user canvas firstly and subscribe user video stream if need retrieve video stream of the remote user .
            var canvas = new RtcVideoCanvas
            {
                callback = new VideoFrameCallback(OnTexture2DSubstreamVideoFrame),
            };
            _rtcEngine.SetupRemoteVideoCanvas(uid, type, canvas);
            _rtcEngine.SubscribeRemoteVideoStream(uid, type, RtcRemoteVideoStreamType.kNERtcRemoteVideoStreamTypeHigh, true);
        }

        private void OnUserVideoStopHandler(RtcVideoStreamType type, ulong uid)
        {
            _logger.Log($"OnUserVideoStop type - {type} ,uid - {uid}");
        }

        private void OnScreenCaptureStatusChangedHandler(RtcScreenCaptureStatus status)
        {
            _logger.Log($"OnScreenCaptureStatusChanged status - {status}");
        }

        public void OnTexture2DSubstreamVideoFrame(ulong uid, Texture2D texture, RtcVideoRotation rotation)
        {
            if (uid == 0)
            {
                if(localSubstreamVideoCanvas != null)
                {
                    localSubstreamVideoCanvas.texture = texture;
                    localSubstreamVideoCanvas.transform.localRotation = Quaternion.Euler(0, 0, -(float)rotation);
                }
                return;
            }

            if(remoteSubstreamVideoCanvas != null)
            {
                remoteSubstreamVideoCanvas.texture = texture;
                remoteSubstreamVideoCanvas.transform.localRotation = Quaternion.Euler(0, 0, -(float)rotation);
            }
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
