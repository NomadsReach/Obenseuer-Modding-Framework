using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal sealed class SceneLookMeshStream
    {
        public int Stream;
        public int Start;
        public int Stride;
    }

    internal static class SceneLookMeshPlan
    {
        internal const int AttributeLimit = 14;
        internal const int FormatLimit = 12;
        private static readonly int[] FormatSizes = { 4, 2, 1, 1, 2, 2, 1, 1, 2, 2, 4, 4 };

        internal static List<SceneLookMeshStream> Plan(SceneLookMesh mesh, out string reason)
        {
            reason = null;
            List<SceneLookMeshStream> streams = new List<SceneLookMeshStream>();
            Dictionary<int, SceneLookMeshStream> known = new Dictionary<int, SceneLookMeshStream>();
            Dictionary<int, int> offsets = new Dictionary<int, int>();
            if (mesh.Channels == null)
            {
                reason = "mesh has no channel list";
                return null;
            }
            for (int i = 0; i < mesh.Channels.Count; i++)
            {
                SceneLookChannel channel = mesh.Channels[i];
                if (channel.Dimension == 0) continue;
                if (i >= AttributeLimit)
                {
                    reason = "mesh channel " + i + " is past the attribute limit";
                    return null;
                }
                if (channel.Format < 0 || channel.Format >= FormatLimit)
                {
                    reason = "vertex format " + channel.Format + " is not handled";
                    return null;
                }
                SceneLookMeshStream stream;
                if (!known.TryGetValue(channel.Stream, out stream))
                {
                    stream = new SceneLookMeshStream();
                    stream.Stream = channel.Stream;
                    known[channel.Stream] = stream;
                    offsets[channel.Stream] = 0;
                    streams.Add(stream);
                }
                int offset = offsets[channel.Stream];
                if (offset != channel.Offset)
                {
                    reason = "mesh channel offsets are not the layout order";
                    return null;
                }
                int size = channel.Dimension * FormatSizes[channel.Format];
                offsets[channel.Stream] = offset + size;
                stream.Stride = offset + size;
            }
            if (streams.Count == 0)
            {
                reason = "mesh has no vertex channels";
                return null;
            }
            long start = 0;
            for (int i = 0; i < streams.Count; i++)
            {
                streams[i].Start = (int)start;
                long size = mesh.VertexCount * streams[i].Stride;
                start += i < streams.Count - 1 ? (size + 15) / 16 * 16 : size;
            }
            if (start != mesh.VertexDataSize)
            {
                reason = "mesh vertex bytes are " + mesh.VertexDataSize + " and the layout needs " + start;
                return null;
            }
            return streams;
        }

        internal static List<int> Attributes(SceneLookMesh mesh)
        {
            List<int> attributes = new List<int>();
            for (int i = 0; i < mesh.Channels.Count; i++)
            {
                if (mesh.Channels[i].Dimension == 0) continue;
                attributes.Add(i);
            }
            return attributes;
        }
    }
}
