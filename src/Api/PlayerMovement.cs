using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public static class PlayerMovement
    {
        internal const string Owner = "obenseuer.moddingframework.playermovement";
        private const float MinMultiplier = 0.25f;
        private const float MaxMultiplier = 4f;
        private static readonly FieldInfo walkingField = AccessTools.Field(typeof(OpenSewerCharacterController), "walking");
        private static readonly FieldInfo moveDisabledField = AccessTools.Field(typeof(ThirdPersonCameraController), "moveDisabled");
        private static readonly FieldInfo firstpersonMoveDisabledField = AccessTools.Field(typeof(ThirdPersonCameraController), "firstpersonMoveDisabled");
        private static readonly Dictionary<string, int> locks = new Dictionary<string, int>();
        private static GameObject holder;
        private static GameController lockedGame;
        private static int lockCount;
        private static bool installed;
        private static float walkMultiplier = 1f;
        private static float jumpMultiplier = 1f;
        private static OpenSewerCharacterController applied;
        private static float baseWalkSpeed;
        private static float baseJumpHeight;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool IsGrounded
        {
            get
            {
                OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
                return controller != null && controller.movement != null && controller.isGrounded;
            }
        }

        public static bool IsCrouching
        {
            get
            {
                OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
                return controller != null && controller.isCrouching;
            }
        }

        public static bool IsSprinting
        {
            get
            {
                OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
                if (controller == null || walkingField == null) return false;
                InputManager input = InputManager.instance;
                bool controllerInput = input != null && input.inputType == InputManager.InputType.Controller;
                bool walking = (bool)walkingField.GetValue(controller);
                return (!walking || controllerInput) && !controller.isCrouching;
            }
        }

        public static Vector3 Velocity
        {
            get
            {
                OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
                return controller != null && controller.movement != null ? controller.movement.velocity : Vector3.zero;
            }
        }

        public static Vector3 Position
        {
            get
            {
                OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
                return controller != null ? controller.transform.position : Vector3.zero;
            }
        }

        public static float WalkSpeedMultiplier
        {
            get
            {
                return walkMultiplier;
            }
            set
            {
                walkMultiplier = Clamp(value);
                ApplyTo(OpenSewerCharacterController.instance);
            }
        }

        public static float JumpMultiplier
        {
            get
            {
                return jumpMultiplier;
            }
            set
            {
                jumpMultiplier = Clamp(value);
                ApplyTo(OpenSewerCharacterController.instance);
            }
        }

        public static bool Lock(string modId)
        {
            ModIds.Require(modId, "modId");
            GameController game = GameController.instance;
            if (game == null || SaveGate.Busy || OpenSewerCharacterController.instance == null || ThirdPersonCameraController.instance == null || ThirdPersonCameraCollision.instance == null) return false;
            try
            {
                if (lockCount == 0)
                {
                    if (holder == null)
                    {
                        holder = new GameObject("ObenseuerCore.PlayerMovement");
                        UnityEngine.Object.DontDestroyOnLoad(holder);
                    }
                    lockedGame = game;
                    game.ControlsDisabled(holder);
                }
                int count;
                locks.TryGetValue(modId, out count);
                locks[modId] = count + 1;
                lockCount++;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.Lock", e);
                ReleaseGame();
                return false;
            }
        }

        public static bool Unlock(string modId)
        {
            ModIds.Require(modId, "modId");
            int count;
            if (!locks.TryGetValue(modId, out count)) return false;
            if (count > 1) locks[modId] = count - 1;
            else locks.Remove(modId);
            lockCount--;
            if (lockCount == 0) ReleaseGame();
            return true;
        }

        internal static void Init()
        {
            installed = walkingField != null && PlayerMovementPatches.Install();
            if (!installed)
            {
                CorePatcher.DisableOwner(Owner);
                CoreLog.For(Owner).Warn("controller hooks incomplete, speed and jump multipliers are disabled");
            }
            Events.SceneLoaded += OnSceneLoaded;
            Events.Loaded += OnLoaded;
        }

        internal static void ApplyTo(OpenSewerCharacterController controller)
        {
            if (!IsAvailable || controller == null) return;
            try
            {
                if (controller != applied)
                {
                    applied = controller;
                    baseWalkSpeed = controller.walkSpeed;
                    baseJumpHeight = controller.baseJumpHeight;
                }
                controller.walkSpeed = baseWalkSpeed * walkMultiplier;
                controller.baseJumpHeight = baseJumpHeight * jumpMultiplier;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.ApplyTo", e);
            }
        }

        private static float Clamp(float value)
        {
            if (float.IsNaN(value)) throw new ArgumentException("multiplier must be a number", "value");
            return Mathf.Clamp(value, MinMultiplier, MaxMultiplier);
        }

        private static void Restore()
        {
            if (applied != null)
            {
                applied.walkSpeed = baseWalkSpeed;
                applied.baseJumpHeight = baseJumpHeight;
            }
            applied = null;
        }

        private static void ReleaseAll()
        {
            if (lockCount == 0) return;
            locks.Clear();
            lockCount = 0;
            ReleaseGame();
            lockedGame = null;
        }

        private static void ReleaseGame()
        {
            if (lockedGame == null || holder == null) return;
            ThirdPersonCameraController camera = ThirdPersonCameraController.instance;
            bool restoreLooks = camera != null && moveDisabledField != null && firstpersonMoveDisabledField != null;
            bool moveDisabled = restoreLooks && (bool)moveDisabledField.GetValue(camera);
            bool firstpersonMoveDisabled = restoreLooks && (bool)firstpersonMoveDisabledField.GetValue(camera);
            try
            {
                lockedGame.ControlsEnabled(holder);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.Release", e);
            }
            if (restoreLooks)
            {
                moveDisabledField.SetValue(camera, moveDisabled);
                firstpersonMoveDisabledField.SetValue(camera, firstpersonMoveDisabled);
            }
            lockedGame = null;
        }

        private static void OnSceneLoaded(string ignored)
        {
            if (lockedGame == null || lockedGame != GameController.instance) ReleaseAll();
            Restore();
            ApplyTo(OpenSewerCharacterController.instance);
        }

        private static void OnLoaded()
        {
            ReleaseAll();
        }
    }
}
