using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class WebUiNative
    {
        internal const string Dll = "ObxWebView.dll";
        internal const int ErrorInsufficientBuffer = unchecked((int)0x8007007A);

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
        }
#pragma warning restore 649

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string path);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);

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
        internal static extern int Obx_Shutdown();

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
    }
}
