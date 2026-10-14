using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class GuitarInfo
    {
        public string Id { get; private set; }

        public int ItemId { get; private set; }

        public bool Equipped { get; private set; }

        public bool HoldPlaying { get; private set; }

        public bool CanPlayNote { get; private set; }

        internal GuitarInfo(string id, int itemId, bool equipped, bool holdPlaying, bool canPlayNote)
        {
            Id = id;
            ItemId = itemId;
            Equipped = equipped;
            HoldPlaying = holdPlaying;
            CanPlayNote = canPlayNote;
        }
    }

    public sealed class GuitarPlayedArgs
    {
        public string GuitarId { get; private set; }

        internal GuitarPlayedArgs(string guitarId)
        {
            GuitarId = guitarId;
        }
    }

    public sealed class GuitarCommentArgs
    {
        public string WitnessId { get; private set; }

        public int NpcId { get; private set; }

        public bool Positive { get; private set; }

        internal GuitarCommentArgs(string witnessId, int npcId, bool positive)
        {
            WitnessId = witnessId;
            NpcId = npcId;
            Positive = positive;
        }
    }

    public static class Instruments
    {
        internal const string Owner = "obenseuer.moddingframework.instruments";
        private static readonly FieldInfo cooldownField = typeof(global::WieldableItem.WieldableGuitar).GetField("cooldownOn", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo noteAudioField = typeof(global::WieldableItem.WieldableGuitar).GetField("primaryUseAudio", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo holdAudioField = AccessTools.Field(typeof(global::WieldableItem.WieldableGuitar), "activeSound");
        private static readonly FieldInfo holdSoundsField = AccessTools.Field(typeof(global::WieldableItem.WieldableGuitar), "primaryHoldAudio");
        private static readonly List<SongEntry> songs = new List<SongEntry>();
        private static readonly List<HoldEntry> holds = new List<HoldEntry>();
        private static bool installed;

        public static event Action<GuitarPlayedArgs> GuitarPlayed;

        public static event Action<GuitarCommentArgs> GuitarCommented;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<GuitarInfo> All()
        {
            List<GuitarInfo> list = new List<GuitarInfo>();
            try
            {
                foreach (global::WieldableItem.WieldableGuitar guitar in SceneIds.Active<global::WieldableItem.WieldableGuitar>())
                {
                    if (guitar != null) list.Add(ToInfo(guitar));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "All", e);
                return new List<GuitarInfo>();
            }
        }

        public static GuitarInfo Get(string id)
        {
            try
            {
                global::WieldableItem.WieldableGuitar guitar = SceneIds.Find<global::WieldableItem.WieldableGuitar>(id);
                return guitar == null ? null : ToInfo(guitar);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Get", e);
                return null;
            }
        }

        public static bool PlayNote(string modId, string guitarId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                return Strum(SceneIds.Find<global::WieldableItem.WieldableGuitar>(guitarId));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Strum", e);
                return false;
            }
        }

        public static bool PlaySong(string modId, string guitarId, IList<float> delays)
        {
            ModIds.Require(modId, "modId");
            if (!GuitarSong.Valid(delays))
            {
                ApiLog.Once(Owner, "PlaySong delays", "delays must be one or more numbers of 0 or more, the seconds before each strum");
                return false;
            }
            if (SaveGate.Busy) return false;
            try
            {
                global::WieldableItem.WieldableGuitar guitar = SceneIds.Find<global::WieldableItem.WieldableGuitar>(guitarId);
                if (guitar == null || !CanPlay(guitar)) return false;
                SetSong(modId, guitarId, guitar, new GuitarSong(delays));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlaySong", e);
                return false;
            }
        }

        public static bool StopSong(string modId, string guitarId)
        {
            ModIds.Require(modId, "modId");
            return songs.RemoveAll(s => s.ModId == modId && s.GuitarId == guitarId) > 0;
        }

        public static bool HoldNote(string modId, string guitarId, int index)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy || PlayerInputBlocked()) return false;
            try
            {
                global::WieldableItem.WieldableGuitar guitar = SceneIds.Find<global::WieldableItem.WieldableGuitar>(guitarId);
                if (guitar == null || !CanPlay(guitar)) return false;
                AudioSource[] sounds = HoldSounds(guitar);
                if (sounds == null || index < 0 || index >= sounds.Length || sounds[index] == null || sounds[index].isPlaying) return false;
                SetHold(modId, guitarId, guitar, sounds[index]);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "HoldNote", e);
                return false;
            }
        }

        public static bool StopHold(string modId, string guitarId)
        {
            ModIds.Require(modId, "modId");
            return RemoveHolds(h => h.ModId == modId && h.GuitarId == guitarId);
        }

        public static int HoldCount(string guitarId)
        {
            try
            {
                global::WieldableItem.WieldableGuitar guitar = SceneIds.Find<global::WieldableItem.WieldableGuitar>(guitarId);
                if (guitar == null) return 0;
                AudioSource[] sounds = HoldSounds(guitar);
                return sounds == null ? 0 : sounds.Length;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "HoldCount", e);
                return 0;
            }
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            bool song = songs.RemoveAll(s => s.ModId == modId) > 0;
            bool hold = RemoveHolds(h => h.ModId == modId);
            return song || hold;
        }

        internal static void Init()
        {
            installed = InstrumentsPatches.Install();
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (installed) return;
            CoreLog.For(Owner).Warn("instrument hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled");
        }

        internal static void Tick()
        {
            if (holds.Count > 0) CheckHolds();
            if (songs.Count == 0) return;
            foreach (SongEntry entry in songs.ToArray())
            {
                if (!songs.Contains(entry)) continue;
                int due = entry.Notes.Advance(Time.deltaTime);
                for (int i = 0; i < due && songs.Contains(entry); i++) Strum(entry.Guitar);
                if (entry.Notes.Done) songs.Remove(entry);
            }
        }

        internal static bool HasPlayedHandlers
        {
            get { return GuitarPlayed != null; }
        }

        internal static bool HasCommentedHandlers
        {
            get { return GuitarCommented != null; }
        }

        internal static void RaisePlayed(GuitarPlayedArgs args)
        {
            Events.Raise(GuitarPlayed, "Instruments.GuitarPlayed", h => h(args));
        }

        internal static void RaiseCommented(GuitarCommentArgs args)
        {
            Events.Raise(GuitarCommented, "Instruments.GuitarCommented", h => h(args));
        }

        internal static bool CanPlay(global::WieldableItem.WieldableGuitar guitar)
        {
            if (cooldownField == null || noteAudioField == null)
            {
                ApiLog.Once(Owner, "CanPlay", "guitar field cooldownOn or primaryUseAudio is missing, no guitar can play");
                return false;
            }
            return guitar.equipped && !(bool)cooldownField.GetValue(guitar) && noteAudioField.GetValue(guitar) != null;
        }

        private static bool PlayerInputBlocked()
        {
            GameController game = GameController.instance;
            GameUIController ui = GameUIController.instance;
            DialogueController dialogue = DialogueController.instance;
            PlayerHandItems hands = PlayerHandItems.instance;
            return (game != null && game.GameIsPaused) || (ui != null && ui.IsMenuVisible) || (dialogue != null && dialogue.DialogueActive) || (hands != null && hands.itemUseDisabled);
        }

        private static bool Strum(global::WieldableItem.WieldableGuitar guitar)
        {
            if (SaveGate.Busy || PlayerInputBlocked()) return false;
            try
            {
                if (guitar == null || !guitar.gameObject.activeInHierarchy || !CanPlay(guitar)) return false;
                guitar.onPrimaryUse();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Strum", e);
                return false;
            }
        }

        private static void SetSong(string modId, string guitarId, global::WieldableItem.WieldableGuitar guitar, GuitarSong notes)
        {
            foreach (SongEntry entry in songs)
            {
                if (entry.GuitarId == guitarId && entry.ModId != modId)
                {
                    ApiLog.Once(Owner, "song " + guitarId + " " + entry.ModId + " " + modId, "song on guitar " + guitarId + " is set by " + entry.ModId + " and " + modId + ", the newest call (" + modId + ") plays");
                }
            }
            songs.RemoveAll(s => s.GuitarId == guitarId);
            songs.Add(new SongEntry { ModId = modId, GuitarId = guitarId, Guitar = guitar, Notes = notes });
        }

        private static void SetHold(string modId, string guitarId, global::WieldableItem.WieldableGuitar guitar, AudioSource sound)
        {
            foreach (HoldEntry entry in holds)
            {
                if (entry.GuitarId == guitarId && entry.ModId != modId)
                {
                    ApiLog.Once(Owner, "hold " + guitarId + " " + entry.ModId + " " + modId, "hold on guitar " + guitarId + " is set by " + entry.ModId + " and " + modId + ", the newest call (" + modId + ") plays");
                }
            }
            RemoveHolds(h => h.GuitarId == guitarId);
            holds.Add(new HoldEntry { ModId = modId, GuitarId = guitarId, Guitar = guitar, Sound = sound });
            sound.Play();
        }

        private static AudioSource[] HoldSounds(global::WieldableItem.WieldableGuitar guitar)
        {
            if (holdSoundsField == null)
            {
                ApiLog.Once(Owner, "HoldSounds", "guitar field primaryHoldAudio is missing, no held note can play");
                return null;
            }
            return holdSoundsField.GetValue(guitar) as AudioSource[];
        }

        private static void CheckHolds()
        {
            bool blocked = PlayerInputBlocked();
            RemoveHolds(h => blocked || h.Guitar == null || !h.Guitar.equipped || h.Sound == null || !h.Sound.isPlaying || GameOwnsSound(h));
        }

        private static bool RemoveHolds(Func<HoldEntry, bool> match)
        {
            bool removed = false;
            for (int i = holds.Count - 1; i >= 0; i--)
            {
                if (!match(holds[i])) continue;
                if (holds[i].Sound != null && !GameOwnsSound(holds[i])) holds[i].Sound.Stop();
                holds.RemoveAt(i);
                removed = true;
            }
            return removed;
        }

        private static bool GameOwnsSound(HoldEntry entry)
        {
            if (entry.Guitar == null || entry.Sound == null) return false;
            if (holdAudioField == null)
            {
                ApiLog.Once(Owner, "GameOwnsSound", "guitar field activeSound is missing, a held note does not yield to the player's hold");
                return false;
            }
            AudioSource active = holdAudioField.GetValue(entry.Guitar) as AudioSource;
            return active != null && active == entry.Sound;
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            songs.Clear();
            RemoveHolds(h => true);
        }

        private static GuitarInfo ToInfo(global::WieldableItem.WieldableGuitar guitar)
        {
            AudioSource hold = holdAudioField == null ? null : holdAudioField.GetValue(guitar) as AudioSource;
            return new GuitarInfo(SceneIds.Of(guitar), guitar.item == null ? 0 : guitar.item.ID, guitar.equipped, hold != null && hold.isPlaying, CanPlay(guitar));
        }

        private sealed class SongEntry
        {
            internal string ModId;
            internal string GuitarId;
            internal global::WieldableItem.WieldableGuitar Guitar;
            internal GuitarSong Notes;
        }

        private sealed class HoldEntry
        {
            internal string ModId;
            internal string GuitarId;
            internal global::WieldableItem.WieldableGuitar Guitar;
            internal AudioSource Sound;
        }
    }
}
