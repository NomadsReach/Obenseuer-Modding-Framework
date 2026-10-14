using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class ComputerDocInfo
    {
        public string Title { get; private set; }
        public string Sender { get; private set; }
        public string Body { get; private set; }

        internal ComputerDocInfo(string title, string sender, string body)
        {
            Title = title;
            Sender = sender;
            Body = body;
        }
    }

    public sealed class ComputerInfo
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Scene { get; private set; }

        internal ComputerInfo(string id, string title, string scene)
        {
            Id = id;
            Title = title;
            Scene = scene;
        }
    }

    public static class ComputerContents
    {
        internal const string Owner = "obenseuer.moddingframework.computerfiles";
        private static readonly FieldInfo emailsField = AccessTools.Field(typeof(InteractableComputer), "emails");
        private static readonly FieldInfo filesField = AccessTools.Field(typeof(InteractableComputer), "recycleBinFiles");

        public static List<ComputerInfo> All()
        {
            return Guard(() => ListAll(), new List<ComputerInfo>(), "All");
        }

        public static string IdOf(GameObject computer)
        {
            if (computer == null) return null;
            return Guard(() => IdFor(computer.GetComponent<InteractableComputer>()), null, "IdOf");
        }

        public static List<ComputerDocInfo> Files(string computerId)
        {
            Require(computerId);
            return Guard(() => FilesOf(computerId), new List<ComputerDocInfo>(), "Files");
        }

        public static List<ComputerDocInfo> Emails(string computerId)
        {
            Require(computerId);
            return Guard(() => EmailsOf(computerId), new List<ComputerDocInfo>(), "Emails");
        }

        private static void Require(string computerId)
        {
            if (computerId == null) throw new ArgumentException("computerId must not be null", "computerId");
        }

        private static string IdFor(InteractableComputer computer)
        {
            if (computer == null) return null;
            return SceneIds.Of(computer);
        }

        private static List<ComputerInfo> ListAll()
        {
            List<ComputerInfo> result = new List<ComputerInfo>();
            foreach (InteractableComputer computer in SceneIds.Active<InteractableComputer>())
            {
                if (computer == null) continue;
                result.Add(new ComputerInfo(IdFor(computer), computer.title ?? "", computer.gameObject.scene.name));
            }
            return result;
        }

        internal static InteractableComputer FindComputer(string computerId)
        {
            if (computerId.Length == 0) return null;
            foreach (InteractableComputer computer in SceneIds.Active<InteractableComputer>())
            {
                if (computer != null && IdFor(computer) == computerId) return computer;
            }
            return null;
        }

        private static List<ComputerDocInfo> FilesOf(string computerId)
        {
            List<ComputerDocInfo> result = new List<ComputerDocInfo>();
            InteractableComputer computer = FindComputer(computerId);
            if (computer == null) return result;
            List<ComputerFile> files = (List<ComputerFile>)filesField.GetValue(computer);
            if (files == null) return result;
            foreach (ComputerFile file in files)
            {
                if (file == null) continue;
                string body = file.textFile == null || file.textFile.content == null ? "" : file.textFile.content;
                result.Add(new ComputerDocInfo(file.fileName ?? "", "", body));
            }
            return result;
        }

        private static List<ComputerDocInfo> EmailsOf(string computerId)
        {
            List<ComputerDocInfo> result = new List<ComputerDocInfo>();
            InteractableComputer computer = FindComputer(computerId);
            if (computer == null) return result;
            List<Email> emails = (List<Email>)emailsField.GetValue(computer);
            if (emails == null) return result;
            foreach (Email email in emails)
            {
                if (email == null) continue;
                result.Add(new ComputerDocInfo(email.title ?? "", email.from ?? "", email.content ?? ""));
            }
            return result;
        }

        private static T Guard<T>(Func<T> read, T fallback, string name)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Debug(name, name + " failed: " + e.ToString());
                return fallback;
            }
        }
    }
}
