using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace nertc.examples
{
    public class Audio3DSample : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("You need firstly create you app and fetch your APP_KEY.For more information,visit https://doc.yunxin.163.com/nertc/docs/TA0ODQ2NjI?platform=unity")]
        public string APP_KEY = "YOUR APP KEY";

        [SerializeField]
        [Tooltip("You need request a token in the safe mode or set null in the debugging mode.For more information,visit https://doc.yunxin.163.com/nertc/docs/jAzNzY2MDQ?platform=unity")]
        public string TOKEN = "";

        [SerializeField]
        public string CHANNEL_NAME = "YOUR CHANNEL NAME";

        [SerializeField]
        public ulong UID = 0;

        [Header("Log Output")]
        public Text _logText;

        [Header("3D Game Object")]
        public GameObject selfGameObject;
        [Header("Set 3D Game Object Position And Rotation")]
        public InputField SelfPositonX;
        public InputField SelfPositonY;
        public InputField SelfPositonZ;
        public InputField SelfRotationX;
        public InputField SelfRotationY;
        public InputField SelfRotationZ;

        [Header("Set Audio Recv Range")]
        [SerializeField]
        public int AudibleDistance = 30;
        [SerializeField]
        public int ConversationalDistance = 1;
        [SerializeField]
        public RtcDistanceRolloffModel RollOffModel = RtcDistanceRolloffModel.kNERtcDistanceRolloffLinear;

        [Header("Set Audio3D Render Mode")]
        [SerializeField]
        public RtcSpatializerRenderMode Audio3DRenderMode = RtcSpatializerRenderMode.kNERtcSpatializerRenderBinauralHighQuality;

        [Header("Room Effect Properties")]
        [SerializeField]
        public bool EnableRoomEffect = false;
        public RtcSpatializerRoomCapacity RoomCapacity = RtcSpatializerRoomCapacity.kNERtcSpatializerRoomCapacityLarge;
        public RtcSpatializerMaterialName RoomMaterial = RtcSpatializerMaterialName.kNERtcSpatializerMaterialTransparent;
        public float RoomReflectionScalar = 1.0f;
        public float RoomReverbGain = 1.0f;
        public float RoomReverbTime = 1.0f;
        public float RoomReverbBrightness = 1.0f;


        //--
        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();

        void Start()
        {
            _logger = new Logger(_logText);
            PermissionHelper.RequestMicroPhonePermission();

            _logger.Log($"Start");

            _ = Dispatcher.Current;
            BindEvent();
            if (InitRtcEngine())
            {
                StartCoroutine(UpdateMySelfPosition());
                JoinChannel();
            }

            SelfPositonX.text = selfGameObject.transform.position.x.ToString();
            SelfPositonY.text = selfGameObject.transform.position.y.ToString();
            SelfPositonZ.text = selfGameObject.transform.position.z.ToString();
            SelfRotationX.text = selfGameObject.transform.rotation.x.ToString();
            SelfRotationY.text = selfGameObject.transform.rotation.y.ToString();
            SelfRotationZ.text = selfGameObject.transform.rotation.z.ToString();
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

            _rtcEngine.EnableLocalAudio(true);
            Enable3DAudio();
            return true;
        }

        private void Enable3DAudio()
        {
            //open 3d Audio
            _rtcEngine.EnableSpatializer(true);
            _rtcEngine.SetSpatializerRenderMode(Audio3DRenderMode);
            _rtcEngine.UpdateSpatializerAudioRecvRange(AudibleDistance, ConversationalDistance, RollOffModel);

            //audio profile must be stereo,2 channels
            _rtcEngine.SetAudioProfile(RtcAudioProfileType.kNERtcAudioProfileMiddleQualityStereo, RtcAudioScenarioType.kNERtcAudioScenarioMusic);

            //enable or disable room effect if need
            _rtcEngine.EnableSpatializerRoomEffects(EnableRoomEffect);
            if (EnableRoomEffect)
            {
                var roomProperties = new RtcSpatializerRoomProperty
                {
                    roomCapacity = RoomCapacity,
                    material = RoomMaterial,
                    reflectionScalar = RoomReflectionScalar,
                    reverbGain = RoomReverbGain,
                    reverbTime = RoomReverbTime,
                    reverbBrightness = RoomReverbBrightness,
                };
                _rtcEngine.SetSpatializerRoomProperty(roomProperties);
            }
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
        }

        private void JoinChannel()
        {
            int result = _rtcEngine.JoinChannel(TOKEN, CHANNEL_NAME, UID);
            if (result != (int)RtcErrorCode.kNERtcNoError)
            {
                _logger.LogWarning($"RtcEngine JoinChannel Failed : {result}");
                return;
            }

            _logger.LogWarning($"RtcEngine JoinChannel result : {result}");
        }
        //update my position in the game world
        private IEnumerator UpdateMySelfPosition()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                if (selfGameObject  != null)
                {
                    var info = new RtcSpatializerPositionInfo();
                    info.headPosition = new float[3] { selfGameObject.transform.position.x, selfGameObject.transform.position.y, selfGameObject.transform.position.z };
                    info.headQuaternion = new float[4] { selfGameObject.transform.rotation.x, selfGameObject.transform.rotation.y, selfGameObject.transform.rotation.z, selfGameObject.transform.rotation.w };
                    info.speakerPosition = info.headPosition;
                    info.speakerQuaternion = info.headQuaternion;
                    int result = _rtcEngine.UpdateSpatializerSelfPosition(info);
                    if(result != (int)RtcErrorCode.kNERtcNoError)
                    {
                        _logger.Log($"UpdateSpatializerSelfPosition Failed : {result}");
                    }
                }
            }

        }

        // Update is called once per frame
        void Update()
        {
            var position = new Vector3
            {
                x = string.IsNullOrEmpty(SelfPositonX.text) ? 0 : float.Parse(SelfPositonX.text),
                y = string.IsNullOrEmpty(SelfPositonY.text) ? 0 : float.Parse(SelfPositonY.text),
                z = string.IsNullOrEmpty(SelfPositonZ.text) ? 0 : float.Parse(SelfPositonZ.text),
            };

            var rotation = new Vector3
            {
                x = string.IsNullOrEmpty(SelfRotationX.text) ? 0 : float.Parse(SelfRotationX.text),
                y = string.IsNullOrEmpty(SelfRotationY.text) ? 0 : float.Parse(SelfRotationY.text),
                z = string.IsNullOrEmpty(SelfRotationZ.text) ? 0 : float.Parse(SelfRotationZ.text),
            };

            selfGameObject.transform.position = position;
            selfGameObject.transform.rotation = Quaternion.Euler(rotation);
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
        }
        private void OnUserAudioStartHandler(ulong uid)
        {
            _logger.Log($"OnUserAudioStart uid - {uid}");
        }
        private void OnUserAudioStopHandler(ulong uid)
        {
            _logger.Log($"OnUserAudioStop uid - {uid}");
        }

        #endregion

        private void OnDestroy()
        {
            Debug.Log("Audio3DSample OnDestroy");
            StopAllCoroutines();
        }

        public void OnApplicationQuit()
        {
            //release engine object when the app will be quit
            _logger.Log("OnApplicationQuit");
            _rtcEngine.LeaveChannel();
            _rtcEngine.Release(true);
        }
    }
}
