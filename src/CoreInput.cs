using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class CoreInput
    {
        private const string Owner = "ObenseuerCore";
        private const string ConfigurationManagerGuid = "com.bepis.bepinex.configurationmanager";
        private static readonly HashSet<object> owners = new HashSet<object>();
        private static GameObject holder;
        private static bool held;
        private static bool configSearched;
        private static PropertyInfo configShown;

        internal static void BlockGameplay(object owner)
        {
            owners.Add(owner);
        }

        internal static void Unblock(object owner)
        {
            owners.Remove(owner);
        }

        internal static bool GameBusy()
        {
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (view == null || view.controlsDisabled) return true;
            if (GameUIController.instance != null && GameUIController.instance.IsMenuVisible) return true;
            if (ShopController.instance != null && ShopController.instance.menuIsOpen) return true;
            if (PauseMenu.instance != null && PauseMenu.instance.PauseIsActive()) return true;
            if (DialogueController.instance != null && DialogueController.instance.DialogueActive) return true;
            return DeveloperConsole.Instance != null && DeveloperConsole.Instance.consoleOpened;
        }

        internal static void Update()
        {
            try
            {
                FindConfigWindow();
                if (owners.Count == 0)
                {
                    if (held) Release();
                }
                else if (!held && !GameBusy())
                {
                    Hold();
                }
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "CoreInput.Update", e);
            }
        }

        private static void Hold()
        {
            GameController game = GameController.instance;
            if (game == null || ThirdPersonCameraController.instance == null) return;
            if (holder == null)
            {
                holder = new GameObject("ObenseuerCore.CoreInput");
                UnityEngine.Object.DontDestroyOnLoad(holder);
            }
            held = true;
            game.ControlsDisabled(holder, ShowCursor: true);
        }

        private static void Release()
        {
            held = false;
            GameController game = GameController.instance;
            if (game != null) game.ControlsEnabled(holder);
        }

        private static void FindConfigWindow()
        {
            if (configSearched) return;
            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(ConfigurationManagerGuid, out info) || info.Instance == null) return;
            configSearched = true;
            object window = info.Instance;
            Type type = window.GetType();
            PropertyInfo shown = type.GetProperty("DisplayingWindow", BindingFlags.Instance | BindingFlags.Public);
            EventInfo changed = type.GetEvent("DisplayingWindowChanged", BindingFlags.Instance | BindingFlags.Public);
            if (shown == null || changed == null || shown.PropertyType != typeof(bool))
            {
                CoreLog.For(Owner).Warn("ConfigurationManager has no DisplayingWindow, F1 does not block gameplay");
                return;
            }
            configShown = shown;
            MethodInfo handler = typeof(CoreInput).GetMethod("OnConfigWindowChanged", BindingFlags.Static | BindingFlags.NonPublic);
            changed.AddEventHandler(window, Delegate.CreateDelegate(changed.EventHandlerType, null, handler));
            OnConfigWindowChanged(window, EventArgs.Empty);
        }

        private static void OnConfigWindowChanged(object window, object args)
        {
            try
            {
                if ((bool)configShown.GetValue(window, null))
                {
                    BlockGameplay(window);
                }
                else
                {
                    Unblock(window);
                }
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("config window hook failed: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
