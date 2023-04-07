using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class AudioVolumeIndicationSample : MonoBehaviour
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
        [Tooltip("The time interval for volume monitoring")]
        public ulong AudioVolumeIndicationInterval = 1000;

        [Header("Audio volume indication for the local user")]
        public Text _localAudioVolumeIndicationText;

        [Header("Audio volume indication for the remote users")]
        public Text _remoteAudioVolumeIndicationText;

        [Header("Log Output")]
        public Text _logText;

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
            _rtcEngine.OnLocalAudioVolumeIndication = OnLocalAudioVolumeIndicationHandler;
            _rtcEngine.OnRemoteAudioVolumeIndication = OnRemoteAudioVolumeIndicationHandler;
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

        public void OnEnableAudioVolumeIndicationClicked()
        {
            int result = _rtcEngine.EnableAudioVolumeIndication(true, AudioVolumeIndicationInterval);
            _logger.LogWarning($"RtcEngine EnableAudioVolumeIndication result : {result}");
        }

        public void OnDisableAudioVolumeIndicationClicked()
        {
            int result = _rtcEngine.EnableAudioVolumeIndication(false, 0);
            _logger.LogWarning($"RtcEngine EnableAudioVolumeIndication result : {result}");
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

        private void OnLocalAudioVolumeIndicationHandler(int volume)
        {
            Dispatcher.QueueOnMainThread(() =>
            {
                _localAudioVolumeIndicationText.text = $"local user \r\n volume : {volume}";
            });
        }
        private void OnRemoteAudioVolumeIndicationHandler(RtcAudioVolumeInfo[] speakers, int totalVolume)
        {
            Dispatcher.QueueOnMainThread(() =>
            {

                var sb = new StringBuilder();
                sb.Append($"Remote users \r\n totalVolume :{totalVolume} \r\n");
                if(speakers != null)
                {
                    foreach(var speaker in speakers)
                    {
                        sb.Append($"{speaker.uid} volume : {speaker.volume} \r\n");
                    }
                }

                _remoteAudioVolumeIndicationText.text = sb.ToString();
            });
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
