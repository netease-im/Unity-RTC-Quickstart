using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class CustomAudioInputSample : MonoBehaviour
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
        [Tooltip("You need specify the audio stream type")]
        public RtcAudioStreamType AUDIO_STREAM_TYPE = RtcAudioStreamType.kNERtcAudioStreamTypeMain;

        [Header("Log Output")]
        public Text _logText;

        [Header("Set External Audio Input")]
        public bool EnableExternalAudioInput = true;
        [Tooltip("Sets audio channels for external audio input")]
        public int AUDIO_CHANNELS = 2;
        [Tooltip("Sets audio sample rate for external audio input")]
        public int AUDIO_SAMPLE_RATE = 48000;
        [Tooltip("Sets the times per second to push audio frames")]
        public int AUDIO_PUSH_FREQ_PER_SEC = 50;


        private bool _isReady = false;
        private bool _pushAudioFrameThreadSignal = true;

        private Thread _pushAudioFrameThread;
        private RingBuffer<byte> _audioBuffer;

        Logger _logger;
        IRtcEngine _rtcEngine = IRtcEngine.GetInstance();
    
        void Start()
        {
            _logger = new Logger(_logText);
            PermissionHelper.RequestMicroPhonePermission();

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
            //prepare for external audio capture
            PrepareCustomAudioInput();
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
            _rtcEngine.EnableLocalAudio(AUDIO_STREAM_TYPE, true);

            //Enable the external audio capture before joining the channel
            _rtcEngine.SetExternalAudioSource(AUDIO_STREAM_TYPE, EnableExternalAudioInput, AUDIO_SAMPLE_RATE,AUDIO_CHANNELS);

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

        private void PrepareCustomAudioInput()
        {
            _logger.Log($"PrepareCustomAudioInput custom audio input : {EnableExternalAudioInput}");
            if (!EnableExternalAudioInput)
            {
                return;
            }
            var maxBufferLength = AUDIO_SAMPLE_RATE / AUDIO_PUSH_FREQ_PER_SEC * AUDIO_CHANNELS * 20;
            _audioBuffer = new RingBuffer<byte>(maxBufferLength, true);
            
            //start a thread 
            _pushAudioFrameThreadSignal = true;
            _isReady = true;
            _pushAudioFrameThread = new Thread(PushAudioFrameThread);
            _pushAudioFrameThread.Start();

        }

        private void PushAudioFrameThread(object obj)
        {
            var bytesPerSample = 2;
            var freq = 1000 / AUDIO_PUSH_FREQ_PER_SEC;
            var samplesPerChannel = AUDIO_SAMPLE_RATE * freq / 1000;
            int bufferLength = samplesPerChannel * bytesPerSample * AUDIO_CHANNELS;
            var buffer = new byte[bufferLength];
            var nativeBuffer = Marshal.AllocHGlobal(bufferLength);

            var watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            long durationCount = 0;
            while (_pushAudioFrameThreadSignal)
            {
                long duration = watcher.ElapsedMilliseconds - durationCount;
                if (duration >= freq)
                {
                    durationCount += freq;
                    duration -= freq;
                    if(_audioBuffer.Size >= bufferLength)
                    {
                        lock (_audioBuffer)
                        {
                            for(int idx = 0; idx < buffer.Length; idx++)
                            {
                                buffer[idx] = _audioBuffer.Get();
                            }

                            Marshal.Copy(buffer, 0, nativeBuffer, bufferLength);
                        }
                        var frame = new RtcAudioFrame()
                        {
                            format = new RtcAudioFormat
                            {
                                type = RtcAudioType.kNERtcAudioTypePCM16,
                                channels = (uint)AUDIO_CHANNELS,
                                sampleRate = (uint)AUDIO_SAMPLE_RATE,
                                bytesPerSample = (uint)bytesPerSample,
                                samplesPerChannel = (uint)samplesPerChannel,
                            },
                            data = nativeBuffer,
                        };
                        int result = _rtcEngine.PushExternalAudioFrame(AUDIO_STREAM_TYPE, frame);
                        if (result != (int)RtcErrorCode.kNERtcNoError)
                        {
                            UnityEngine.Debug.Log($"PushExternalAudioFrame result {result}");
                        }
                    }                    
                }
                Thread.Sleep(1);
            }

            watcher.Stop();
            Marshal.FreeHGlobal(nativeBuffer);
        }

        private void UnprepareCustomAudioInput()
        {
            _pushAudioFrameThreadSignal = false;
            _isReady = false;
            _pushAudioFrameThread?.Abort();
            _pushAudioFrameThread = null;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if(!_isReady)
            {
                return;
            }
            var bytes = new byte[2];
            foreach (var t in data)
            {
                var smaple = (t > 1) ? 1 : (t < -1) ? -1 : t;
                bytes = BitConverter.GetBytes((short)(smaple * 32767f));
                lock (_audioBuffer)
                {
                    _audioBuffer.Put(bytes[0]);
                    _audioBuffer.Put(bytes[1]);
                }
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
            UnprepareCustomAudioInput();

            //you must release engine object when the app will be quit.
            //If you need use IRtcEngine again after release ,you can be Initialize again.
            //In this,you need call leave channel and Release engine resources.
            _rtcEngine.LeaveChannel();
            _rtcEngine.Release(true);
        }
    }
}
