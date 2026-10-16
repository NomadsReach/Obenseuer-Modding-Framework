using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class PlayerMovementInfo
    {
        public float WalkSpeed { get; private set; }

        public float RunSpeed { get; private set; }

        public float SwimmingSpeed { get; private set; }

        public float CrouchSpeed { get; private set; }

        public float BackwardSpeed { get; private set; }

        public float StrafeSpeed { get; private set; }

        public bool IsCrouching { get; private set; }

        public bool IsWalking { get; private set; }

        public bool IsGrounded { get; private set; }

        public bool ControlsDisabled { get; private set; }

        internal PlayerMovementInfo(float walkSpeed, float runSpeed, float swimmingSpeed, float crouchSpeed, float backwardSpeed, float strafeSpeed, bool isCrouching, bool isWalking, bool isGrounded, bool controlsDisabled)
        {
            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            SwimmingSpeed = swimmingSpeed;
            CrouchSpeed = crouchSpeed;
            BackwardSpeed = backwardSpeed;
            StrafeSpeed = strafeSpeed;
            IsCrouching = isCrouching;
            IsWalking = isWalking;
            IsGrounded = isGrounded;
            ControlsDisabled = controlsDisabled;
        }
    }

    public static class PlayerMovement
    {
        internal const string Owner = "obenseuer.moddingframework.playermovement";
        private const float MinMultiplier = 0.25f;
        private const float MaxMultiplier = 4f;
        private static readonly FieldInfo walkingField = AccessTools.Field(typeof(OpenSewerCharacterController), "walking");
        private static readonly FieldInfo runSpeedField = AccessTools.Field(typeof(OpenSewerCharacterController), "_runSpeed");
        private static readonly FieldInfo swimmingSpeedField = AccessTools.Field(typeof(OpenSewerCharacterController), "_swimmingSpeed");
        private static readonly FieldInfo walkSpeedField = AccessTools.Field(typeof(OpenSewerCharacterController), "_walkSpeed");
        private static readonly FieldInfo crouchSpeedField = AccessTools.Field(typeof(OpenSewerCharacterController), "_crouchSpeed");
        private static readonly FieldInfo backwardSpeedField = AccessTools.Field(typeof(OpenSewerCharacterController), "_backwardSpeed");
        private static readonly FieldInfo strafeSpeedField = AccessTools.Field(typeof(OpenSewerCharacterController), "_strafeSpeed");
        private static readonly FieldInfo baseJumpHeightField = AccessTools.Field(typeof(ECM.Controllers.BaseCharacterController), "_baseJumpHeight");
        private static readonly FieldInfo moveDisabledField = AccessTools.Field(typeof(ThirdPersonCameraController), "moveDisabled");
        private static readonly FieldInfo firstpersonMoveDisabledField = AccessTools.Field(typeof(ThirdPersonCameraController), "firstpersonMoveDisabled");
        private static readonly FieldInfo firstpersonSoftLockField = AccessTools.Field(typeof(ThirdPersonCameraController), "firstpersonSoftLock");
        private static readonly FieldInfo lookAtTargetField = AccessTools.Field(typeof(ThirdPersonCameraController), "lookAtTarget");
        private static readonly List<string> lookLocks = new List<string>();
        private static bool gameRotationOff;
        private static bool ownRotationCall;
        private static bool lookPatched;
        private static readonly List<KeyValuePair<string, Transform>> holds = new List<KeyValuePair<string, Transform>>();
        private static GameObject holder;
        private static GameController lockedGame;
        private static bool lookLocked;
        private static bool softLockBefore;
        private static Transform targetBefore;
        private static Transform appliedTarget;
        private static bool installed;
        private static readonly Dictionary<string, float> walkMultipliers = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> jumpMultipliers = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> speedMultipliers = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> runMultipliers = new Dictionary<string, float>();
        private static OpenSewerCharacterController applied;
        private static float baseWalkSpeed;
        private static float baseRunSpeed;
        private static float baseSwimmingSpeed;
        private static float baseCrouchSpeed;
        private static float baseBackwardSpeed;
        private static float baseStrafeSpeed;
        private static float baseJumpHeight;
        private static readonly HashSet<string> pendingOwners = new HashSet<string>();

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
                return Own(walkMultipliers);
            }
            set
            {
                walkMultipliers[string.Empty] = Clamp(value);
                ApplyTo(OpenSewerCharacterController.instance);
            }
        }

        public static float JumpMultiplier
        {
            get
            {
                return Own(jumpMultipliers);
            }
            set
            {
                jumpMultipliers[string.Empty] = Clamp(value);
                ApplyTo(OpenSewerCharacterController.instance);
            }
        }

        public static PlayerMovementInfo State()
        {
            OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
            if (controller == null || walkingField == null) return null;
            try
            {
                bool walking = (bool)walkingField.GetValue(controller);
                bool shrooms = OrangeMushroomEffect.instance != null;
                float run = shrooms ? controller.runSpeed : (float)runSpeedField.GetValue(controller);
                float swim = shrooms ? controller.swimmingSpeed : (float)swimmingSpeedField.GetValue(controller);
                return new PlayerMovementInfo(controller.walkSpeed, run, swim, controller.crouchSpeed, controller.backwardSpeed, controller.strafeSpeed, controller.isCrouching, walking, IsGrounded, controller.ControlsDisabled());
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.State", e);
                return null;
            }
        }

        public static bool SetSpeedMultiplier(float multiplier)
        {
            return SetMultiplier(speedMultipliers, string.Empty, multiplier);
        }

        public static bool SetSpeedMultiplier(string owner, float multiplier)
        {
            ModIds.Require(owner, "owner");
            return SetMultiplier(speedMultipliers, owner, multiplier);
        }

        public static bool SetWalkMultiplier(string owner, float multiplier)
        {
            ModIds.Require(owner, "owner");
            return SetMultiplier(walkMultipliers, owner, multiplier);
        }

        public static bool SetRunMultiplier(string owner, float multiplier)
        {
            ModIds.Require(owner, "owner");
            return SetMultiplier(runMultipliers, owner, multiplier);
        }

        public static bool SetJumpMultiplier(string owner, float multiplier)
        {
            ModIds.Require(owner, "owner");
            return SetMultiplier(jumpMultipliers, owner, multiplier);
        }

        public static bool ClearMultipliers(string owner)
        {
            ModIds.Require(owner, "owner");
            bool removed = walkMultipliers.Remove(owner) | jumpMultipliers.Remove(owner) | speedMultipliers.Remove(owner) | runMultipliers.Remove(owner);
            if (removed) ApplyTo(OpenSewerCharacterController.instance);
            return removed;
        }

        private static bool SetMultiplier(Dictionary<string, float> slot, string key, float multiplier)
        {
            if (!IsAvailable || float.IsNaN(multiplier) || float.IsInfinity(multiplier)) return false;
            slot[key] = Clamp(multiplier);
            OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
            if (controller == null && key.Length > 0) pendingOwners.Add(key);
            ApplyTo(controller);
            return true;
        }

        private static float Own(Dictionary<string, float> slot)
        {
            float value;
            return slot.TryGetValue(string.Empty, out value) ? value : 1f;
        }

        private static void ForgetOwners(Dictionary<string, float> slot)
        {
            float own = Own(slot);
            foreach (string key in new List<string>(slot.Keys))
            {
                if (key.Length != 0 && !pendingOwners.Contains(key)) slot.Remove(key);
            }
            slot[string.Empty] = own;
        }

        private static float Product(Dictionary<string, float> slot)
        {
            float result = 1f;
            foreach (float value in slot.Values) result *= value;
            return Mathf.Clamp(result, MinMultiplier, MaxMultiplier);
        }

        public static bool Crouch()
        {
            OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
            if (!IsAvailable || controller == null || !controller.isActiveAndEnabled || SaveGate.Busy) return false;
            try
            {
                if (!controller.isCrouching) controller.Crouch();
                return controller.isCrouching;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.Crouch", e);
                return false;
            }
        }

        public static bool StandUp()
        {
            OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
            if (!IsAvailable || controller == null || !controller.isActiveAndEnabled || SaveGate.Busy) return false;
            try
            {
                if (!controller.isCrouching) return true;
                controller.StandUp();
                if (!controller.isCrouching) return true;
                controller.Crouch();
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.StandUp", e);
                return false;
            }
        }

        public static bool Lock(string modId)
        {
            return Hold(modId, null, false);
        }

        public static bool LockLookAt(string modId, Transform target, bool instantly)
        {
            ModIds.Require(modId, "modId");
            return target != null && firstpersonSoftLockField != null && lookAtTargetField != null && Hold(modId, target, instantly);
        }

        public static bool Unlock(string modId)
        {
            ModIds.Require(modId, "modId");
            int index = holds.FindLastIndex(h => h.Key == modId);
            if (index < 0) return false;
            bool look = !object.ReferenceEquals(holds[index].Value, null);
            holds.RemoveAt(index);
            if (holds.Count == 0) ReleaseGame();
            else if (look) RefreshLook();
            return true;
        }

        public static bool LockLook(string modId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy || !lookPatched || ThirdPersonCameraController.instance == null) return false;
            lookLocks.Add(modId);
            ApplyRotation();
            return true;
        }

        public static bool UnlockLook(string modId)
        {
            ModIds.Require(modId, "modId");
            int index = lookLocks.LastIndexOf(modId);
            if (index < 0) return false;
            lookLocks.RemoveAt(index);
            ApplyRotation();
            return true;
        }

        internal static void GameRotation(bool off)
        {
            if (ownRotationCall) return;
            gameRotationOff = off;
            if (!off) ApplyRotation();
        }

        internal static void Init()
        {
            installed = walkingField != null && runSpeedField != null && swimmingSpeedField != null && PlayerMovementPatches.Install();
            if (!installed)
            {
                CorePatcher.DisableOwner(Owner);
                CoreLog.For(Owner).Warn("controller hooks incomplete, speed and jump multipliers are disabled");
            }
            lookPatched = PlayerMovementPatches.InstallLook();
            Events.SceneLoaded += OnSceneLoaded;
            Events.SceneLoadedWithMode += OnSceneLoadedWithMode;
            Events.Loaded += OnLoaded;
            Survival.PlayerDied += OnPlayerDied;
        }

        internal static void ApplyTo(OpenSewerCharacterController controller)
        {
            if (!IsAvailable || controller == null) return;
            try
            {
                if (controller != applied)
                {
                    applied = controller;
                    baseWalkSpeed = walkSpeedField != null ? (float)walkSpeedField.GetValue(controller) : controller.walkSpeed;
                    baseRunSpeed = (float)runSpeedField.GetValue(controller);
                    baseSwimmingSpeed = (float)swimmingSpeedField.GetValue(controller);
                    baseCrouchSpeed = crouchSpeedField != null ? (float)crouchSpeedField.GetValue(controller) : controller.crouchSpeed;
                    baseBackwardSpeed = backwardSpeedField != null ? (float)backwardSpeedField.GetValue(controller) : controller.backwardSpeed;
                    baseStrafeSpeed = strafeSpeedField != null ? (float)strafeSpeedField.GetValue(controller) : controller.strafeSpeed;
                    baseJumpHeight = baseJumpHeightField != null ? (float)baseJumpHeightField.GetValue(controller) : controller.baseJumpHeight;
                }
                float speedMultiplier = Product(speedMultipliers);
                controller.walkSpeed = baseWalkSpeed * Product(walkMultipliers) * speedMultiplier;
                controller.runSpeed = baseRunSpeed * Product(runMultipliers) * speedMultiplier;
                controller.swimmingSpeed = baseSwimmingSpeed * speedMultiplier;
                controller.crouchSpeed = baseCrouchSpeed * speedMultiplier;
                controller.backwardSpeed = baseBackwardSpeed * speedMultiplier;
                controller.strafeSpeed = baseStrafeSpeed * speedMultiplier;
                controller.baseJumpHeight = baseJumpHeight * Product(jumpMultipliers);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.ApplyTo", e);
            }
        }

        private static bool Hold(string modId, Transform lookAt, bool instantly)
        {
            ModIds.Require(modId, "modId");
            GameController game = GameController.instance;
            ThirdPersonCameraController camera = ThirdPersonCameraController.instance;
            if (game == null || SaveGate.Busy || OpenSewerCharacterController.instance == null || camera == null || ThirdPersonCameraCollision.instance == null) return false;
            try
            {
                if (holds.Count == 0)
                {
                    if (holder == null)
                    {
                        holder = new GameObject("ObenseuerCore.PlayerMovement");
                        UnityEngine.Object.DontDestroyOnLoad(holder);
                    }
                    lockedGame = game;
                    game.ControlsDisabled(holder);
                }
                if (lookAt != null)
                {
                    if (!OwnsLook(camera))
                    {
                        softLockBefore = (bool)firstpersonSoftLockField.GetValue(camera);
                        targetBefore = (Transform)lookAtTargetField.GetValue(camera);
                        lookLocked = true;
                    }
                    appliedTarget = lookAt;
                    game.ControlsDisabledMove(holder, true, true, lookAt, true, false, false, instantly);
                }
                holds.Add(new KeyValuePair<string, Transform>(modId, lookAt));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.Lock", e);
                if (holds.Count == 0) ReleaseGame();
                else if (lookAt != null) RefreshLook();
                return false;
            }
        }

        private static bool OwnsLook(ThirdPersonCameraController camera)
        {
            return lookLocked && camera != null && firstpersonSoftLockField != null && lookAtTargetField != null
                && (bool)firstpersonSoftLockField.GetValue(camera) && object.ReferenceEquals(lookAtTargetField.GetValue(camera), appliedTarget);
        }

        private static void RefreshLook()
        {
            try
            {
                ThirdPersonCameraController camera = ThirdPersonCameraController.instance;
                if (OwnsLook(camera))
                {
                    for (int i = holds.Count - 1; i >= 0; i--)
                    {
                        Transform next = holds[i].Value;
                        if (next == null) continue;
                        appliedTarget = next;
                        camera.DisableFirstPersonMovement(true, next, false);
                        return;
                    }
                    firstpersonSoftLockField.SetValue(camera, softLockBefore);
                    lookAtTargetField.SetValue(camera, targetBefore);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.Unlock", e);
            }
            lookLocked = false;
            appliedTarget = null;
            targetBefore = null;
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
                applied.runSpeed = baseRunSpeed;
                applied.swimmingSpeed = baseSwimmingSpeed;
                applied.crouchSpeed = baseCrouchSpeed;
                applied.backwardSpeed = baseBackwardSpeed;
                applied.strafeSpeed = baseStrafeSpeed;
                applied.baseJumpHeight = baseJumpHeight;
            }
            applied = null;
        }

        private static void ReleaseAll()
        {
            if (holds.Count == 0) return;
            holds.Clear();
            ReleaseGame();
        }

        private static void ReleaseGame()
        {
            GameController game = lockedGame;
            lockedGame = null;
            if (game != null && holder != null)
            {
                ThirdPersonCameraController camera = ThirdPersonCameraController.instance;
                bool restoreLooks = camera != null && moveDisabledField != null && firstpersonMoveDisabledField != null;
                bool moveDisabled = restoreLooks && (bool)moveDisabledField.GetValue(camera);
                bool firstpersonMoveDisabled = restoreLooks && (bool)firstpersonMoveDisabledField.GetValue(camera);
                try
                {
                    game.ControlsEnabled(holder, false);
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
            }
            RefreshLook();
        }

        private static void OnSceneLoaded(string ignored)
        {
            if (lockedGame == null || lockedGame != GameController.instance) ReleaseAll();
            Restore();
            OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
            ApplyTo(controller);
            if (controller != null && IsAvailable) pendingOwners.Clear();
        }

        private static void OnLoaded()
        {
            ReleaseAll();
        }

        private static void OnSceneLoadedWithMode(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            ForgetOwners(walkMultipliers);
            ForgetOwners(jumpMultipliers);
            ForgetOwners(speedMultipliers);
            ForgetOwners(runMultipliers);
            lookLocks.Clear();
            gameRotationOff = false;
            ApplyRotation();
        }

        private static void OnPlayerDied(DamageTypeNames reason)
        {
            lookLocks.Clear();
            ApplyRotation();
        }

        private static void ApplyRotation()
        {
            ThirdPersonCameraController camera = ThirdPersonCameraController.instance;
            if (camera == null) return;
            ownRotationCall = true;
            try
            {
                if (gameRotationOff || lookLocks.Count > 0) camera.DisableRotation();
                else camera.EnableRotation();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerMovement.ApplyRotation", e);
            }
            finally
            {
                ownRotationCall = false;
            }
        }
    }
}
