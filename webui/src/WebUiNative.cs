using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class WebUiNative
    {
        internal const string Dll = "ObxWebView.dll";
        internal const int ErrorInsufficientBuffer = unchecked((int)0x8007007A);
        private const double SlowCallMs = 100;
        private const int RecentSize = 8;
        private static readonly string[] recentNames = new string[RecentSize];
        private static readonly long[] recentEnds = new long[RecentSize];
        private static int recentNext;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

#pragma warning disable 649
        [Serializable]
        public sealed class Envelope
        {
            public string type;
            public string where;
            public int hr;
            public int vk;
            public int dropped;
            public string mode;
            public string popup;
            public string focus;
            public string slow;
            public string text;
        }
#pragma warning restore 649

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string path);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);

        private static class Raw
        {
            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_Init(IntPtr hwnd, string userDataFolder, int flags);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_Create(int x, int y, int w, int h, string url);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_SetBounds(int id, int x, int y, int w, int h);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_SetVisible(int id, int visible);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_Navigate(int id, string url);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_MapFolder(int id, string host, string folder);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_SetShellHost(int id, string host);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_AllowFrameHost(int id, string host);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_PostJson(int id, string json);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_AddInitScript(int id, string script);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_PollMessage(StringBuilder buffer, int capacity, out int id, out int isNative);

            [DllImport(Dll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_LastError(StringBuilder buffer, int capacity);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_Destroy(int id);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_SetSuspended(int suspended);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            internal static extern int Obx_Shutdown();
        }

        internal static int Obx_Init(IntPtr hwnd, string userDataFolder, int flags)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_Init(hwnd, userDataFolder, flags);
            Timed("Obx_Init", start);
            return result;
        }

        internal static int Obx_Create(int x, int y, int w, int h, string url)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_Create(x, y, w, h, url);
            Timed("Obx_Create", start);
            return result;
        }

        internal static int Obx_SetBounds(int id, int x, int y, int w, int h)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_SetBounds(id, x, y, w, h);
            Timed("Obx_SetBounds", start);
            return result;
        }

        internal static int Obx_SetVisible(int id, int visible)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_SetVisible(id, visible);
            Timed("Obx_SetVisible", start);
            return result;
        }

        internal static int Obx_Navigate(int id, string url)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_Navigate(id, url);
            Timed("Obx_Navigate", start);
            return result;
        }

        internal static int Obx_MapFolder(int id, string host, string folder)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_MapFolder(id, host, folder);
            Timed("Obx_MapFolder", start);
            return result;
        }

        internal static int Obx_SetShellHost(int id, string host)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_SetShellHost(id, host);
            Timed("Obx_SetShellHost", start);
            return result;
        }

        internal static int Obx_AllowFrameHost(int id, string host)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_AllowFrameHost(id, host);
            Timed("Obx_AllowFrameHost", start);
            return result;
        }

        internal static int Obx_PostJson(int id, string json)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_PostJson(id, json);
            Timed("Obx_PostJson", start);
            return result;
        }

        internal static int Obx_AddInitScript(int id, string script)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_AddInitScript(id, script);
            Timed("Obx_AddInitScript", start);
            return result;
        }

        internal static int Obx_PollMessage(StringBuilder buffer, int capacity, out int id, out int isNative)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_PollMessage(buffer, capacity, out id, out isNative);
            Timed("Obx_PollMessage", start);
            return result;
        }

        internal static int Obx_LastError(StringBuilder buffer, int capacity)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_LastError(buffer, capacity);
            Timed("Obx_LastError", start);
            return result;
        }

        internal static int Obx_Destroy(int id)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_Destroy(id);
            Timed("Obx_Destroy", start);
            return result;
        }

        internal static int Obx_SetSuspended(int suspended)
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_SetSuspended(suspended);
            Timed("Obx_SetSuspended", start);
            return result;
        }

        internal static int Obx_Shutdown()
        {
            long start = Stopwatch.GetTimestamp();
            int result = Raw.Obx_Shutdown();
            Timed("Obx_Shutdown", start);
            return result;
        }

        internal static void ClientSize(IntPtr hwnd, out int width, out int height)
        {
            NativeRect r;
            if (GetClientRect(hwnd, out r) && r.Right > r.Left && r.Bottom > r.Top)
            {
                width = r.Right - r.Left;
                height = r.Bottom - r.Top;
            }
            else
            {
                width = Screen.width;
                height = Screen.height;
            }
        }

        internal static string LastText()
        {
            StringBuilder text = new StringBuilder(512);
            Obx_LastError(text, text.Capacity);
            return text.ToString();
        }

        internal static string LastCall()
        {
            int last = (recentNext + RecentSize - 1) % RecentSize;
            if (recentNames[last] == null) return "none";
            double ago = (Stopwatch.GetTimestamp() - recentEnds[last]) * 1000.0 / Stopwatch.Frequency;
            return recentNames[last] + " " + ago.ToString("F0") + " ms ago";
        }

        private static void Timed(string name, long start)
        {
            long end = Stopwatch.GetTimestamp();
            double ms = (end - start) * 1000.0 / Stopwatch.Frequency;
            if (name != "Obx_PollMessage" && name != "Obx_LastError")
            {
                recentNames[recentNext] = name;
                recentEnds[recentNext] = end;
                recentNext = (recentNext + 1) % RecentSize;
            }
            if (ms > SlowCallMs) CoreLog.For(WebUi.Owner).Warn("WebUi shim call " + name + " took " + ms.ToString("F0") + " ms on the game thread");
        }
    }
}
