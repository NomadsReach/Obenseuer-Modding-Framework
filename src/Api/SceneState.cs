using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class BranchInfo
    {
        public string Id { get; private set; }

        public bool Value { get; private set; }

        internal BranchInfo(string id, bool value)
        {
            Id = id;
            Value = value;
        }
    }

    public sealed class CounterInfo
    {
        public string Id { get; private set; }

        public int Value { get; private set; }

        public int Max { get; private set; }

        internal CounterInfo(string id, int value, int max)
        {
            Id = id;
            Value = value;
            Max = max;
        }
    }

    public sealed class StateMachineInfo
    {
        public string Id { get; private set; }

        public string Current { get; private set; }

        public List<string> Names { get; private set; }

        internal StateMachineInfo(string id, string current, List<string> names)
        {
            Id = id;
            Current = current;
            Names = names;
        }
    }

    public sealed class SequenceInfo
    {
        public string Id { get; private set; }

        public int Entries { get; private set; }

        public bool Loop { get; private set; }

        internal SequenceInfo(string id, int entries, bool loop)
        {
            Id = id;
            Entries = entries;
            Loop = loop;
        }
    }

    public sealed class SceneTimerInfo
    {
        public string Id { get; private set; }

        public float Interval { get; private set; }

        public bool Running { get; private set; }

        public bool Disabled { get; private set; }

        internal SceneTimerInfo(string id, float interval, bool running, bool disabled)
        {
            Id = id;
            Interval = interval;
            Running = running;
            Disabled = disabled;
        }
    }

    public static class SceneState
    {
        internal const string Owner = "obenseuer.moddingframework.scenestate";
        private static readonly FieldInfo counterValue = AccessTools.Field(typeof(CounterRelay), "currentValue");
        private static readonly FieldInfo counterMax = AccessTools.Field(typeof(CounterRelay), "maxValue");
        private static readonly FieldInfo stateCurrent = AccessTools.Field(typeof(logic_state), "currentState");
        private static readonly FieldInfo stateList = AccessTools.Field(typeof(logic_state), "states");

        public static List<BranchInfo> Branches()
        {
            List<BranchInfo> list = new List<BranchInfo>();
            try
            {
                foreach (logic_branch branch in SceneIds.Active<logic_branch>())
                {
                    list.Add(new BranchInfo(SceneIds.Of(branch), branch.IsTrue));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneState.Branches", e);
            }
            return list;
        }

        public static bool SetBranch(string modId, string id, bool value)
        {
            try
            {
                logic_branch branch = Target<logic_branch>(modId, id);
                if (branch == null) return false;
                branch.IsTrue = value;
                return branch.IsTrue == value;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.SetBranch", e);
                return false;
            }
        }

        public static List<CounterInfo> Counters()
        {
            List<CounterInfo> list = new List<CounterInfo>();
            try
            {
                foreach (CounterRelay counter in SceneIds.Active<CounterRelay>())
                {
                    if (counterValue == null || counterMax == null) continue;
                    list.Add(new CounterInfo(SceneIds.Of(counter), (int)counterValue.GetValue(counter), (int)counterMax.GetValue(counter)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneState.Counters", e);
            }
            return list;
        }

        public static bool SetCounter(string modId, string id, int value)
        {
            try
            {
                CounterRelay counter = Target<CounterRelay>(modId, id);
                if (counter == null) return false;
                if (value < 0)
                {
                    ApiLog.Once(modId, "SceneState.SetCounter", "value must be 0 or more, the write was refused");
                    return false;
                }
                counter.SetCounterValue(value);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.SetCounter", e);
                return false;
            }
        }

        public static bool AddCounter(string modId, string id, int amount)
        {
            try
            {
                CounterRelay counter = Target<CounterRelay>(modId, id);
                if (counter == null || amount < 1) return false;
                counter.IncreaseCounter(amount);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.AddCounter", e);
                return false;
            }
        }

        public static List<StateMachineInfo> States()
        {
            List<StateMachineInfo> list = new List<StateMachineInfo>();
            try
            {
                foreach (logic_state machine in SceneIds.Active<logic_state>())
                {
                    if (stateCurrent == null || stateList == null) continue;
                    list.Add(new StateMachineInfo(SceneIds.Of(machine), (string)stateCurrent.GetValue(machine), StateNames(machine)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneState.States", e);
            }
            return list;
        }

        public static bool SetState(string modId, string id, string stateName)
        {
            try
            {
                logic_state machine = Target<logic_state>(modId, id);
                if (machine == null || string.IsNullOrEmpty(stateName) || !StateNames(machine).Contains(stateName)) return false;
                machine.ChangeState(stateName);
                return (string)stateCurrent.GetValue(machine) == stateName;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.SetState", e);
                return false;
            }
        }

        public static List<SequenceInfo> Sequences()
        {
            List<SequenceInfo> list = new List<SequenceInfo>();
            try
            {
                foreach (Logic_Sequence sequence in SceneIds.Active<Logic_Sequence>())
                {
                    list.Add(new SequenceInfo(SceneIds.Of(sequence), sequence.sequenceEntries.Count, sequence.LoopSequence));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneState.Sequences", e);
            }
            return list;
        }

        public static bool StartSequence(string modId, string id)
        {
            try
            {
                Logic_Sequence sequence = Target<Logic_Sequence>(modId, id);
                if (sequence == null) return false;
                sequence.TriggerSequence();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.StartSequence", e);
                return false;
            }
        }

        public static bool StopSequence(string modId, string id)
        {
            try
            {
                Logic_Sequence sequence = Target<Logic_Sequence>(modId, id);
                if (sequence == null) return false;
                sequence.StopSequence();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.StopSequence", e);
                return false;
            }
        }

        public static List<SceneTimerInfo> Timers()
        {
            List<SceneTimerInfo> list = new List<SceneTimerInfo>();
            try
            {
                foreach (logic_timer timer in SceneIds.Active<logic_timer>())
                {
                    list.Add(new SceneTimerInfo(SceneIds.Of(timer), timer.interval, timer.timerActive, timer.timerDisabled));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneState.Timers", e);
            }
            return list;
        }

        public static bool StartTimer(string modId, string id)
        {
            try
            {
                logic_timer timer = Target<logic_timer>(modId, id);
                if (timer == null) return false;
                timer.StartTimer();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.StartTimer", e);
                return false;
            }
        }

        public static bool SetInterval(string modId, string id, float seconds)
        {
            try
            {
                logic_timer timer = Target<logic_timer>(modId, id);
                if (timer == null || !(seconds > 0f) || float.IsInfinity(seconds)) return false;
                timer.ChangeInterval(seconds);
                return timer.interval == seconds;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "SceneState.SetInterval", e);
                return false;
            }
        }

        private static T Target<T>(string modId, string id) where T : UnityEngine.Component
        {
            if (string.IsNullOrEmpty(modId) || SaveGate.Busy) return null;
            return SceneIds.Find<T>(id);
        }

        private static List<string> StateNames(logic_state machine)
        {
            List<string> names = new List<string>();
            foreach (logic_state.State state in (List<logic_state.State>)stateList.GetValue(machine))
            {
                names.Add(state.stateName);
            }
            return names;
        }
    }
}
