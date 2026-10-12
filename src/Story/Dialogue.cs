using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    public delegate void ConversationHandler(ConversationArgs args);

    public sealed class ConversationArgs
    {
        public string Title { get; set; }
        public Transform Actor { get; private set; }
        public Transform Conversant { get; private set; }
        public bool Cancel { get; set; }

        internal ConversationArgs(string title, Transform actor, Transform conversant)
        {
            Title = title;
            Actor = actor;
            Conversant = conversant;
        }
    }

    public sealed class DialogueArgs
    {
        public string Title { get; private set; }

        internal DialogueArgs(string title)
        {
            Title = title;
        }
    }

    public static class Dialogue
    {
        internal const string Owner = "obenseuer.moddingframework.dialogue";
        private static readonly string[] gameVariables = { "Actor", "ActorIndex", "Alert", "Conversant", "ConversantIndex", "Conversation", "Converrsant", "Currency", "Day", "PlayerAge", "PlayerFirstName", "PlayerGender", "PlayerLastName", "PoliceIdle", "Price", "SavedLevelName", "Speaker", "WakeUpReason" };
        private static readonly string[] luaKeywords = { "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while" };
        private static readonly List<Interceptor> interceptors = new List<Interceptor>();
        private static bool installed;
        private static bool running;
        private static string currentTitle = "";

        public static event Action<DialogueArgs> Started;
        public static event Action<DialogueArgs> Ended;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool IsActive
        {
            get
            {
                if (!IsAvailable) return false;
                try
                {
                    return PCDS.DialogueManager.isConversationActive;
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "IsActive", e);
                    return false;
                }
            }
        }

        public static string CurrentTitle
        {
            get
            {
                return IsActive ? currentTitle : "";
            }
        }

        public static bool Exists(string title)
        {
            if (!IsAvailable || string.IsNullOrEmpty(title)) return false;
            try
            {
                PCDS.DialogueDatabase database = PCDS.DialogueManager.masterDatabase;
                return database != null && database.GetConversation(title) != null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Exists", e);
                return false;
            }
        }

        public static bool HasVariable(string name)
        {
            if (!IsAvailable || string.IsNullOrEmpty(name)) return false;
            try
            {
                return PCDS.DialogueLua.DoesVariableExist(name);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "HasVariable", e);
                return false;
            }
        }

        public static string GetVariableString(string name, string fallback)
        {
            if (!IsAvailable || string.IsNullOrEmpty(name)) return fallback;
            try
            {
                return PCDS.DialogueLua.GetVariable(name, fallback);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GetVariableString", e);
                return fallback;
            }
        }

        public static float GetVariableNumber(string name, float fallback)
        {
            if (!IsAvailable || string.IsNullOrEmpty(name)) return fallback;
            try
            {
                return PCDS.DialogueLua.GetVariable(name, fallback);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GetVariableNumber", e);
                return fallback;
            }
        }

        public static bool GetVariableBool(string name, bool fallback)
        {
            if (!IsAvailable || string.IsNullOrEmpty(name)) return fallback;
            try
            {
                return PCDS.DialogueLua.GetVariable(name, fallback);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GetVariableBool", e);
                return fallback;
            }
        }

        public static bool SetVariable(string name, object value)
        {
            if (!IsAvailable) return false;
            string refusal = NameRefusal(name) ?? ValueRefusal(value);
            if (refusal != null)
            {
                StoryLog.Refuse(Owner, "SetVariable", refusal);
                return false;
            }
            if (SaveGate.Busy) return false;
            try
            {
                PCDS.DialogueLua.SetVariable(name, value);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetVariable", e);
                return false;
            }
        }

        public static string GetActorField(string actor, string field, string fallback)
        {
            if (!IsAvailable || string.IsNullOrEmpty(actor) || string.IsNullOrEmpty(field)) return fallback;
            try
            {
                PCDS.Lua.Result result = PCDS.DialogueLua.GetActorField(actor, field);
                return result.isString ? result.asString : fallback;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GetActorField", e);
                return fallback;
            }
        }

        public static string RegisterFunction(string modId, string name, object target, MethodInfo method)
        {
            ModIds.Require(modId, "modId");
            string luaName = DialogueLuaBridge.LuaName(modId, name);
            DialogueLuaBridge.Check(modId, luaName, target, method);
            if (!IsAvailable) return null;
            try
            {
                DialogueLuaBridge.Bind(modId, luaName, target, method);
                return luaName;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "RegisterFunction", e);
                return null;
            }
        }

        public static bool Start(string title, Transform actor, Transform conversant)
        {
            if (!IsAvailable || string.IsNullOrEmpty(title) || IsActive || SaveGate.Busy) return false;
            try
            {
                InteractableTalk talk = conversant == null ? null : conversant.GetComponent<InteractableTalk>();
                if (talk != null) talk.StartDialogue(title);
                else PCDS.DialogueManager.StartConversation(title, actor, conversant);
                return PCDS.DialogueManager.isConversationActive;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Start", e);
                return false;
            }
        }

        public static void Stop()
        {
            if (!IsAvailable) return;
            try
            {
                PCDS.DialogueManager.StopAllConversations();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Stop", e);
            }
        }

        public static void Intercept(string modId, ConversationHandler handler)
        {
            ModIds.Require(modId, "modId");
            if (handler == null) throw new ArgumentNullException("handler");
            if (interceptors.Exists(i => i.ModId == modId && i.Handler == handler)) return;
            interceptors.Add(new Interceptor { ModId = modId, Handler = handler });
        }

        public static void RemoveAll(string modId)
        {
            ModIds.Require(modId, "modId");
            interceptors.RemoveAll(i => i.ModId == modId);
            DialogueLuaBridge.RemoveAll(modId);
        }

        internal static void Init()
        {
            StoryPump.Ensure();
            installed = DialoguePatches.Install();
            if (IsAvailable)
            {
                DialogueLuaBridge.RegisterCore();
                Events.SceneLoaded += OnSceneLoaded;
            }
            CoreLog.For(Owner).Info(installed ? "dialogue installed" : "dialogue not installed");
        }

        internal static bool RunInterceptors(ref string title, Transform actor, Transform conversant)
        {
            if (running || interceptors.Count == 0) return true;
            running = true;
            try
            {
                ConversationArgs args = new ConversationArgs(title, actor, conversant);
                foreach (Interceptor interceptor in new List<Interceptor>(interceptors))
                {
                    try
                    {
                        interceptor.Handler(args);
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(Owner, "Intercept " + interceptor.ModId, e);
                    }
                }
                if (args.Title != title && !string.IsNullOrEmpty(args.Title))
                {
                    if (Exists(args.Title)) title = args.Title;
                    else StoryLog.Refuse(Owner, "Intercept", "redirect target is not in the conversation database, the original title is kept");
                }
                return !args.Cancel;
            }
            finally
            {
                running = false;
            }
        }

        internal static void OnStarted()
        {
            currentTitle = PCDS.DialogueManager.lastConversationStarted ?? "";
            StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.DialogueStarted, Text = currentTitle });
        }

        internal static void OnEnded()
        {
            string title = PCDS.DialogueManager.lastConversationEnded ?? "";
            currentTitle = "";
            StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.DialogueEnded, Text = title });
        }

        internal static void RaiseStarted(string title)
        {
            DialogueArgs args = new DialogueArgs(title);
            Events.Raise(Started, "Dialogue.Started", h => h(args));
        }

        internal static void RaiseEnded(string title)
        {
            DialogueArgs args = new DialogueArgs(title);
            Events.Raise(Ended, "Dialogue.Ended", h => h(args));
        }

        internal static string NameRefusal(string name)
        {
            if (!DialogueLuaBridge.IsIdentifier(name)) return "name must match ^[A-Za-z_][A-Za-z0-9_]*$";
            if (Array.IndexOf(luaKeywords, name) >= 0) return "name is a Lua keyword";
            if (Array.IndexOf(gameVariables, name) >= 0 || name.StartsWith("DREAM_", StringComparison.Ordinal)) return "name is written by the game";
            return null;
        }

        internal static string ValueRefusal(object value)
        {
            if (value is bool || value is int) return null;
            if (value is string) return IsPlainText((string)value) ? null : "string value contains a backslash or a control character";
            if (value is float || value is double)
            {
                float number = value is float ? (float)value : (float)(double)value;
                return float.IsNaN(number) || float.IsInfinity(number) ? "number is NaN or infinite, or too large for a float" : null;
            }
            return "value must be a string, bool, int, float or double";
        }

        private static bool IsPlainText(string text)
        {
            foreach (char c in text)
            {
                if (c == '\\' || char.IsControl(c)) return false;
            }
            return true;
        }

        private static void OnSceneLoaded(string scene)
        {
            if (IsAvailable) DialogueLuaBridge.RegisterCore();
        }

        private sealed class Interceptor
        {
            internal string ModId;
            internal ConversationHandler Handler;
        }
    }
}
