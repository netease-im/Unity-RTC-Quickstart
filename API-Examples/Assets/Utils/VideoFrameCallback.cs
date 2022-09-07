using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace nertc.examples
{
    public class VideoFrameCallback : IVideoFrameTextureCallback
    {
        private Action<ulong, Texture2D, RtcVideoRotation> _callback;

        public VideoFrameCallback(Action<ulong, Texture2D, RtcVideoRotation> callback)
        {
            _callback = callback;
        }
        public void OnVideoFrameCallback(ulong uid, Texture2D texture, RtcVideoRotation rotation)
        {
            _callback?.Invoke(uid, texture, rotation);
        }
    }

    public class ChannelVideoFrameCallback : IVideoFrameTextureCallback
    {
        private Action<IRtcChannel,ulong, Texture2D, RtcVideoRotation> _callback;
        IRtcChannel _channel = null;

        public ChannelVideoFrameCallback(IRtcChannel channel,Action<IRtcChannel, ulong, Texture2D, RtcVideoRotation> callback)
        {
            _callback = callback;
            _channel = channel;
        }
        public void OnVideoFrameCallback(ulong uid, Texture2D texture, RtcVideoRotation rotation)
        {
            _callback?.Invoke(_channel,uid, texture, rotation);
        }
    }
}
