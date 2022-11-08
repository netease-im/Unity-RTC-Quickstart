using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace nertc.examples
{
    public static class ScreenShareHelper
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        
        // Save window titles and handles in these lists.
        private static Dictionary<string, IntPtr> windowInfo;
        private static List<MonitorInfoWithHandle> displayInfo;

        public static MonitorInfoWithHandle[] GetWinDisplayInfo()
        {
            displayInfo = new List<MonitorInfoWithHandle>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, FilterDisplayCallback, IntPtr.Zero);
            return displayInfo.ToArray();
        }

        // Return a list of the desktop windows' handles and titles.
        public static Dictionary<string, IntPtr> GetDesktopWindowInfo()
        {
            windowInfo = new Dictionary<string, IntPtr>();

            if (!EnumDesktopWindows(IntPtr.Zero, FilterCallback, IntPtr.Zero))
            {
                return null;
            }

            return windowInfo;
        }

        // We use this function to filter windows.
        // This version selects visible windows that have titles.
        [MonoPInvokeCallback(typeof(EnumWindowsDelegate))]
        private static bool FilterCallback(IntPtr hWnd, int lParam)
        {
            // Get the window's title.
            StringBuilder sbTitle = new StringBuilder(1024);
            GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
            string title = sbTitle.ToString();

            // If the window is visible and has a title, save it.
            if (IsWindowVisible(hWnd) &&
                !string.IsNullOrEmpty(title))
            {
                if (windowInfo.ContainsKey(title)) {
                    title = string.Format("{0}{1}", title, hWnd);
                }
                windowInfo.Add(title, hWnd);
            }

            // Return true to indicate that we
            // should continue enumerating windows.
            return true;
        }
        [MonoPInvokeCallback(typeof(EnumMonitorsDelegate))]
        private static bool FilterDisplayCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            var mi = new MONITORINFO();
            mi.size = (uint)Marshal.SizeOf(mi);
            GetMonitorInfo(hMonitor, ref mi);

            // Add to monitor info
            displayInfo.Add(new MonitorInfoWithHandle {
                MonitorHandle = hMonitor,
                MonitorInfo = mi
            });
            return true;
        }


        #region native methods

        // Monitor information
        public class MonitorInfoWithHandle
        {
            public IntPtr MonitorHandle { get; set; }
            public MONITORINFO MonitorInfo { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint size;
            public RECT monitor;
            public RECT work;
            public uint flags;
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd,
            StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumDesktopWindows(IntPtr hDesktop,
            EnumWindowsDelegate lpEnumCallbackFunction, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "EnumDisplayMonitors",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            EnumMonitorsDelegate lpEnumCallbackFunction, IntPtr dwData);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfo",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr hmon, ref MONITORINFO mi);

        // Define the callback delegate's type.
        private delegate bool EnumWindowsDelegate(IntPtr hWnd, int lParam);

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        #endregion
#endif
    }
}


