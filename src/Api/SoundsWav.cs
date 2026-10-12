using System;
using System.Text;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SoundsWav
    {
        private const int FormatPcm = 1;
        private const int FormatFloat = 3;
        private const int FormatExtensible = 0xFFFE;

        internal static AudioClip Parse(byte[] d, string name)
        {
            if (d.Length < 12 || Text(d, 0) != "RIFF" || Text(d, 8) != "WAVE") return null;
            int format = 0, channels = 0, rate = 0, bits = 0, dataStart = -1, dataLength = 0;
            int pos = 12;
            while (pos + 8 <= d.Length)
            {
                string id = Text(d, pos);
                int size = BitConverter.ToInt32(d, pos + 4);
                int body = pos + 8;
                if (id == "data")
                {
                    dataStart = body;
                    dataLength = size < 0 ? d.Length - body : Math.Min(size, d.Length - body);
                    break;
                }
                if (size < 0 || size > d.Length - body) return null;
                if (id == "fmt " && size >= 16)
                {
                    format = BitConverter.ToUInt16(d, body);
                    channels = BitConverter.ToUInt16(d, body + 2);
                    rate = BitConverter.ToInt32(d, body + 4);
                    bits = BitConverter.ToUInt16(d, body + 14);
                    if (format == FormatExtensible && size >= 26) format = BitConverter.ToUInt16(d, body + 24);
                }
                pos = body + size + (size & 1);
            }
            bool supported = (format == FormatPcm && (bits == 8 || bits == 16 || bits == 24 || bits == 32)) || (format == FormatFloat && bits == 32);
            if (dataStart < 0 || !supported || channels < 1 || rate < 1) return null;
            int width = bits / 8;
            int count = dataLength / (width * channels) * channels;
            if (count < 1) return null;
            float[] samples = new float[count];
            for (int i = 0; i < count; i++) samples[i] = Sample(d, dataStart + i * width, width, format);
            AudioClip clip = AudioClip.Create(name, count / channels, channels, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static string Text(byte[] d, int p)
        {
            return Encoding.ASCII.GetString(d, p, 4);
        }

        private static float Sample(byte[] d, int p, int width, int format)
        {
            if (format == FormatFloat) return BitConverter.ToSingle(d, p);
            if (width == 1) return (d[p] - 128) / 128f;
            if (width == 2) return BitConverter.ToInt16(d, p) / 32768f;
            if (width == 3) return ((d[p] | d[p + 1] << 8 | d[p + 2] << 16) << 8 >> 8) / 8388608f;
            return BitConverter.ToInt32(d, p) / 2147483648f;
        }
    }
}
