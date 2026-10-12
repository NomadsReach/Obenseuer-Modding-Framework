using System;
using System.Collections.Generic;
using System.Text;

namespace ObenseuerCore
{
    internal static class ModelSelfTest
    {
        private const string CubeText = "v -1 -1 -1\nv 1 -1 -1\nv 1 1 -1\nv -1 1 -1\nv -1 -1 1\nv 1 -1 1\nv 1 1 1\nv -1 1 1\n" +
            "f 1 2 3 4\nf 5 8 7 6\nf 1 5 6 2\nf 2 6 7 3\nf 3 7 8 4\nf 4 8 5 1\n";
        private const string Header = "v 0 0 0\nv 1 0 0\nv 0 1 0\nvt 0 0\nvt 1 0\nvt 0 1\nvn 0 0 1\n";

        internal static string Run()
        {
            try
            {
                string err;
                ObjData cube = ObjParser.Parse(CubeText, false, out err);
                if (cube == null) return "cube: " + err;
                if (cube.Positions.Length != 24 || cube.SubMeshes.Count != 1 || cube.SubMeshes[0].Triangles.Count != 36) return "cube quads: 8 vertices and 12 triangles expected";

                foreach (string face in new string[] { "f 1 2 3", "f 1/1 2/2 3/3", "f 1//1 2//1 3//1", "f 1/1/1 2/2/1 3/3/1" })
                {
                    ObjData d = ObjParser.Parse(Header + face + "\n", false, out err);
                    if (d == null || !Equal(d.SubMeshes[0].Triangles, 0, 1, 2)) return "face format " + face + ": " + err;
                }

                ObjData neg = ObjParser.Parse(Header + "f -3 -2 -1\n", false, out err);
                if (neg == null || !Equal(neg.SubMeshes[0].Triangles, 0, 1, 2)) return "negative indices: " + err;

                ObjData five = ObjParser.Parse(Header + "f 1 2 3 1 2\nf 1 2 3 1 2\n", false, out err);
                if (five == null || five.SubMeshes[0].Triangles.Count != 18 || five.Warnings.Count != 1) return "5-corner face warning";

                ObjData flip = ObjParser.Parse("v 1 0 0\nv 0 1 0\nv 0 0 1\nvn 1 0 0\nf 1//1 2//1 3//1\n", true, out err);
                if (flip == null || !Equal(flip.SubMeshes[0].Triangles, 0, 2, 1) || flip.Positions[0] != -1f || flip.Normals[0] != -1f) return "flipX winding and X negation: " + err;

                if (ObjParser.Parse(Header + "f 1 2 9\n", false, out err) != null || err == null) return "out of range index not refused";
                if (ObjParser.Parse(Header + "f 0 1 2\n", false, out err) != null || err == null) return "zero index not refused";

                foreach (string bad in new string[] { "v NaN 0 0\n", "v 1e39 0 0\n", "v Infinity 0 0\n", "vt 0 -Infinity\n", "vn 0 0 NaN\n" })
                {
                    if (ObjParser.Parse(bad + Header + "f 1 2 3\n", false, out err) != null || err == null) return "non-finite float not refused: " + bad;
                }

                ObjData partialNormal = ObjParser.Parse(Header + "f 1//1 2//1 3\n", false, out err);
                if (partialNormal == null || partialNormal.Normals.Length != 0 || partialNormal.Warnings.Count != 1 || partialNormal.Positions.Length != 9) return "partial normals not dropped";
                ObjData partialUv = ObjParser.Parse(Header + "f 1/1 2 3\n", false, out err);
                if (partialUv == null || partialUv.Uvs.Length != 0 || partialUv.Warnings.Count != 1) return "partial uvs not dropped";
                ObjData fullNormal = ObjParser.Parse(Header + "f 1//1 2//1 3//1\n", false, out err);
                if (fullNormal == null || fullNormal.Normals.Length != 9 || fullNormal.Warnings.Count != 0) return "full normals dropped";

                ObjData bom = ObjParser.Parse("\uFEFF" + CubeText, false, out err);
                if (bom == null || bom.Positions.Length != 24) return "leading BOM not stripped: " + err;
                ObjData none = ObjParser.Parse(null, false, out err);
                if (none != null || err == null) return "null text not refused";

                if (ObjParser.Parse("v 0 0 0\n" + Fan(2000003), false, out err) != null || err == null) return "single face over the cap not refused";
                if (ObjParser.Parse("v 0 0 0\n" + Fan(1000002) + "\n" + Fan(1000002) + "\n", false, out err) == null) return "two faces at the index cap refused: " + err;
                if (ObjParser.Parse("v 0 0 0\n" + Fan(1000002) + "\n" + Fan(1000002) + "\nf 1 1 1\n", false, out err) != null || err.IndexOf("triangle indices") < 0) return "cumulative index cap not refused";

                string objFolder = "C:\\models\\obj";
                MtlData mtl = MtlParser.ParseText("newmtl a\nmap_Kd ../../evil.png\nnewmtl b\nKd 0.5 0.25 1\nmap_Kd -s 1 1 1 ..\\tex\\a.png\nnewmtl c\nKd NaN 0 0\nKe 0 Infinity 0\nnewmtl d\nmap_Kd\nmap_Kd .\n", objFolder + "\\mats", objFolder);
                List<MtlMaterial> mats = mtl.Materials;
                if (mats.Count != 4 || mats[0].MapKd != null || mtl.Warnings.Count == 0) return "map_Kd escape not rejected";
                if (mats[1].MapKd != objFolder + "\\tex\\a.png" || mats[1].Kd[0] != 0.5f) return "map_Kd inside the folder not resolved";
                if (mats[2].Kd[0] != 1f || mats[2].Ke[1] != 0f || mtl.Warnings.Count < 3) return "non-finite Kd or Ke not refused";
                if (mats[3].MapKd != null) return "map_Kd without a file or a dot not refused";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "model parser self-check ok";
        }

        private static string Fan(int corners)
        {
            StringBuilder b = new StringBuilder("f");
            for (int i = 0; i < corners; i++) b.Append(" 1");
            return b.Append('\n').ToString();
        }

        private static bool Equal(List<int> list, params int[] expected)
        {
            if (list.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (list[i] != expected[i]) return false;
            }
            return true;
        }
    }
}
