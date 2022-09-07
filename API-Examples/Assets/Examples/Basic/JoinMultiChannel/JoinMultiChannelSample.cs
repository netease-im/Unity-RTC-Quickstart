using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class JoinMultiChannelSample : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("You need firstly create you app and fetch your APP_KEY.For more information,visit https://doc.yunxin.163.com/nertc/docs/TA0ODQ2NjI?platform=unity")]
        public string APP_KEY = "YOUR APP KEY";

        [SerializeField]
        [Tooltip("You need request a token in the safe mode or set null in the debugging mode.For more information,visit https://doc.yunxin.163.com/nertc/docs/jAzNzY2MDQ?platform=unity")]
        public string CHANNEL1_TOKEN = "";
        public string CHANNEL2_TOKEN = "";

        [SerializeField]
        public string CHANNEL_NAME_1 = "YOUR CHANNEL NAME 1";

        [SerializeField]
        public string CHANNEL_NAME_2 = "YOUR CHANNEL NAME 2";

        [SerializeField]
        public ulong UID = 0;

        [Header("Log Output")]
        public Text _logText;

        [Header("Set Local Video Canvas")]
        public RawImage channel1LocalVideoCanvas;

        [Header("Set Remote User Video Canvas")]
        public RawImage channel1RemoteVideoCanvas;
        public RawImage channel2RemoteVideoCanvas;

        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();
        IRtcChannel _channel1 = null;
        IRtcChannel _channel2 = null;

        void Start()
        {
            _logger = new Logger(_logText);
            PermissionHelper.RequestMicroPhonePermission();
            PermissionHelper.RequestCameraPermission();

            _logger.Log($"Start");

            _ = Dispatcher.Current;
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
            if (result != (int)RtcErrorCode.kNERtcNoError)
            {
                _logger.LogWarning($"RtcEngine Initialize Failed : {result}");
                return false;
            }

            _logger.Log($"RtcEngine Initialize Success");

            //create channel object
            _channel1 = _rtcEngine.CreateChannel(CHANNEL_NAME_1);
            _channel2 = _rtcEngine.CreateChannel(CHANNEL_NAME_2);

            //bind channel callbacks
            BindChannelEvent(_channel1);
            BindChannelEvent(_channel2);

            _channel1.SetClientRole(RtcClientRole.kNERtcClientRoleBroadcaster);
            _channel2.SetClientRole(RtcClientRole.kNERtcClientRoleAudience);

            //only one channel can publish audio or video stream
            _channel1.EnableLocalAudio(true);
            _channel1.EnableLocalVideo(true);

            _channel2.EnableLocalAudio(false);
            _channel2.EnableLocalVideo(false);

            var canvas = new RtcVideoCanvas
            {
                callback = new ChannelVideoFrameCallback(_channel1,ChannelOnTexture2DVideoFrame),
            };
            _channel1.SetupLocalVideoCanvas(canvas);
            return true;
        }

        private void BindChannelEvent(IRtcChannel channel)
        {
            /* All events are not called on main thread.If you want to update UI，you should schedule a task to the main thread.
            * You can do it like this:
            * private void ChannelOnJoinChannelHandler(IRtcChannel channel,ulong cid, ulong uid, RtcErrorCode result, ulong elapsed) {
            *   Dispatcher.QueueOnMainThread(() => { 
            *       //update UI here
            *   });
            * }
            */
            channel.ChannelOnJoinChannel = ChannelOnJoinChannelHandler;
            channel.ChannelOnLeaveChannel = ChannelOnLeaveChannelHandler;
            channel.ChannelOnUserJoined = ChannelOnUserJoinedHandler;
            channel.ChannelOnUserLeft = ChannelOnUserLeftHandler;
            channel.ChannelOnUserAudioStart = ChannelOnUserAudioStartHandler;
            channel.ChannelOnUserAudioStop = ChannelOnUserAudioStopHandler;
            channel.ChannelOnUserVideoStart = ChannelOnUserVideoStartHandler;
            channel.ChannelOnUserVideoStop = ChannelOnUserVideoStopHandler;
        }

        private void JoinChannel()
        {
            int result = _channel1.JoinChannel(CHANNEL1_TOKEN, UID);
            if (result != (int)RtcErrorCode.kNERtcNoError)
            {
                _logger.LogWarning($"RtcEngine JoinChannel Failed : {CHANNEL_NAME_1},{result}");
                return;
            }

            _logger.LogWarning($"RtcEngine JoinChannel result : {CHANNEL_NAME_1},{result}");

            result = _channel2.JoinChannel(CHANNEL2_TOKEN, UID);
            if (result != (int)RtcErrorCode.kNERtcNoError)
            {
                _logger.LogWarning($"RtcEngine JoinChannel Failed : {CHANNEL_NAME_2},{result}");
                return;
            }

            _logger.LogWarning($"RtcEngine JoinChannel result : {CHANNEL_NAME_2},{result}");
        }

        // Update is called once per frame
        void Update()
        {

        }
      
        #region Engine Events
        private void ChannelOnJoinChannelHandler(IRtcChannel channel,ulong cid, ulong uid, RtcErrorCode result, ulong elapsed)
        {
            _logger.Log($"ChannelOnJoinChannel channel - {channel.GetChannelName()},cid - {cid}, uid- {uid},result - {result}, elapsed - {elapsed}");
        }
        private void ChannelOnLeaveChannelHandler(IRtcChannel channel, RtcErrorCode result)
        {
            _logger.Log($"ChannelOnLeaveChannel channel - {channel.GetChannelName()}, result - {result}");
        }
        private void ChannelOnUserJoinedHandler(IRtcChannel channel, ulong uid, string userName)
        {
            _logger.Log($"OnUserJoined channel - {channel.GetChannelName()}, uid - {uid},userName - {userName}");
        }
        private void ChannelOnUserLeftHandler(IRtcChannel channel, ulong uid, RtcSessionLeaveReason reason)
        {
            _logger.Log($"ChannelOnUserLeft channel - {channel.GetChannelName()}, uid - {uid},reason - {reason}");
            //remove video canvas after user left
            channel.SetupRemoteVideoCanvas(uid, null);
        }
        private void ChannelOnUserAudioStartHandler(IRtcChannel channel, ulong uid)
        {
            _logger.Log($"ChannelOnUserAudioStart channel - {channel.GetChannelName()}, uid - {uid}");
        }
        private void ChannelOnUserAudioStopHandler(IRtcChannel channel, ulong uid)
        {
            _logger.Log($"ChannelOnUserAudioStop channel - {channel.GetChannelName()}, uid - {uid}");
        }
        private void ChannelOnUserVideoStartHandler(IRtcChannel channel, ulong uid, RtcVideoProfileType maxProfile)
        {
            _logger.Log($"ChannelOnUserVideoStart channel - {channel.GetChannelName()}, uid - {uid},maxProfile - {maxProfile}");

            //set remote user canvas and subscribe user video stream
            var canvas = new RtcVideoCanvas
            {
                callback = new ChannelVideoFrameCallback(channel,ChannelOnTexture2DVideoFrame),
            };
            channel.SetupRemoteVideoCanvas(uid, canvas);
            channel.SubscribeRemoteVideoStream(uid, RtcRemoteVideoStreamType.kNERtcRemoteVideoStreamTypeHigh, true);
        }

        private void ChannelOnUserVideoStopHandler(IRtcChannel channel, ulong uid)
        {
            _logger.Log($"ChannelOnUserVideoStop channel - {channel.GetChannelName()},uid - {uid}");
        }

        public void ChannelOnTexture2DVideoFrame(IRtcChannel channel, ulong uid, Texture2D texture, RtcVideoRotation rotation)
        {
            if (uid == 0)
            {
                if (channel1LocalVideoCanvas != null)
                {
                    channel1LocalVideoCanvas.texture = texture;
                }
                return;
            }

            if (channel == _channel1 && channel1RemoteVideoCanvas != null)
            {
                channel1RemoteVideoCanvas.texture = texture;
            }

            if (channel == _channel2 && channel2RemoteVideoCanvas != null)
            {
                channel2RemoteVideoCanvas.texture = texture;
            }
        }

        #endregion

        public void OnApplicationQuit()
        {
            //destroy channels and release engine object when the app will be quit
            _logger.Log("OnApplicationQuit");
            _channel1?.LeaveChannel();
            _channel2?.LeaveChannel();
            _channel1?.Destroy();
            _channel2?.Destroy();
            _rtcEngine.Release(true);
        }
    }
}