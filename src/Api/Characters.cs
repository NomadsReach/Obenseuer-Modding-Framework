using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class SavedCharacterInfo
    {
        public string Name { get; private set; }

        public string Folder { get; private set; }

        public DateTime LastPlayed { get; private set; }

        internal SavedCharacterInfo(string name, string folder, DateTime lastPlayed)
        {
            Name = name;
            Folder = folder;
            LastPlayed = lastPlayed;
        }
    }

    public sealed class CharacterInfo
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        public string LastName { get; private set; }

        public string FullName { get; private set; }

        public string GenderString { get; private set; }

        public int Age { get; private set; }

        public string Relationship { get; private set; }

        public string Job { get; private set; }

        public bool IsPlayer { get; private set; }

        internal CharacterInfo(Character character, bool isPlayer)
        {
            Id = character.ID;
            Name = character.Name;
            LastName = character.LastName;
            FullName = character.FullName;
            GenderString = character.GenderString;
            Age = character.Age;
            Relationship = character.Relationship;
            Job = character.Job;
            IsPlayer = isPlayer;
        }
    }

    public static class Characters
    {
        internal const string Owner = "obenseuer.moddingframework.characters";

        public static List<CharacterInfo> All()
        {
            List<CharacterInfo> list = new List<CharacterInfo>();
            try
            {
                List<Character> source = Source();
                for (int i = 0; i < source.Count; i++)
                {
                    if (source[i] != null) list.Add(new CharacterInfo(source[i], i == 0));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Characters.All", e);
            }
            return list;
        }

        public static CharacterInfo Get(int id)
        {
            foreach (CharacterInfo character in All())
            {
                if (character.Id == id) return character;
            }
            return null;
        }

        public static List<SavedCharacterInfo> Saved()
        {
            List<SavedCharacterInfo> list = new List<SavedCharacterInfo>();
            try
            {
                foreach (SaveController.CharacterSaves character in SaveController.GetCharacters())
                {
                    DateTime lastPlayed = DateTime.MinValue;
                    try
                    {
                        lastPlayed = LastWritten(character.FolderName);
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(Owner, "Characters.Saved " + character.FolderName, e);
                    }
                    list.Add(new SavedCharacterInfo(character.CharacterDisplayName(), character.FolderName, lastPlayed));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Characters.Saved", e);
            }
            return list;
        }

        private static DateTime LastWritten(string folder)
        {
            DateTime newest = DateTime.MinValue;
            foreach (string info in Directory.GetFiles(folder, "Info.tnmt", SearchOption.AllDirectories))
            {
                DateTime written = File.GetLastWriteTime(info);
                if (written > newest) newest = written;
            }
            return newest;
        }

        private static List<Character> Source()
        {
            List<Character> list = new List<Character>();
            if (CharacterDatabase.Characters.Count > 0 || CharacterDatabase.instance != null)
            {
                list.AddRange(CharacterDatabase.Characters);
                return list;
            }
            foreach (UnityEngine.Object asset in Resources.LoadAll("Characters", typeof(Character)))
            {
                Character character = asset as Character;
                if (character != null) list.Add(character);
            }
            return list;
        }
    }
}
