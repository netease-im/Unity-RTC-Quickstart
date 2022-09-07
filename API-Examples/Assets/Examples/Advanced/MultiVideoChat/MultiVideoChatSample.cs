using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class MultiVideoChatSample : MonoBehaviour
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
       
        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();
        private const float _offset = 100;

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
        
        #region Engine Events
        private void OnJoinChannelHandler(ulong cid, ulong uid, RtcErrorCode result, ulong elapsed)
        {
            _logger.Log($"OnJoinChannel cid - {cid}, uid- {uid},result - {result}, elapsed - {elapsed}");
            Dispatcher.QueueOnMainThread(() => {
                var go = GameObject.Find($"0");
                if (go == null)
                {
                    MakeImageView($"0");
                }
            });
        }
        private void OnLeaveChannelHandler(RtcErrorCode result)
        {
            _logger.Log($"OnLeaveChannel result - {result}");
            Dispatcher.QueueOnMainThread(() =>
            {
                //Destroy all image views
                GameObject canvas = GameObject.Find("VideoCanvas");
                if (canvas != null)
                {
                    Debug.Log(canvas.transform.childCount);
                    var array = new List<GameObject>();
                    for(int idx = 0; idx < canvas.transform.childCount;idx++)
                    {
                        array.Add(canvas.transform.GetChild(idx).gameObject);
                    }

                    foreach(var go in array)
                    {
                        GameObject.Destroy(go);
                    }
                }

            });
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
            Dispatcher.QueueOnMainThread(() =>
            {
                DestroyImageView($"{uid}");
            });
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

            Dispatcher.QueueOnMainThread(() =>
            {
                var go = GameObject.Find($"{uid}");
                if (go == null)
                {
                    MakeImageView($"{uid}");
                }
            });
        }

        private void OnUserVideoStopHandler(ulong uid)
        {
            _logger.Log($"OnUserVideoStop uid - {uid}");
        }

        public void OnTexture2DVideoFrame(ulong uid, Texture2D texture, RtcVideoRotation rotation)
        {
            var go = GameObject.Find($"{uid}");
            if(go != null)
            {
                var rawImage = go.GetComponent<RawImage>();
                rawImage.texture = texture;
                go.transform.localRotation = Quaternion.Euler(0,0,-(float)rotation);
            }
        }

        public void MakeImageView(string goName)
        {
            var go = new GameObject();

            if (go == null)
            {
                return;
            }

            go.name = goName;
            go.AddComponent<RawImage>();
            go.AddComponent<DraggableUIElement>();
            GameObject canvas = GameObject.Find("VideoCanvas");
            if (canvas != null)
            {
                go.transform.SetParent(canvas.transform);
                _logger.Log("add video view");
            }
            // set up transform
            float xPos = UnityEngine.Random.Range(_offset - Screen.width / 2f, Screen.width / 2f - _offset);
            float yPos = UnityEngine.Random.Range(_offset, Screen.height / 2f - _offset);
            _logger.Log("position x " + xPos + " y: " + yPos);
            go.transform.localPosition = new Vector3(xPos, yPos, 0f);
            go.transform.localScale = new Vector3(4f, 4f, 1f);
        }

        private void DestroyImageView(string goName)
        {
            _logger.Log($"DestroyImageView {goName}");
            var go = GameObject.Find(goName);
            if(!ReferenceEquals(go,null))
            {
                GameObject.Destroy(go);
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
