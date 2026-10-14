using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.Mono;
using UnityEngine;

namespace ObenseuerCore
{
    /// <summary>
    /// A per-mod handle that gives save data, keybinds, logging and patch helpers under one mod id.
    /// </summary>
    public sealed class CoreMod
    {
        private static readonly Dictionary<string, CoreMod> handles = new Dictionary<string, CoreMod>(StringComparer.OrdinalIgnoreCase);

        private CoreMod(string id)
        {
            Id = id;
        }

        /// <summary>
        /// The mod id (the plugin GUID) this handle belongs to.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The log for this mod, written to the mod's own log file.
        /// </summary>
        public CoreLog Log
        {
            get { return CoreLog.For(Id); }
        }

        /// <summary>
        /// Returns the handle for a plugin's GUID, creating it on first use and returning the same handle after that.
        /// </summary>
        public static CoreMod For(BaseUnityPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException("plugin");
            return Of(plugin.Info.Metadata.GUID);
        }

        internal static CoreMod Of(string id)
        {
            ModIds.Require(id, "id");
            CoreMod handle;
            if (!handles.TryGetValue(id, out handle))
            {
                handle = new CoreMod(id);
                handles[id] = handle;
            }
            return handle;
        }

        /// <summary>
        /// Registers this mod's save data under the key, saved to a file named after the mod and the key.
        /// </summary>
        public void Save<T>(string key, Func<T> save, Action<T> load) where T : class, new()
        {
            SaveData.Register(SaveData.EntryName(Id, key), save, load);
        }

        /// <summary>
        /// Registers a keybind for this mod, returning the same keybind as Keybinds.Register.
        /// </summary>
        public ModKeybind Key(string id, string displayName, KeyCode defaultKey)
        {
            return Keybinds.Register(Id, id, displayName, defaultKey);
        }

        /// <summary>
        /// Adds a prefix to the named method, found by name, under this mod's owner.
        /// </summary>
        public bool Prefix(Type target, string method, Type handlerType, string handlerName)
        {
            return CorePatcher.Prefix(target, method, handlerType, handlerName, Id);
        }

        /// <summary>
        /// Adds a prefix to the method with the given parameter types, under this mod's owner.
        /// </summary>
        public bool Prefix(Type target, string method, Type[] args, Type handlerType, string handlerName)
        {
            return CorePatcher.Prefix(target, method, args, handlerType, handlerName, Id);
        }

        /// <summary>
        /// Adds a postfix to the named method, found by name, under this mod's owner.
        /// </summary>
        public bool Postfix(Type target, string method, Type handlerType, string handlerName)
        {
            return CorePatcher.Postfix(target, method, handlerType, handlerName, Id);
        }

        /// <summary>
        /// Adds a postfix to the method with the given parameter types, under this mod's owner.
        /// </summary>
        public bool Postfix(Type target, string method, Type[] args, Type handlerType, string handlerName)
        {
            return CorePatcher.Postfix(target, method, args, handlerType, handlerName, Id);
        }

        /// <summary>
        /// Adds a finalizer to the named method, found by name, under this mod's owner.
        /// </summary>
        public bool Finalizer(Type target, string method, Type handlerType, string handlerName)
        {
            return CorePatcher.Finalizer(target, method, handlerType, handlerName, Id);
        }

        /// <summary>
        /// Adds a finalizer to the method with the given parameter types, under this mod's owner.
        /// </summary>
        public bool Finalizer(Type target, string method, Type[] args, Type handlerType, string handlerName)
        {
            return CorePatcher.Finalizer(target, method, args, handlerType, handlerName, Id);
        }
    }
}
