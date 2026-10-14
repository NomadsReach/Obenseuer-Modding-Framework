using System;
using System.Text;

namespace ObenseuerCore
{
    internal static class CoreModSelfTest
    {
        private const string Id = "obenseuer.coremodcheck";

        internal static string Run()
        {
            try
            {
                CoreMod first = CoreMod.Of(Id);
                if (!object.ReferenceEquals(first, CoreMod.Of(Id))) return "the same guid gave two handles";
                if (object.ReferenceEquals(first, CoreMod.Of(Id + ".other"))) return "two guids shared a handle";
                if (first.Id != Id) return "Id does not match the guid";
                SaveSlot<Probe> slot = new SaveSlot<Probe>();
                if (slot.Value == null) return "a new slot has a null Value";
                slot.Load(null);
                if (slot.Value == null) return "a load of nothing left a null Value";
                Probe loaded = new Probe();
                slot.Load(loaded);
                if (!object.ReferenceEquals(slot.Value, loaded)) return "a load did not set the loaded value";
                if (CorePatcher.UniqueNamed(typeof(StringBuilder), "Clear") == null) return "a unique name was not found";
                if (CorePatcher.UniqueNamed(typeof(StringBuilder), "Append") != null) return "an overloaded name was found without its parameter types";
                if (CorePatcher.UniqueNamed(typeof(StringBuilder), "NoSuchMember") != null) return "a missing name was found";
                try
                {
                    slot.Value = null;
                    return "a null Value was accepted";
                }
                catch (ArgumentNullException)
                {
                }
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "coremod self-check ok";
        }

        private sealed class Probe
        {
        }
    }
}
