using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class RadioInfo
    {
        public string Id { get; private set; }

        public string Title { get; private set; }

        public Vector3 Position { get; private set; }

        public bool Enabled { get; private set; }

        public int State { get; private set; }

        internal RadioInfo(string id, string title, Vector3 position, bool enabled, int state)
        {
            Id = id;
            Title = title;
            Position = position;
            Enabled = enabled;
            State = state;
        }
    }

    public static class Radios
    {
        public static List<RadioInfo> All()
        {
            List<RadioInfo> list = new List<RadioInfo>();
            try
            {
                foreach (Radio radio in SceneIds.Active<Radio>())
                {
                    try
                    {
                        if (radio != null) list.Add(ToInfo(radio));
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once("ObenseuerCore", "Radios.Entry", e);
                    }
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "Radios.All", e);
                return new List<RadioInfo>();
            }
        }

        public static RadioInfo Get(string id)
        {
            try
            {
                Radio radio = SceneIds.Find<Radio>(id);
                return radio == null ? null : ToInfo(radio);
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "Radios.Get", e);
                return null;
            }
        }

        public static bool Enabled(string id)
        {
            try
            {
                Radio radio = SceneIds.Find<Radio>(id);
                return radio != null && radio.RadioEnabled;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "Radios.Enabled", e);
                return false;
            }
        }

        public static int State(string id)
        {
            try
            {
                Radio radio = SceneIds.Find<Radio>(id);
                return radio == null ? 0 : radio.CurrentState;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "Radios.State", e);
                return 0;
            }
        }

        public static bool SetEnabled(string id, bool enabled)
        {
            if (string.IsNullOrEmpty(id))
            {
                ApiLog.Once("ObenseuerCore", "Radios.SetEnabled.id", "id must be a non-empty radio id");
                return false;
            }
            try
            {
                if (SaveGate.Busy) return false;
                Radio radio = SceneIds.Find<Radio>(id);
                if (radio == null) return false;
                radio.EnableRadio(enabled);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "Radios.SetEnabled", e);
                return false;
            }
        }

        private static RadioInfo ToInfo(Radio radio)
        {
            return new RadioInfo(SceneIds.Of(radio), radio.title, radio.transform.position, radio.RadioEnabled, radio.CurrentState);
        }
    }
}
