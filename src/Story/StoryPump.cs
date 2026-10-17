using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ObenseuerCore
{
    internal enum StoryEventKind
    {
        TaskChanged,
        ObjectiveChanged,
        DialogueStarted,
        DialogueEnded,
        Committed,
        Spotted,
        Fined,
        Arrested,
        Released
    }

    internal struct StoryEvent
    {
        internal StoryEventKind Kind;
        internal string Text;
        internal string Detail;
        internal int Number;
        internal int Current;
        internal int Target;
        internal CommittedArgs Crime;
    }

    internal static class StoryPump
    {
        private static readonly Queue<StoryEvent> queue = new Queue<StoryEvent>(32);
        private static GameObject host;

        internal static void Ensure()
        {
            if (host != null) return;
            host = new GameObject("ObenseuerCore.Story");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<StoryDriver>();
            SaveController.PlayerWillLoadGame += OnWillLoadGame;
        }

        internal static void Clear()
        {
            queue.Clear();
        }

        private static void OnWillLoadGame()
        {
            if (!SameCharacter(SaveController.LastSaveFolderPath, SaveData.CharacterName)) queue.Clear();
        }

        private static bool SameCharacter(string folder, string character)
        {
            string trimmed = (folder ?? "").TrimEnd('/', '\\');
            if (trimmed.Length == 0 || character == null) return false;
            return string.Equals(Path.GetFileName(Path.GetDirectoryName(trimmed)), character, System.StringComparison.OrdinalIgnoreCase);
        }

        internal static void Enqueue(StoryEvent e)
        {
            queue.Enqueue(e);
        }

        internal static void Flush()
        {
            if (SaveGate.Busy) return;
            int count = queue.Count;
            for (int i = 0; i < count; i++) Raise(queue.Dequeue());
        }

        private static void Raise(StoryEvent e)
        {
            switch (e.Kind)
            {
                case StoryEventKind.TaskChanged:
                    Quests.RaiseTaskChanged(e.Text, (TaskState)e.Number);
                    break;
                case StoryEventKind.ObjectiveChanged:
                    Quests.RaiseObjectiveChanged(e.Text, e.Detail, (ObjectiveState)e.Number, e.Current, e.Target);
                    break;
                case StoryEventKind.DialogueStarted:
                    Dialogue.RaiseStarted(e.Text);
                    break;
                case StoryEventKind.DialogueEnded:
                    Dialogue.RaiseEnded(e.Text);
                    break;
                case StoryEventKind.Committed:
                    Law.RaiseCommitted(e.Crime);
                    break;
                case StoryEventKind.Spotted:
                    Law.RaiseSpotted();
                    break;
                case StoryEventKind.Fined:
                    Law.RaiseFined(e.Number);
                    break;
                case StoryEventKind.Arrested:
                    Law.RaiseArrested();
                    break;
                case StoryEventKind.Released:
                    Law.RaiseReleased();
                    break;
            }
        }
    }

    internal sealed class StoryDriver : MonoBehaviour
    {
        private void LateUpdate()
        {
            StoryPump.Flush();
        }
    }
}
