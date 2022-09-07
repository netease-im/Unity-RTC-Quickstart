using UnityEngine;
using UnityEngine.UI;

namespace nertc.examples
{
    public class Logger
    {
        Text _logText;
        public Logger(Text logText)
        {
            _logText = logText;
        }

        public void Log(string message)
        {
            updateLogText(message);
            Debug.Log(message + "\r\n");
        }

        public void LogError(string message)
        {
            updateLogText(message);
            Debug.LogError(message + "\r\n");
        }

        public void LogWarning(string message)
        {
            updateLogText(message);
            Debug.LogWarning(message + "\r\n");
        }

        public void LogAssert(string message)
        {
            updateLogText(message);
            Debug.LogAssertion(message + "\r\n");
        }


        private void updateLogText(string message)
        {
            Dispatcher.QueueOnMainThread(() =>
            {
                if (_logText == null)
                {
                    return;
                }
                string text = _logText.text;
                if (text.Length > 2000 || text == null)
                {
                    _logText.text = string.Empty;
                }
                text = $"{message} \r\n {text}";
                _logText.text = text;
            });
        }
    }
}
