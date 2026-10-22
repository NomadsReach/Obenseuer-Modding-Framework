using System;
using System.Reflection;
using HarmonyLib;
using ObenseuerCore.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal static class DevMenuNoClip
    {
        private static readonly FieldInfo consoleFlag = AccessTools.Field(typeof(DeveloperConsoleRoutines), "_shouldDisableClipping");
        private static ModToggle toggle;
        private static bool switchedOn;

        internal static void Init()
        {
            Events.SceneLoadedWithMode += SceneChanged;
        }

        internal static void Build(RectTransform card)
        {
            ModToggle created = null;
            created = ModUi.Toggle(card, "No clip", IsOn(), value => Set(created, value));
            toggle = created;
        }

        private static void Set(ModToggle which, bool on)
        {
            if (!DevMenu.Act("No clip", on ? "on" : "off", () => Apply(on))) which.Set(IsOn());
        }

        private static bool Apply(bool on)
        {
            DeveloperMovement movement = DeveloperMovement.Instance;
            if (movement == null)
            {
                ApiLog.Once(DevMenu.Owner, "no clip without debug movement", "the game's debug movement is not in this scene");
                return false;
            }
            if (DeveloperConsole.Instance == null)
            {
                ApiLog.Once(DevMenu.Owner, "no clip without the developer console", "the game's developer console is not loaded");
                return false;
            }
            try
            {
                if (on) movement.ActivateDebugMovement();
                else movement.DeactivateDebugMovement();
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "no clip", e);
                return false;
            }
            SetConsoleFlag(on);
            switchedOn = IsOn();
            return switchedOn == on;
        }

        private static void SceneChanged(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (switchedOn)
            {
                switchedOn = false;
                SwitchOff(scene);
            }
            Refresh();
        }

        private static void SwitchOff(string scene)
        {
            DeveloperMovement movement = DeveloperMovement.Instance;
            if (movement != null && movement.shouldHandleMovement)
            {
                try
                {
                    movement.DeactivateDebugMovement();
                }
                catch (Exception e)
                {
                    ApiLog.Once(DevMenu.Owner, "no clip at a scene change", e);
                }
            }
            SetConsoleFlag(false);
            ReleaseControls();
            CoreLog.For(DevMenu.Owner).Info("no clip switched off by the scene change to " + scene);
        }

        private static void ReleaseControls()
        {
            DeveloperMovement movement = DeveloperMovement.Instance;
            if (movement == null || !movement.shouldHandleMovement) return;
            GameController controller = GameController.instance;
            if (controller == null) return;
            try
            {
                controller.ControlsEnabledMove(controller.gameObject, false, true);
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "no clip controls at a scene change", e);
            }
        }

        private static void Refresh()
        {
            ModToggle current = toggle;
            if (current == null) return;
            if (current.Rect == null)
            {
                toggle = null;
                return;
            }
            current.Set(IsOn());
        }

        private static void SetConsoleFlag(bool on)
        {
            if (consoleFlag == null)
            {
                ApiLog.Once(DevMenu.Owner, "no clip without the console flag", "the game's noclip command keeps its own state and is now out of step with this toggle");
                return;
            }
            consoleFlag.SetValue(null, !on);
        }

        private static bool IsOn()
        {
            DeveloperMovement movement = DeveloperMovement.Instance;
            return movement != null && movement.shouldHandleMovement;
        }
    }
}
