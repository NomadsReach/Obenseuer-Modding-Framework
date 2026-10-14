using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SaveJsonSelfTest
    {
        internal static string Run()
        {
            try
            {
                ProbeData sample = ProbeData.Sample();
                string json = SaveJson.Write(sample);
                if (Has(json, "\"Derived\"") || Has(json, "\"skipped\"")) return "a property or a [NonSerialized] field was written";
                if (!Has(json, "\"Note\":\"\"")) return "a null string was not written as an empty string";
                if (!Has(json, "\"Position\":{\"x\":1.0,\"y\":2.5,\"z\":-3.0}")) return "a Vector3 was not written as x, y, z";
                ProbeData back = ReadNewtonsoft<ProbeData>(json);
                if (back.Name != sample.Name) return "a date-like string changed on the way back";
                if (back.Note != "") return "an empty string did not come back as an empty string";
                if (back.Inner == null || back.Inner.Count != 4) return "a nested class did not come back";
                if (back.Items == null || back.Items.Length != 2 || back.Items[1].Count != 2) return "an array of classes did not come back";
                if (back.Rows.Count != 1 || back.Rows[0].Count != 5) return "a List of classes did not come back";
                if (back.Position != sample.Position) return "a Vector3 did not come back";
                if (back.Secret != 7) return "a [SerializeField] private field did not come back";
                if (back.skipped != 0 || back.Derived != 0) return "a [NonSerialized] field or a property came back";
                const string legacy = "{\"Name\":\"legacy\",\"Note\":\"\",\"Inner\":{\"Count\":5},\"Items\":[{\"Count\":6}],\"Rows\":[{\"Count\":7}],\"Position\":{\"x\":1,\"y\":2,\"z\":3},\"secret\":8}";
                ProbeData old = ReadNewtonsoft<ProbeData>(legacy);
                if (old.Name != "legacy" || old.Inner.Count != 5 || old.Items[0].Count != 6 || old.Rows[0].Count != 7) return "a JsonUtility-shaped file did not load";
                if (old.Position != new Vector3(1f, 2f, 3f) || old.Secret != 8) return "a JsonUtility-shaped file did not load its numbers";
                if (old.Level != 3) return "a field missing from the file lost its initial value";

                ProbeNulls nulls = new ProbeNulls();
                nulls.Area = new Rect(1f, 2f, 3f, 4f);
                string nullJson = SaveJson.Write(nulls);
                if (!Has(nullJson, "\"Names\":[]") || !Has(nullJson, "\"Rows\":[]")) return "a null array or List was not written as an empty value";
                if (!Has(nullJson, "\"Part\":{\"Count\":0}") || !Has(nullJson, "\"Kept\":{\"Count\":9}")) return "a null class was not written as a default instance";
                if (!Has(nullJson, "\"Link\":null")) return "a class that contains itself was not written as null";
                if (Has(nullJson, "\"Target\"")) return "a UnityEngine.Object field was written";
                if (!Has(nullJson, "\"Area\":{\"x\":1.0,\"y\":2.0,\"width\":3.0,\"height\":4.0}")) return "a Rect was not written as x, y, width, height";
                ProbeNulls filled = ReadNewtonsoft<ProbeNulls>(nullJson);
                if (filled.Names == null || filled.Names.Length != 0 || filled.Rows == null || filled.Rows.Count != 0 || filled.Part == null || filled.Kept.Count != 9) return "a null array, List or class did not come back non-null";
                if (filled.Link != null || filled.Area.width != 3f) return "a class that contains itself was filled, or a Rect did not come back";
                ProbeNulls legacyNulls = ReadNewtonsoft<ProbeNulls>("{\"Area\":{\"x\":1,\"y\":2,\"width\":3,\"height\":4},\"Grid\":{\"e00\":5,\"e11\":6},\"Position\":null,\"Names\":null,\"Rows\":null,\"Part\":null,\"Kept\":null}");
                if (legacyNulls.Area.x != 1f || legacyNulls.Area.y != 2f || legacyNulls.Area.width != 3f || legacyNulls.Area.height != 4f) return "a Rect saved as x, y, width, height did not load";
                if (legacyNulls.Grid.m00 != 5f || legacyNulls.Grid.m11 != 6f) return "a Matrix4x4 saved as e00 to e33 did not load";
                if (legacyNulls.Position != Vector3.zero) return "a null Vector3 token did not read as zero";
                if (legacyNulls.Names == null || legacyNulls.Rows == null || legacyNulls.Part == null || legacyNulls.Kept.Count != 9) return "an explicit null collection or class did not end non-null, or overwrote its initializer";
                ProbeNulls named = ReadNewtonsoft<ProbeNulls>("{\"Area\":{\"m_XMin\":1,\"m_YMin\":2,\"m_Width\":3,\"m_Height\":4}}");
                if (named.Area.x != 1f || named.Area.width != 3f) return "a Rect saved under its field names did not load";
                if (named.Names == null || named.Rows == null || named.Part == null || named.Kept.Count != 9) return "a missing collection or class did not end non-null, or lost its initializer";
                ProbeNulls withTarget = ReadNewtonsoft<ProbeNulls>("{\"Target\":{\"name\":\"x\"}}");
                if (withTarget.Target != null) return "a UnityEngine.Object field was read";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "save json self-check ok";
        }

        internal static string Probe()
        {
            try
            {
                ProbeData sample = ProbeData.Sample();
                return "JsonUtility: " + JsonUtility.ToJson(sample) + "\nSaveData: " + SaveJson.Write(sample);
            }
            catch (Exception e)
            {
                return "json probe failed: " + e.Message;
            }
        }

        private static bool Has(string text, string part)
        {
            return text.IndexOf(part, StringComparison.Ordinal) >= 0;
        }

        private static T ReadNewtonsoft<T>(string json) where T : class, new()
        {
            bool fellBack;
            T value = SaveJson.Read<T>("save json self-check", json, out fellBack);
            if (fellBack) throw new InvalidOperationException("fell back to JsonUtility");
            return value;
        }

        [Serializable]
        public sealed class ProbePart
        {
            public int Count;
        }

        [Serializable]
        public sealed class ProbeData
        {
            public int Level = 3;
            public string Name;
            public string Note = null;
            public ProbePart Inner;
            public ProbePart[] Items;
            public List<ProbePart> Rows = new List<ProbePart>();
            public Vector3 Position;
            [SerializeField] private int secret;
            [NonSerialized] public int skipped;
            public int Derived { get; set; }

            internal int Secret
            {
                get { return secret; }
            }

            internal static ProbeData Sample()
            {
                ProbeData data = new ProbeData();
                data.Name = "2026-10-12";
                data.Inner = new ProbePart { Count = 4 };
                data.Items = new[] { new ProbePart { Count = 1 }, new ProbePart { Count = 2 } };
                data.Rows.Add(new ProbePart { Count = 5 });
                data.Position = new Vector3(1f, 2.5f, -3f);
                data.secret = 7;
                data.skipped = 9;
                data.Derived = 6;
                return data;
            }
        }

        [Serializable]
        public sealed class ProbeLink
        {
            public ProbeLink Next = null;
        }

        [Serializable]
        public sealed class ProbeNulls
        {
            public string[] Names = null;
            public List<ProbePart> Rows = null;
            public ProbePart Part = null;
            public ProbePart Kept = new ProbePart { Count = 9 };
            public ProbeLink Link = null;
            public Vector3 Position = new Vector3(4f, 5f, 6f);
            public Rect Area = new Rect();
            public Matrix4x4 Grid = new Matrix4x4();
            public GameObject Target = null;
        }
    }
}
