using System;
using System.IO;
using System.Text;

namespace ObenseuerCore
{
    internal sealed class SceneLookCursor
    {
        private readonly byte[] data;
        private int position;

        internal SceneLookCursor(byte[] data, int position)
        {
            this.data = data;
            this.position = position;
        }

        internal int Position
        {
            get { return position; }
        }

        internal int Length
        {
            get { return data.Length; }
        }

        internal byte U8()
        {
            Need(1);
            return data[position++];
        }

        internal ushort U16()
        {
            Need(2);
            ushort value = BitConverter.ToUInt16(data, position);
            position += 2;
            return value;
        }

        internal short I16()
        {
            Need(2);
            short value = BitConverter.ToInt16(data, position);
            position += 2;
            return value;
        }

        internal int I32()
        {
            Need(4);
            int value = BitConverter.ToInt32(data, position);
            position += 4;
            return value;
        }

        internal uint U32()
        {
            Need(4);
            uint value = BitConverter.ToUInt32(data, position);
            position += 4;
            return value;
        }

        internal long I64()
        {
            Need(8);
            long value = BitConverter.ToInt64(data, position);
            position += 8;
            return value;
        }

        internal float F32()
        {
            Need(4);
            float value = BitConverter.ToSingle(data, position);
            position += 4;
            return value;
        }

        internal int Count()
        {
            int count = I32();
            if (count < 0) throw new InvalidDataException("negative count " + count);
            return count;
        }

        internal SceneLookRef Pptr()
        {
            SceneLookRef reference = new SceneLookRef();
            reference.FileId = I32();
            reference.PathId = I64();
            return reference;
        }

        internal float[] Floats(int count)
        {
            Need(4L * count);
            float[] values = new float[count];
            for (int i = 0; i < count; i++) values[i] = F32();
            return values;
        }

        internal byte[] Raw(long count)
        {
            Need(count);
            int size = (int)count;
            byte[] copy = new byte[size];
            Buffer.BlockCopy(data, position, copy, 0, size);
            position += size;
            return copy;
        }

        internal void Skip(long count)
        {
            Need(count);
            position += (int)count;
        }

        internal void Align(int n)
        {
            position = (position + n - 1) & ~(n - 1);
        }

        internal string Text()
        {
            int count = I32();
            Need(count);
            string text = Encoding.UTF8.GetString(data, position, count);
            position += count;
            Align(4);
            return text;
        }

        internal string CString()
        {
            int end = Array.IndexOf(data, (byte)0, position);
            if (end < 0) throw new InvalidDataException("unterminated string");
            string text = Encoding.UTF8.GetString(data, position, end - position);
            position = end + 1;
            return text;
        }

        private void Need(long count)
        {
            if (count < 0 || count > data.Length - position) throw new InvalidDataException("read of " + count + " bytes is past the end of the object");
        }
    }
}
