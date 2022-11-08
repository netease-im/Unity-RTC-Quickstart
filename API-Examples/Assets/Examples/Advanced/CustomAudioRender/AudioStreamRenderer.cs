using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace nertc.examples
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioStreamRenderer : MonoBehaviour
    {
        
        public delegate void OnAudioRead(float[] data);


        public OnAudioRead AudioReadCallback;
        void OnAudioFilterRead(float[] data, int channels)
        {
            AudioReadCallback?.Invoke(data);
        }

    }
}
