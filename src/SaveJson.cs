using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SaveJson
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly SaveContractResolver Resolver = new SaveContractResolver();

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = Resolver,
            Converters = new List<JsonConverter> { new UnityStructConverter() },
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            DateParseHandling = DateParseHandling.None
        };

        internal static string Write(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        internal static T Read<T>(string modId, string json, out bool fellBack) where T : class, new()
        {
            try
            {
                T value = Populate<T>(json);
                Fill(value, new HashSet<object> { value });
                fellBack = false;
                return value;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "save data for " + modId + " read with Newtonsoft", "fell back to JsonUtility, the next save writes it in the Newtonsoft format: " + e.Message);
                fellBack = true;
                return JsonUtility.FromJson<T>(json);
            }
        }

        private static T Populate<T>(string json) where T : class, new()
        {
            T value = new T();
            JsonConvert.PopulateObject(json, value, Settings);
            return value;
        }

        private static void Fill(object target, HashSet<object> seen)
        {
            foreach (FieldInfo field in SavedFields(target.GetType()))
            {
                object value = field.GetValue(target);
                if (value == null && field.FieldType != typeof(string))
                {
                    value = Blank(field.FieldType);
                    if (value != null) field.SetValue(target, value);
                }
                if (value != null) Descend(value, seen);
            }
        }

        private static void Descend(object value, HashSet<object> seen)
        {
            if (value is string || value is UnityEngine.Object || value.GetType().IsValueType || !seen.Add(value)) return;
            IEnumerable items = value as IEnumerable;
            if (items == null)
            {
                Fill(value, seen);
                return;
            }
            foreach (object item in items)
            {
                if (item != null) Descend(item, seen);
            }
        }

        private static bool Blankable(Type type)
        {
            return type == typeof(string) || type.IsArray || IsBlankClass(type);
        }

        private static object Blank(Type type)
        {
            if (!Blankable(type)) return null;
            if (type == typeof(string)) return "";
            if (type.IsArray) return Array.CreateInstance(type.GetElementType(), new int[type.GetArrayRank()]);
            return Activator.CreateInstance(type);
        }

        private static bool IsBlankClass(Type type)
        {
            return type.IsClass && !type.IsAbstract && type != typeof(object) && type.IsDefined(typeof(SerializableAttribute), false) && type.GetConstructor(Type.EmptyTypes) != null
                && !typeof(UnityEngine.Object).IsAssignableFrom(type) && !Reaches(type, type, new HashSet<Type>());
        }

        private static bool Reaches(Type from, Type target, HashSet<Type> seen)
        {
            if (!seen.Add(from)) return false;
            foreach (FieldInfo field in SavedFields(from))
            {
                Type next = field.FieldType;
                if (next == target || (next.IsClass && !next.IsArray && next != typeof(string) && next != typeof(object) && Reaches(next, target, seen))) return true;
            }
            return false;
        }

        private static List<FieldInfo> SavedFields(Type type)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            HashSet<string> names = new HashSet<string>();
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(Members | BindingFlags.DeclaredOnly))
                {
                    if (IsSaved(field) && names.Add(field.Name)) fields.Add(field);
                }
            }
            return fields;
        }

        private static bool IsSaved(FieldInfo field)
        {
            Type owner = field.DeclaringType;
            return !field.IsDefined(typeof(NonSerializedAttribute), false) && (field.IsPublic || field.IsDefined(typeof(SerializeField), false) || (owner.IsValueType && owner.Namespace == "UnityEngine")) && !HoldsUnityObject(field.FieldType);
        }

        private static bool HoldsUnityObject(Type type)
        {
            if (type.IsArray) return HoldsUnityObject(type.GetElementType());
            if (type.IsGenericType)
            {
                foreach (Type argument in type.GetGenericArguments())
                {
                    if (HoldsUnityObject(argument)) return true;
                }
            }
            return typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        private sealed class SaveContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                List<JsonProperty> properties = new List<JsonProperty>();
                foreach (FieldInfo field in SavedFields(type))
                {
                    JsonProperty property = CreateProperty(field, MemberSerialization.Fields);
                    property.ValueProvider = new BlankProvider(property.ValueProvider, property.PropertyType);
                    if (Blankable(property.PropertyType)) property.NullValueHandling = NullValueHandling.Ignore;
                    properties.Add(property);
                }
                return properties;
            }
        }

        private sealed class BlankProvider : IValueProvider
        {
            private readonly IValueProvider inner;
            private readonly Type type;

            internal BlankProvider(IValueProvider inner, Type type)
            {
                this.inner = inner;
                this.type = type;
            }

            public object GetValue(object target)
            {
                return inner.GetValue(target) ?? Blank(type);
            }

            public void SetValue(object target, object value)
            {
                inner.SetValue(target, value);
            }
        }

        private sealed class UnityStructConverter : JsonConverter
        {
            private static readonly Dictionary<Type, string[]> Names = new Dictionary<Type, string[]>
            {
                { typeof(Vector2), new[] { "x", "y" } },
                { typeof(Vector3), new[] { "x", "y", "z" } },
                { typeof(Vector4), new[] { "x", "y", "z", "w" } },
                { typeof(Quaternion), new[] { "x", "y", "z", "w" } },
                { typeof(Color), new[] { "r", "g", "b", "a" } },
                { typeof(Rect), new[] { "x", "y", "width", "height" } },
                { typeof(Matrix4x4), Cells("e") }
            };

            private static readonly Dictionary<Type, string[]> Fields = new Dictionary<Type, string[]>
            {
                { typeof(Rect), new[] { "m_XMin", "m_YMin", "m_Width", "m_Height" } },
                { typeof(Matrix4x4), Cells("m") }
            };

            public override bool CanConvert(Type objectType)
            {
                return Names.ContainsKey(objectType);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Type type = value.GetType();
                string[] names = Names[type];
                string[] fields = FieldNames(type);
                writer.WriteStartObject();
                for (int i = 0; i < names.Length; i++)
                {
                    writer.WritePropertyName(names[i]);
                    writer.WriteValue((float)type.GetField(fields[i], Members).GetValue(value));
                }
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return Activator.CreateInstance(objectType);
                JObject json = JObject.Load(reader);
                string[] names = Names[objectType];
                string[] fields = FieldNames(objectType);
                object value = Activator.CreateInstance(objectType);
                for (int i = 0; i < names.Length; i++)
                {
                    JToken part = json[names[i]] ?? json[fields[i]];
                    if (part != null) objectType.GetField(fields[i], Members).SetValue(value, (float)part);
                }
                return value;
            }

            private static string[] FieldNames(Type type)
            {
                string[] fields;
                return Fields.TryGetValue(type, out fields) ? fields : Names[type];
            }

            private static string[] Cells(string prefix)
            {
                string[] cells = new string[16];
                for (int i = 0; i < 16; i++) cells[i] = prefix + i / 4 + i % 4;
                return cells;
            }
        }
    }
}
