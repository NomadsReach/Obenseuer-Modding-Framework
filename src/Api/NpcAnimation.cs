using System;
using System.Collections.Generic;
using NPC;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class NpcAnimation
    {
        internal const string Owner = "obenseuer.moddingframework.npcanimation";
        private static readonly List<Playing> playing = new List<Playing>();

        public static bool Play(string modId, int npcId, AnimationClip clip, bool loop, float fadeSeconds = 0.2f)
        {
            ModIds.Require(modId, "modId");
            if (clip == null) throw new ArgumentNullException("clip");
            if (clip.legacy) throw new ArgumentException("clip must not be a legacy clip", "clip");
            RequireFade(fadeSeconds, "fadeSeconds");
            if (SaveGate.Busy) return false;
            try
            {
                NPCInfo npc = Live(npcId);
                Animator animator = npc == null ? null : npc.GetComponentInChildren<Animator>();
                if (animator == null) return false;
                return Begin(modId, Find(npcId), npcId, npc, animator, clip, loop, fadeSeconds);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAnimation.Play", e);
                return false;
            }
        }

        public static bool PlayOn(string modId, Animator animator, AnimationClip clip, bool loop, float fadeSeconds = 0.2f)
        {
            ModIds.Require(modId, "modId");
            if ((object)animator == null) throw new ArgumentNullException("animator");
            if (clip == null) throw new ArgumentNullException("clip");
            if (clip.legacy) throw new ArgumentException("clip must not be a legacy clip", "clip");
            RequireFade(fadeSeconds, "fadeSeconds");
            if (animator == null) return false;
            if (SaveGate.Busy) return false;
            try
            {
                if (!animator.gameObject.activeInHierarchy || animator.GetComponentInParent<NPCInfo>() != null) return false;
                return Begin(modId, FindOn(animator), 0, null, animator, clip, loop, fadeSeconds);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAnimation.PlayOn", e);
                return false;
            }
        }

        public static bool Stop(string modId, int npcId, float fadeSeconds = 0.2f)
        {
            ModIds.Require(modId, "modId");
            RequireFade(fadeSeconds, "fadeSeconds");
            if (SaveGate.Busy) return false;
            try
            {
                return End(modId, Find(npcId), fadeSeconds);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAnimation.Stop", e);
                return false;
            }
        }

        public static bool StopOn(string modId, Animator animator, float fadeSeconds = 0.2f)
        {
            ModIds.Require(modId, "modId");
            if ((object)animator == null) throw new ArgumentNullException("animator");
            RequireFade(fadeSeconds, "fadeSeconds");
            if (animator == null) return false;
            if (SaveGate.Busy) return false;
            try
            {
                return End(modId, FindOn(animator), fadeSeconds);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAnimation.StopOn", e);
                return false;
            }
        }

        public static bool IsPlaying(int npcId)
        {
            try
            {
                Playing current = Find(npcId);
                return current != null && !current.Ending;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAnimation.IsPlaying", e);
                return false;
            }
        }

        public static int StopAll(string modId)
        {
            ModIds.Require(modId, "modId");
            int count = 0;
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                if (playing[i].ModId != modId) continue;
                Remove(playing[i]);
                count++;
            }
            return count;
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        internal static void Tick()
        {
            float deltaTime = Time.deltaTime;
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                Playing current = playing[i];
                bool keep;
                try
                {
                    keep = current.Advance(deltaTime);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "NpcAnimation.Tick", e);
                    keep = false;
                }
                if (!keep) Remove(current);
            }
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Playing[] all = playing.ToArray();
            playing.Clear();
            foreach (Playing current in all) current.DestroyGraph();
        }

        private static NPCInfo Live(int npcId)
        {
            if (NPCManager.instance == null) return null;
            NPCInfo npc = NPCManager.instance.FindNpcById(npcId);
            if (npc == null || !npc.gameObject.activeInHierarchy || npc.IsDead()) return null;
            return npc;
        }

        internal static void ReleaseOn(Animator animator)
        {
            if (animator == null) return;
            Playing current = FindOn(animator);
            if (current != null) Remove(current);
        }

        private static bool Begin(string modId, Playing current, int npcId, NPCInfo npc, Animator animator, AnimationClip clip, bool loop, float fadeSeconds)
        {
            if (current != null && current.ModId != modId)
            {
                if (!current.Ending) return false;
                Remove(current);
                current = null;
            }
            if (current != null && current.Rig == animator)
            {
                try
                {
                    current.Replace(clip, loop, fadeSeconds);
                }
                catch
                {
                    Remove(current);
                    throw;
                }
                return true;
            }
            if (current != null) Remove(current);
            playing.Add(Playing.Start(modId, npcId, npc, animator, npc == null, clip, loop, fadeSeconds));
            return true;
        }

        private static bool End(string modId, Playing current, float fadeSeconds)
        {
            if (current == null || current.ModId != modId) return false;
            if (current.Ending)
            {
                if (fadeSeconds > 0f) return false;
                Remove(current);
                return true;
            }
            if (fadeSeconds <= 0f) Remove(current);
            else current.BeginEnd(fadeSeconds);
            return true;
        }

        private static Playing Find(int npcId)
        {
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                Playing current = playing[i];
                if (current.Extra || current.NpcId != npcId) continue;
                if (current.IsAlive()) return current;
                Remove(current);
            }
            return null;
        }

        private static Playing FindOn(Animator animator)
        {
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                Playing current = playing[i];
                if (!current.Extra || current.Rig != animator) continue;
                if (current.IsAlive()) return current;
                Remove(current);
            }
            return null;
        }

        private static void Remove(Playing current)
        {
            playing.Remove(current);
            current.DestroyGraph();
        }

        private static void RequireFade(float fadeSeconds, string param)
        {
            if (float.IsNaN(fadeSeconds) || float.IsInfinity(fadeSeconds) || fadeSeconds < 0f) throw new ArgumentException(param + " must be a finite number of 0 or more", param);
        }

        private sealed class Playing
        {
            public string ModId;
            public int NpcId;
            public NPCInfo Npc;
            public bool Extra;
            public GameObject Root;
            public Animator Rig;
            public PlayableGraph Graph;
            public AnimationPlayableOutput Output;
            public AnimationClipPlayable Clip;
            public AnimationClip ClipAsset;
            public AnimationClipPlayable OldClip;
            public AnimationMixerPlayable Mixer;
            public bool Crossfading;
            public float Mix;
            public float CrossSeconds;
            public bool Loop;
            public float FadeSeconds;
            public float Blend;
            public bool Ending;

            public static Playing Start(string modId, int npcId, NPCInfo npc, Animator animator, bool extra, AnimationClip clip, bool loop, float fadeSeconds)
            {
                Playing created = new Playing();
                created.ModId = modId;
                created.NpcId = npcId;
                created.Npc = npc;
                created.Extra = extra;
                created.Root = extra ? animator.gameObject : npc.gameObject;
                created.Rig = animator;
                try
                {
                    created.Graph = PlayableGraph.Create("ObenseuerCore.NpcAnimation." + npcId);
                    created.Clip = AnimationClipPlayable.Create(created.Graph, clip);
                    created.ClipAsset = clip;
                    if (!loop) created.Clip.SetDuration(clip.length);
                    created.Output = AnimationPlayableOutput.Create(created.Graph, "ObenseuerCore.NpcAnimation", animator);
                    created.Output.SetSourcePlayable(created.Clip);
                    created.Loop = loop;
                    created.FadeSeconds = loop ? fadeSeconds : Mathf.Min(fadeSeconds, clip.length * 0.5f);
                    created.Blend = created.FadeSeconds <= 0f ? 1f : 0f;
                    created.Output.SetWeight(created.Blend);
                    created.Graph.Play();
                }
                catch
                {
                    created.DestroyGraph();
                    throw;
                }
                return created;
            }

            public void Replace(AnimationClip clip, bool loop, float fadeSeconds)
            {
                if (Crossfading) FinishCrossfade();
                AnimationClipPlayable next = AnimationClipPlayable.Create(Graph, clip);
                if (!loop) next.SetDuration(clip.length);
                Loop = loop;
                FadeSeconds = loop ? fadeSeconds : Mathf.Min(fadeSeconds, clip.length * 0.5f);
                Ending = false;
                if (FadeSeconds > 0f && TryCrossfade(next, clip)) return;
                SwitchAt(next, clip);
            }

            private bool TryCrossfade(AnimationClipPlayable next, AnimationClip clip)
            {
                OldClip = Clip;
                Mixer = AnimationMixerPlayable.Create(Graph, 2);
                Output.SetSourcePlayable(Mixer);
                if (!Graph.Connect(OldClip, 0, Mixer, 0) || !Graph.Connect(next, 0, Mixer, 1))
                {
                    Mixer.Destroy();
                    Output.SetSourcePlayable(OldClip);
                    return false;
                }
                Mixer.SetInputWeight(0, 1f);
                Mixer.SetInputWeight(1, 0f);
                Clip = next;
                ClipAsset = clip;
                Crossfading = true;
                Mix = 0f;
                CrossSeconds = FadeSeconds;
                return true;
            }

            private void SwitchAt(AnimationClipPlayable next, AnimationClip clip)
            {
                Output.SetSourcePlayable(next);
                Clip.Destroy();
                Clip = next;
                ClipAsset = clip;
                Blend = 1f;
                Output.SetWeight(Blend);
            }

            public void BeginEnd(float fadeSeconds)
            {
                FadeSeconds = fadeSeconds;
                Ending = true;
            }

            public bool IsAlive()
            {
                if (Extra) return Rig != null && Root != null && Root.activeInHierarchy && Graph.IsValid();
                return Npc != null && !Npc.IsDead() && Rig != null && Root != null && Root.activeInHierarchy && Graph.IsValid();
            }

            public bool Advance(float deltaTime)
            {
                if (!IsAlive()) return false;
                if (!Loop && Clip.GetTime() + deltaTime >= ClipAsset.length)
                {
                    Clip.SetTime(ClipAsset.length);
                    Clip.Pause();
                }
                if (!Ending) CheckClip();
                if (Crossfading)
                {
                    Mix = Approach(Mix, 1f, CrossSeconds, deltaTime);
                    if (Mix >= 1f) FinishCrossfade();
                    else
                    {
                        Mixer.SetInputWeight(0, 1f - Mix);
                        Mixer.SetInputWeight(1, Mix);
                    }
                }
                Blend = Approach(Blend, Ending ? 0f : 1f, FadeSeconds, deltaTime);
                Output.SetWeight(Blend);
                return !(Ending && Blend <= 0f);
            }

            public void DestroyGraph()
            {
                try
                {
                    if (Graph.IsValid()) Graph.Destroy();
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "NpcAnimation.DestroyGraph", e);
                }
            }

            private void CheckClip()
            {
                if (Loop)
                {
                    if (ClipAsset.isLooping || ClipAsset.length <= 0f) return;
                    double time = Clip.GetTime();
                    if (time >= ClipAsset.length) Clip.SetTime(time % ClipAsset.length);
                    return;
                }
                double end = ClipAsset.length - FadeSeconds;
                if (end < 0d) end = 0d;
                if (Clip.GetTime() >= end) Ending = true;
            }

            private void FinishCrossfade()
            {
                Graph.Disconnect(Mixer, 0);
                Graph.Disconnect(Mixer, 1);
                Output.SetSourcePlayable(Clip);
                OldClip.Destroy();
                Mixer.Destroy();
                Crossfading = false;
                Mix = 1f;
            }

            private static float Approach(float value, float target, float seconds, float deltaTime)
            {
                if (seconds <= 0f) return target;
                return Mathf.MoveTowards(value, target, deltaTime / seconds);
            }
        }
    }
}
