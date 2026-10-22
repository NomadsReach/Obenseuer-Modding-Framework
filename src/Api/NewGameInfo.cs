using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ObenseuerCore
{
    public sealed class NewGameOptionValue
    {
        public string ModId { get; private set; }
        public string Key { get; private set; }
        public string Title { get; private set; }
        public string Value { get; private set; }

        internal NewGameOptionValue(string modId, string key, string title, string value)
        {
            ModId = modId;
            Key = key;
            Title = title;
            Value = value;
        }
    }

    public sealed class NewGameInfo
    {
        public string CharacterName { get; private set; }
        public string CharacterFirstName { get; private set; }
        public string CharacterLastName { get; private set; }
        public DifficultySettings Difficulty { get; private set; }
        public ReadOnlyCollection<NewGameOptionValue> Options { get; private set; }

        internal NewGameInfo(string firstName, string lastName, DifficultySettings difficulty, List<NewGameOptionValue> options)
        {
            CharacterFirstName = firstName;
            CharacterLastName = lastName;
            CharacterName = Name(firstName, lastName);
            Difficulty = difficulty;
            Options = new ReadOnlyCollection<NewGameOptionValue>(options == null ? new List<NewGameOptionValue>() : options);
        }

        internal static NewGameInfo Capture()
        {
            string first = null;
            string last = null;
            try
            {
                PlayerIdentityMainMenu identity = PlayerIdentityMainMenu.identity;
                if (identity != null)
                {
                    first = identity.firstName;
                    last = identity.lastName;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(NewGameOptions.Owner, "the character's name", e);
            }
            return new NewGameInfo(first, last, DifficultySnapshot(), OptionsSnapshot());
        }

        private static List<NewGameOptionValue> OptionsSnapshot()
        {
            try
            {
                return NewGameOptions.Snapshot();
            }
            catch (Exception e)
            {
                ApiLog.Once(NewGameOptions.Owner, "the new game option values", e);
                return new List<NewGameOptionValue>();
            }
        }

        private static DifficultySettings DifficultySnapshot()
        {
            try
            {
                return GameDifficulty.Current();
            }
            catch (Exception e)
            {
                ApiLog.Once(NewGameOptions.Owner, "the new game difficulty", e);
                return null;
            }
        }

        private static string Name(string firstName, string lastName)
        {
            string name = ((firstName == null ? string.Empty : firstName) + " " + (lastName == null ? string.Empty : lastName)).Trim();
            return name.Length == 0 ? null : name;
        }
    }
}
