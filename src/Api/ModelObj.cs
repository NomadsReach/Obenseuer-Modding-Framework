using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ObenseuerCore
{
    internal sealed class ObjSubMesh
    {
        internal string Material;
        internal List<int> Triangles = new List<int>();
    }

    internal sealed class ObjData
    {
        internal float[] Positions = new float[0];
        internal float[] Uvs = new float[0];
        internal float[] Normals = new float[0];
        internal readonly List<ObjSubMesh> SubMeshes = new List<ObjSubMesh>();
        internal string MtlLib;
        internal readonly List<string> Warnings = new List<string>();
    }

    internal static class ObjParser
    {
        internal const long MaxFileBytes = 64L * 1024 * 1024;
        internal const int MaxVertices = 2000000;
        internal const int MaxAttribute = 2097151;
        internal const int MaxSubMeshes = 8;
        internal const int MaxIndices = MaxVertices * 3;
        internal const int MaxLineValues = MaxIndices / 3 + 3;

        private sealed class State
        {
            internal bool Flip;
            internal readonly List<float> Pos = new List<float>();
            internal readonly List<float> Uv = new List<float>();
            internal readonly List<float> Nor = new List<float>();
            internal readonly List<float> OutPos = new List<float>();
            internal readonly List<float> OutUv = new List<float>();
            internal readonly List<float> OutNor = new List<float>();
            internal readonly Dictionary<ulong, int> Unique = new Dictionary<ulong, int>();
            internal readonly ObjData Data = new ObjData();
            internal string Material;
            internal ObjSubMesh Current;
            internal bool FanWarned;
            internal int Indices;
            internal bool SawUv;
            internal bool SawNoUv;
            internal bool SawNormal;
            internal bool SawNoNormal;
        }

        internal static ObjData Parse(string text, bool flipX, out string error)
        {
            if (text == null)
            {
                error = "no text";
                return null;
            }
            if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);
            State s = new State();
            s.Flip = flipX;
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string e = Line(s, lines[i].Trim());
                if (e != null)
                {
                    error = "line " + (i + 1) + ": " + e;
                    return null;
                }
            }
            if (s.Data.SubMeshes.Count == 0)
            {
                error = "no faces";
                return null;
            }
            bool partialUv = s.SawUv && s.SawNoUv;
            bool partialNormal = s.SawNormal && s.SawNoNormal;
            if (partialUv) s.Data.Warnings.Add("some corners have a uv and some do not; uvs dropped");
            if (partialNormal) s.Data.Warnings.Add("some corners have a normal and some do not; normals dropped, the mesh builder recalculates them");
            s.Data.Positions = s.OutPos.ToArray();
            s.Data.Uvs = s.SawUv && !partialUv ? s.OutUv.ToArray() : new float[0];
            s.Data.Normals = s.SawNormal && !partialNormal ? s.OutNor.ToArray() : new float[0];
            error = null;
            return s.Data;
        }

        internal static ObjData ParseFile(string path, bool flipX, out string error)
        {
            if (!File.Exists(path))
            {
                error = "file not found: " + path;
                return null;
            }
            try
            {
                if (new FileInfo(path).Length > MaxFileBytes)
                {
                    error = "file is larger than 64 MB";
                    return null;
                }
                return Parse(File.ReadAllText(path), flipX, out error);
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }

        private static string Line(State s, string line)
        {
            if (line.Length == 0 || line[0] == '#') return null;
            if (CountValues(line) > MaxLineValues) return "line has more than " + MaxLineValues + " values";
            string[] t = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            switch (t[0])
            {
                case "v":
                    return Floats(s.Pos, t, 3, "position");
                case "vt":
                    return Floats(s.Uv, t, 2, "uv");
                case "vn":
                    return Floats(s.Nor, t, 3, "normal");
                case "f":
                    return Face(s, t);
                case "usemtl":
                    s.Material = Rest(line, t[0]);
                    s.Current = null;
                    return null;
                case "mtllib":
                    if (s.Data.MtlLib == null) s.Data.MtlLib = Rest(line, t[0]);
                    return null;
                default:
                    return null;
            }
        }

        private static int CountValues(string line)
        {
            int count = 0;
            bool inside = false;
            foreach (char c in line)
            {
                bool separator = c == ' ' || c == '\t';
                if (!separator && !inside) count++;
                inside = !separator;
            }
            return count;
        }

        private static string Rest(string line, string keyword)
        {
            return line.Substring(keyword.Length).Trim();
        }

        private static string Floats(List<float> into, string[] t, int count, string what)
        {
            if (t.Length < count + 1) return what + " needs " + count + " numbers";
            if (into.Count / count >= MaxAttribute) return "more than " + MaxAttribute + " " + what + " values";
            for (int i = 1; i <= count; i++)
            {
                float f;
                if (!float.TryParse(t[i], NumberStyles.Float, CultureInfo.InvariantCulture, out f) || float.IsNaN(f) || float.IsInfinity(f)) return "bad number " + t[i];
                into.Add(f);
            }
            return null;
        }

        private static string Face(State s, string[] t)
        {
            int n = t.Length - 1;
            if (n < 3) return "face needs at least 3 corners";
            int added = (n - 2) * 3;
            if (s.Indices + added > MaxIndices) return "more than " + MaxIndices + " triangle indices";
            s.Indices += added;
            if (n > 4 && !s.FanWarned)
            {
                s.FanWarned = true;
                s.Data.Warnings.Add("faces with more than 4 corners are fan-triangulated; concave faces need Triangulate Faces in the exporter");
            }
            int[] c = new int[n];
            for (int i = 0; i < n; i++)
            {
                string e = Corner(s, t[i + 1], out c[i]);
                if (e != null) return e;
            }
            if (s.Current == null) s.Current = SubMeshFor(s);
            for (int i = 1; i + 1 < n; i++)
            {
                if (s.Flip)
                {
                    s.Current.Triangles.Add(c[0]);
                    s.Current.Triangles.Add(c[i + 1]);
                    s.Current.Triangles.Add(c[i]);
                }
                else
                {
                    s.Current.Triangles.Add(c[0]);
                    s.Current.Triangles.Add(c[i]);
                    s.Current.Triangles.Add(c[i + 1]);
                }
            }
            return null;
        }

        private static ObjSubMesh SubMeshFor(State s)
        {
            foreach (ObjSubMesh m in s.Data.SubMeshes)
            {
                if (m.Material == s.Material) return m;
            }
            if (s.Data.SubMeshes.Count < MaxSubMeshes)
            {
                ObjSubMesh created = new ObjSubMesh { Material = s.Material };
                s.Data.SubMeshes.Add(created);
                return created;
            }
            return s.Data.SubMeshes[s.Data.SubMeshes.Count - 1];
        }

        private static string Corner(State s, string token, out int vertex)
        {
            vertex = -1;
            string[] p = token.Split('/');
            if (p.Length > 3) return "bad face corner " + token;
            int v;
            int vt = -1;
            int vn = -1;
            if (!Index(p[0], s.Pos.Count / 3, out v)) return "bad vertex index " + token;
            if (p.Length > 1 && p[1].Length > 0 && !Index(p[1], s.Uv.Count / 2, out vt)) return "bad texture index " + token;
            if (p.Length > 2 && p[2].Length > 0 && !Index(p[2], s.Nor.Count / 3, out vn)) return "bad normal index " + token;
            if (vt >= 0) s.SawUv = true; else s.SawNoUv = true;
            if (vn >= 0) s.SawNormal = true; else s.SawNoNormal = true;
            ulong key = ((ulong)(uint)(v + 1) << 42) | ((ulong)(uint)(vt + 1) << 21) | (uint)(vn + 1);
            if (s.Unique.TryGetValue(key, out vertex)) return null;
            if (s.OutPos.Count / 3 >= MaxVertices) return "more than " + MaxVertices + " vertices";
            vertex = s.OutPos.Count / 3;
            s.Unique.Add(key, vertex);
            AddVertex(s, v, vt, vn);
            return null;
        }

        private static bool Index(string text, int count, out int index)
        {
            int value;
            index = -1;
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value)) return false;
            index = value > 0 ? value - 1 : count + value;
            return index >= 0 && index < count;
        }

        private static void AddVertex(State s, int v, int vt, int vn)
        {
            float x = s.Pos[v * 3];
            s.OutPos.Add(s.Flip ? -x : x);
            s.OutPos.Add(s.Pos[v * 3 + 1]);
            s.OutPos.Add(s.Pos[v * 3 + 2]);
            s.OutUv.Add(vt >= 0 ? s.Uv[vt * 2] : 0f);
            s.OutUv.Add(vt >= 0 ? s.Uv[vt * 2 + 1] : 0f);
            float nx = vn >= 0 ? s.Nor[vn * 3] : 0f;
            s.OutNor.Add(s.Flip ? -nx : nx);
            s.OutNor.Add(vn >= 0 ? s.Nor[vn * 3 + 1] : 0f);
            s.OutNor.Add(vn >= 0 ? s.Nor[vn * 3 + 2] : 0f);
        }
    }
}
