using System;
using UnityEngine;

namespace ObenseuerCore
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModInfoAttribute : Attribute
    {
        public ModInfoAttribute(string id, string name, string version)
        {
            Id = id;
            Name = name;
            Version = version;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Version { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ModDependencyAttribute : Attribute
    {
        public ModDependencyAttribute(string id)
            : this(id, null)
        {
        }

        public ModDependencyAttribute(string id, string minVersion)
        {
            Id = id;
            MinVersion = minVersion;
        }

        public string Id { get; private set; }

        public string MinVersion { get; private set; }
    }

    public abstract class FrameworkMod : MonoBehaviour
    {
        private ModInfoAttribute info;
        private ModConfig config;

        public ModInfoAttribute Info
        {
            get
            {
                if (info == null) info = (ModInfoAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ModInfoAttribute), false);
                return info;
            }
        }

        public CoreMod Mod
        {
            get { return CoreMod.Of(Info.Id); }
        }

        public CoreLog Log
        {
            get { return CoreLog.For(Info.Id); }
        }

        public ModConfig Config
        {
            get
            {
                if (config == null) config = new ModConfig(Info.Id);
                return config;
            }
        }

        protected abstract void OnLoad();

        internal void RunOnLoad()
        {
            OnLoad();
        }
    }
}
