using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class CustomAudioRenderSample : MonoBehaviour
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

        [Header("Set External Audio Rendering")]
        public bool EnableExternalAudioRender = true;
        [Tooltip("Sets audio channels for external audio rendering")]
        public int AUDIO_CHANNELS = 2;
        [Tooltip("Sets audio sample rate for external audio rendering")]
        public int AUDIO_SAMPLE_RATE = 48000;
        [Tooltip("Sets the times per second to pull audio frames")]
        public int AUDIO_PULL_FREQ_PER_SEC = 100;

        [Header("Log Output")]
        public Text _logText;


        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();

        private bool _isReady = false;
        private bool _pullAudioFrameThreadSignal = true;

        private Thread _pullAudioFrameThread;
        private RingBuffer<float> _audioBuffer;
        private AudioSource _audioSource;
        private AudioStreamRenderer _audioRender;

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
                //prepare for external audio rendering
                PrepareCustomAudioRender();

                //join audio channel
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

            //Enable or disable external audio rendering.
            _rtcEngine.SetExternalAudioRender(EnableExternalAudioRender, AUDIO_SAMPLE_RATE, AUDIO_CHANNELS);

            _logger.Log($"EnableExternalAudioRender : {EnableExternalAudioRender},SampleRate:{AUDIO_SAMPLE_RATE},Channels:{AUDIO_CHANNELS},PullFreq:{AUDIO_PULL_FREQ_PER_SEC}\r\n");
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

        private void PrepareCustomAudioRender()
        {
            if (!EnableExternalAudioRender)
            {
                return;
            }
            var maxBufferLength = AUDIO_SAMPLE_RATE / AUDIO_PULL_FREQ_PER_SEC * AUDIO_CHANNELS * 20; // 200ms 
            _audioBuffer = new RingBuffer<float>(maxBufferLength, true);

            _audioRender = gameObject.AddComponent<AudioStreamRenderer>();
            _audioSource = gameObject.GetComponent<AudioSource>();

            _audioRender.AudioReadCallback = OnAudioRead;
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;

            //start a thread 
            _pullAudioFrameThreadSignal = true;
            _pullAudioFrameThread = new Thread(PullAudioFrameThread);
            _pullAudioFrameThread.Start();
        }

        private void UnprepareCustomAudioRender()
        {
            _pullAudioFrameThreadSignal = false;
            _isReady = false;
            _pullAudioFrameThread?.Abort();
            _pullAudioFrameThread = null;
            _audioSource?.Stop();
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

        #region 

        //
        private void OnAudioRead(float[] data)
        {
            if (!_isReady)
            {
                return;
            }
            if(data.Length > (_audioBuffer?.Size ?? 0))
            {
                return;
            }
            for (var i = 0; i < data.Length; i++)
            {
                lock (_audioBuffer)
                {
                    data[i] = _audioBuffer.Get();
                }
            }
        }

        private void PullAudioFrameThread()
        {
            var bytesPerSample = 2;
            var freq = 1000 / AUDIO_PULL_FREQ_PER_SEC;
            int bufferLength = AUDIO_SAMPLE_RATE * bytesPerSample * AUDIO_CHANNELS * freq / 1000;
            var buffer = new byte[bufferLength];

            var watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            long durationCount = 0;
            while (_pullAudioFrameThreadSignal)
            {
                long duration = watcher.ElapsedMilliseconds - durationCount;
                if (duration >= freq)
                {
                    durationCount += freq;
                    duration -= freq;
                    int result = _rtcEngine.PullExternalAudioFrame(buffer, buffer.Length);
                    if (result != (int)RtcErrorCode.kNERtcNoError)
                    {
                        continue;
                    }

                    float[] floatArray = ByteArrayToFloatArray(buffer);
                    lock (_audioBuffer)
                    {
                        _audioBuffer.Put(floatArray);
                    }
                    if (!_isReady)
                    {
                        _isReady = true;
                    }
                }
                Thread.Sleep(1);
            }

            watcher.Stop();
        }
        private float[] ByteArrayToFloatArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / 2];
            for (var i = 0; i < floatArray.Length; i++)
            {
                floatArray[i] = BitConverter.ToInt16(byteArray, i * 2) / 32768f;
            }
            return floatArray;
        }
        #endregion

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
        }
        private void OnUserAudioStartHandler(RtcAudioStreamType type, ulong uid)
        {
            _logger.Log($"OnUserAudioStart type - {type} ,uid - {uid}");
        }
        private void OnUserAudioStopHandler(RtcAudioStreamType type, ulong uid)
        {
            _logger.Log($"OnUserAudioStop type - {type} ,uid - {uid}");
        }
        #endregion

        public void OnApplicationQuit()
        {
            _logger.Log("OnApplicationQuit");

            //stop thread
            UnprepareCustomAudioRender();

            //you must release engine object when the app will be quit.
            //If you need use IRtcEngine again after release ,you can be Initialize again.
            //In this,you need call leave channel and Release engine resources.
            _rtcEngine.LeaveChannel();
            _rtcEngine.Release(true);
        }
    }
}
