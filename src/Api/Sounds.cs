using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace ObenseuerCore
{
    public static class Sounds
    {
        private const string Owner = "obenseuer.moddingframework.audio";
        private const string HolderName = "ObenseuerCore_Sound";
        private const float ClipPadding = 1.1f;
        private const long MaxWavBytes = 16L * 1024 * 1024;
        private static readonly FieldInfo[] libraryFields = typeof(RandomAudioLibrary).GetFields(BindingFlags.Public | BindingFlags.Instance);
        private static readonly string[] libraryNames = Array.ConvertAll(libraryFields, field => field.Name);
        private static readonly string[] pickupNames = Enum.GetNames(typeof(PickupSoundName));
        private static readonly Dictionary<string, bool> failed = new Dictionary<string, bool>();
        private static readonly string[] mixerGroups = { "Master", "Sound", "Music", "Nasty" };
        private static readonly FieldInfo mixerField = typeof(VolumeSave).GetField("audioMixer", BindingFlags.Instance | BindingFlags.NonPublic);

        public static float MasterVolume
        {
            get
            {
                gameVolume saved = Saved();
                return saved == null ? 0f : saved.master;
            }
        }

        public static float SfxVolume
        {
            get
            {
                gameVolume saved = Saved();
                return saved == null ? 0f : saved.sound;
            }
        }

        public static float MusicVolume
        {
            get
            {
                gameVolume saved = Saved();
                return saved == null ? 0f : saved.music;
            }
        }

        public static string SoundscapeId
        {
            get
            {
                SoundscapeController controller = SoundscapeController.instance;
                SoundscapeArea area = controller == null ? null : controller.currentSoundscape;
                return area == null ? null : area.id;
            }
        }

        public static List<string> PickupNames()
        {
            return new List<string>(pickupNames);
        }

        public static List<string> LibraryNames()
        {
            return new List<string>(libraryNames);
        }

        public static bool PlayPickup(string name)
        {
            RequireText(name, "name");
            if (Array.IndexOf(pickupNames, name) < 0) throw new ArgumentException("unknown pickup sound: " + name, "name");
            if (ItemSounds.instance == null) return false;
            return Guard("PlayPickup", () => { ItemSounds.instance.PlaySound(name); return true; }, false);
        }

        public static bool PlayLibrary(string name)
        {
            RandomAudio sounds = Library(name);
            return sounds != null && MiscAudioPlayer.instance != null && Guard("PlayLibrary", () => sounds.PlayRandomClip() != null, false);
        }

        public static bool PlayLibraryAt(string name, Vector3 position)
        {
            RandomAudio sounds = Library(name);
            return sounds != null && MiscAudioPlayer.instance != null && Guard("PlayLibraryAt", () => PlayMarked(sounds, position), false);
        }

        public static bool PlayClip(AudioClip clip, string group, float volume = 1f)
        {
            return Output(clip, group, Vector3.zero, volume, true);
        }

        public static bool PlayClipAt(AudioClip clip, string group, Vector3 position, float volume = 1f)
        {
            return Output(clip, group, position, volume, false);
        }

        public static AudioMixerGroup Find(string name)
        {
            RequireText(name, "name");
            return Guard<AudioMixerGroup>("Find", () => FirstGroup(name), null);
        }

        public static AudioClip LoadWav(string path)
        {
            RequireText(path, "path");
            return Guard<AudioClip>("LoadWav", () => File.Exists(path) && new FileInfo(path).Length <= MaxWavBytes ? SoundsWav.Parse(File.ReadAllBytes(path), Path.GetFileNameWithoutExtension(path)) : null, null);
        }

        public static bool SetSoundscape(string id)
        {
            RequireText(id, "id");
            SoundscapeController controller = SoundscapeController.instance;
            if (controller == null) return false;
            return Guard("SetSoundscape", () =>
            {
                SoundscapeArea area = controller.FindSoundscapeAreaById(id);
                if (area == null) return false;
                controller.PlaySoundscape(area, playImmediately: true);
                return true;
            }, false);
        }

        public static bool ResetSoundscape()
        {
            SoundscapeController controller = SoundscapeController.instance;
            return controller != null && Guard("ResetSoundscape", () => { controller.PlaySoundscape(null, playImmediately: true); return true; }, false);
        }

        public static bool SetMixerVolume(string modId, string group, float decibels)
        {
            if (!ValidMod(modId) || SaveGate.Busy) return false;
            if (Array.IndexOf(mixerGroups, group) < 0 || float.IsNaN(decibels) || decibels < -80f || decibels > 0f)
            {
                ApiLog.Once(Owner, "SetMixerVolume", "group must be Master, Sound, Music or Nasty, and decibels must be from -80 to 0");
                return false;
            }
            return Guard("SetMixerVolume", () => WriteMixer(group, decibels), false);
        }

        private static bool Output(AudioClip clip, string group, Vector3 position, float volume, bool playEverywhere)
        {
            if (clip == null) throw new ArgumentException("clip must not be null", "clip");
            RequireText(group, "group");
            if (float.IsNaN(volume) || volume < 0f || volume > 1f) throw new ArgumentException("volume must be between 0 and 1", "volume");
            AudioMixerGroup mixer = Find(group);
            return mixer != null && Guard("PlayClip", () => Spawn(clip, mixer, position, volume, playEverywhere), false);
        }

        private static bool Spawn(AudioClip clip, AudioMixerGroup mixer, Vector3 position, float volume, bool playEverywhere)
        {
            GameObject holder = new GameObject(HolderName);
            holder.transform.position = position;
            try
            {
                AudioSourceMaker.FromClip(clip, volume, 1f, 1f, 50f, holder, true, playEverywhere, mixer, false);
            }
            catch (Exception)
            {
                UnityEngine.Object.Destroy(holder);
                throw;
            }
            UnityEngine.Object.Destroy(holder, clip.length * ClipPadding);
            return true;
        }

        private static bool PlayMarked(RandomAudio sounds, Vector3 position)
        {
            GameObject marker = new GameObject(HolderName);
            try
            {
                marker.transform.position = position;
                return sounds.PlayRandomClip(marker) != null;
            }
            finally
            {
                UnityEngine.Object.Destroy(marker);
            }
        }

        private static AudioMixerGroup FirstGroup(string name)
        {
            MiscAudioPlayer player = MiscAudioPlayer.instance;
            if (player == null || player.defaultMixerGroup == null) return null;
            AudioMixerGroup[] groups = player.defaultMixerGroup.audioMixer.FindMatchingGroups(name);
            return groups.Length == 0 ? null : groups[0];
        }

        private static RandomAudio Library(string name)
        {
            RequireText(name, "name");
            int index = Array.IndexOf(libraryNames, name);
            if (index < 0) throw new ArgumentException("unknown library sound: " + name, "name");
            RandomAudioLibrary library = RandomAudioLibrary.instance;
            return library == null ? null : libraryFields[index].GetValue(library) as RandomAudio;
        }

        private static gameVolume Saved()
        {
            return VolumeSave.instance == null ? null : VolumeSave.instance.soundSave;
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }

        private static bool WriteMixer(string group, float decibels)
        {
            AudioMixer mixer = VolumeSave.instance == null || mixerField == null ? null : mixerField.GetValue(VolumeSave.instance) as AudioMixer;
            if (mixer == null) return false;
            bool set = mixer.SetFloat(group, decibels);
            if (group == "Sound") mixer.SetFloat("OtherSounds", decibels);
            return set;
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " must be non-empty", name);
        }

        private static T Guard<T>(string member, Func<T> action, T fallback)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                if (!failed.ContainsKey(member))
                {
                    failed[member] = true;
                    CoreLog.For(Owner).Error(member + " failed: " + e.ToString());
                }
                return fallback;
            }
        }
    }
}
