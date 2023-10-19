using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class CustomVideoInputSample : MonoBehaviour
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

        [SerializeField]
        [Tooltip("You need specify the video stream type")]
        public RtcVideoStreamType VIDEO_STREAM_TYPE = RtcVideoStreamType.kNERTCVideoStreamMain;

        [Header("Log Output")]
        public Text _logText;

        [Header("Set Local Video Canvas")]
        public RawImage localVideoCanvas;

        [Header("Set Remote User Video Canvas")]
        public RawImage remoteVideoCanvas;

        private Rect _rect;
        private Texture2D _texture;
        private int _frameRate = 30;// your prefer frame rate

        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();
    
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

            InitTexture();

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

            //Enable the external video capture before joining the channel
            _rtcEngine.SetExternalVideoSource(VIDEO_STREAM_TYPE, true);

            //Enables local audio and local video capture.
            _rtcEngine.EnableLocalAudio(RtcAudioStreamType.kNERtcAudioStreamTypeMain, true);
            _rtcEngine.EnableLocalVideo(VIDEO_STREAM_TYPE, true);

            //Sets local views.This method is used to set the display information about the local video. The method is applicable for only local
            //users.Remote users are not affected.
            var canvas = new RtcVideoCanvas {
                callback = new VideoFrameCallback(OnTexture2DVideoFrame),
            };
            _rtcEngine.SetupLocalVideoCanvas(VIDEO_STREAM_TYPE, canvas);

#if UNITY_STANDALONE || UNITY_EDITOR
            //You need set the specified external input device on windows or macos platform
            _rtcEngine.VideoDeviceManager.SetDevice(VIDEO_STREAM_TYPE, RtcConstants.kNERtcExternalVideoDeviceID);
#endif
            //set the configures for video
            _rtcEngine.SetVideoConfig(VIDEO_STREAM_TYPE, new RtcVideoConfig
            {
                maxProfile = RtcVideoProfileType.kNERtcVideoProfileHD1080P,
                framerate = (RtcVideoFramerateType)_frameRate,
                width = (uint)_rect.width,
                height = (uint)_rect.height,
                mirrorMode = RtcVideoMirrorMode.kNERtcVideoMirrorModeEnabled,
            });
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


        private void InitTexture()
        {
            _logger.Log($"Screen.width:{Screen.width},Screen.height:{Screen.height}");
            _rect = new Rect(0, 0, Screen.width, Screen.height);
            _texture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        }

        // Update is called once per frame
        void Update()
        {
            StartCoroutine(shareScreen());
        }
        IEnumerator shareScreen()
        {
            yield return new WaitForEndOfFrame();
            if (_rtcEngine != null)
            {
                _texture.ReadPixels(_rect, 0, 0);
                _texture.Apply();
                byte[] bytes = _texture.GetRawTextureData();
                int bufferLength = bytes.Length;
                IntPtr nativeBuffer = Marshal.AllocHGlobal(bufferLength);
                Marshal.Copy(bytes, 0, nativeBuffer, bufferLength);
                var externalVideoFrame = new RtcExternalVideoFrame
                {
                    format = RtcVideoType.kNERtcVideoTypeARGB,
                    timestamp = (ulong)DateTime.Now.Ticks / 10000,
                    width = (uint)_texture.width,
                    height = (uint)_texture.height,
                    rotation = RtcVideoRotation.kNERtcVideoRotation180,
                    buffer = nativeBuffer,
                    bufferLength = bufferLength,
                    count = 1,
                    offsets = new long[4] {0,0,0,0},
                    strides = new uint[4] {(uint)_texture.width * 4 ,0,0,0},
                };
                int result = _rtcEngine.PushExternalVideoFrame(VIDEO_STREAM_TYPE, externalVideoFrame);
                Marshal.FreeHGlobal(nativeBuffer);
                //_logger.Log($"PushExternalVideoFrame result :{result}");
            }
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
            _rtcEngine.SetupRemoteVideoCanvas(uid, VIDEO_STREAM_TYPE, null);
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
                callback = new VideoFrameCallback(OnTexture2DVideoFrame),
            };
            _rtcEngine.SetupRemoteVideoCanvas(uid, type, canvas);
            _rtcEngine.SubscribeRemoteVideoStream(uid, type, RtcRemoteVideoStreamType.kNERtcRemoteVideoStreamTypeHigh, true);
        }

        private void OnUserVideoStopHandler(RtcVideoStreamType type, ulong uid)
        {
            _logger.Log($"OnUserVideoStop type - {type} ,uid - {uid}");
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
