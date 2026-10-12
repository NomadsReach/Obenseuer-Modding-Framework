using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ObenseuerCore
{
    internal sealed class MtlMaterial
    {
        internal string Name;
        internal float[] Kd = new float[] { 1f, 1f, 1f };
        internal string MapKd;
        internal float[] Ke = new float[3];
        internal bool HasTransparency;
    }

    internal sealed class MtlData
    {
        internal readonly List<MtlMaterial> Materials = new List<MtlMaterial>();
        internal readonly List<string> Warnings = new List<string>();
    }

    internal static class MtlParser
    {
        internal const long MaxFileBytes = 1024L * 1024;

        internal static MtlData Parse(string mtlPath, string objFolder)
        {
            MtlData data = new MtlData();
            try
            {
                if (!File.Exists(mtlPath))
                {
                    data.Warnings.Add("mtl file not found: " + mtlPath);
                    return data;
                }
                if (new FileInfo(mtlPath).Length > MaxFileBytes)
                {
                    data.Warnings.Add("mtl file is larger than 1 MB; no materials used");
                    return data;
                }
                return ParseText(File.ReadAllText(mtlPath), Path.GetDirectoryName(mtlPath), objFolder);
            }
            catch (Exception e)
            {
                data.Warnings.Add("mtl not read: " + e.Message);
                return data;
            }
        }

        internal static MtlData ParseText(string text, string mtlFolder, string objFolder)
        {
            MtlData data = new MtlData();
            MtlMaterial current = null;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                string[] t = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (t[0] == "newmtl")
                {
                    current = new MtlMaterial();
                    current.Name = line.Substring(t[0].Length).Trim();
                    data.Materials.Add(current);
                }
                else if (current != null)
                {
                    Statement(data, current, t, mtlFolder, objFolder);
                }
            }
            return data;
        }

        private static void Statement(MtlData data, MtlMaterial m, string[] t, string mtlFolder, string objFolder)
        {
            switch (t[0])
            {
                case "Kd":
                    float[] kd = Color(t);
                    if (kd != null) m.Kd = kd;
                    else data.Warnings.Add("material " + m.Name + ": bad Kd line ignored");
                    break;
                case "Ke":
                    float[] ke = Color(t);
                    if (ke != null) m.Ke = ke;
                    else data.Warnings.Add("material " + m.Name + ": bad Ke line ignored");
                    break;
                case "map_Kd":
                    m.MapKd = MapPath(data, m, t, mtlFolder, objFolder);
                    break;
                case "d":
                    float d;
                    if (float.TryParse(t[t.Length - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d < 1f) Transparent(data, m);
                    break;
                case "Tr":
                    float tr;
                    if (float.TryParse(t[t.Length - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out tr) && tr > 0f) Transparent(data, m);
                    break;
            }
        }

        private static float[] Color(string[] t)
        {
            if (t.Length < 4) return null;
            float[] c = new float[3];
            for (int i = 0; i < 3; i++)
            {
                if (!float.TryParse(t[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out c[i]) || float.IsNaN(c[i]) || float.IsInfinity(c[i])) return null;
            }
            return c;
        }

        private static void Transparent(MtlData data, MtlMaterial m)
        {
            if (m.HasTransparency) return;
            m.HasTransparency = true;
            data.Warnings.Add("material " + m.Name + ": transparency (d or Tr) is not supported in v1; drawn opaque");
        }

        private static string MapPath(MtlData data, MtlMaterial m, string[] t, string mtlFolder, string objFolder)
        {
            string token = t.Length > 1 ? t[t.Length - 1] : "";
            string rel = token.Replace('\\', '/');
            try
            {
                string name = Path.GetFileName(rel);
                if (name.Length > 0 && name != "." && name != ".." && !Path.IsPathRooted(rel))
                {
                    string full = Path.GetFullPath(Path.Combine(mtlFolder, rel));
                    string root = Path.GetFullPath(objFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return full;
                }
            }
            catch (Exception)
            {
            }
            data.Warnings.Add("material " + m.Name + ": map_Kd not used (no file, or outside the model folder): " + token);
            return null;
        }
    }
}
