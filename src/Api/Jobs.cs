using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class JobInfo
    {
        public string Id { get; private set; }

        public int SalaryPerHourOc { get; private set; }

        public float SalaryPerHourRm { get; private set; }

        public Skill Skill { get; private set; }

        public float Hardness { get; private set; }

        internal JobInfo(string id, int salaryPerHourOc, float salaryPerHourRm, Skill skill, float hardness)
        {
            Id = id;
            SalaryPerHourOc = salaryPerHourOc;
            SalaryPerHourRm = salaryPerHourRm;
            Skill = skill;
            Hardness = hardness;
        }
    }

    public sealed class WorkedArgs
    {
        public string Id { get; private set; }

        public float Hours { get; private set; }

        public float PayOc { get; private set; }

        public float PayRm { get; private set; }

        internal WorkedArgs(string id, float hours, float payOc, float payRm)
        {
            Id = id;
            Hours = hours;
            PayOc = payOc;
            PayRm = payRm;
        }
    }

    public static class Jobs
    {
        internal const string Owner = "obenseuer.moddingframework.jobs";
        private static bool installed;

        public static event Action<WorkedArgs> Worked;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<JobInfo> All()
        {
            List<JobInfo> list = new List<JobInfo>();
            try
            {
                foreach (InteractableWaitWork job in UnityEngine.Object.FindObjectsByType<InteractableWaitWork>(UnityEngine.FindObjectsSortMode.None))
                {
                    list.Add(ToInfo(job));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Jobs.All", e);
            }
            return list;
        }

        public static JobInfo Get(string id)
        {
            InteractableWaitWork job = Find(id);
            return job == null ? null : ToInfo(job);
        }

        internal static void Init()
        {
            installed = JobsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("job hooks incomplete, Worked is disabled");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Worked != null;
            }
        }

        internal static void RaiseWorked(InteractableWaitWork job, int timeWaited, float payOc, float payRm)
        {
            if (Worked == null || timeWaited <= 0) return;
            WorkedArgs args = new WorkedArgs(IdOf(job), timeWaited / 3600f, payOc, payRm);
            Events.Raise(Worked, "Jobs.Worked", h => h(args));
        }

        private static string IdOf(InteractableWaitWork job)
        {
            return SceneIds.Of(job);
        }

        private static InteractableWaitWork Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (InteractableWaitWork job in UnityEngine.Object.FindObjectsByType<InteractableWaitWork>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (IdOf(job) == id) return job;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Jobs.Find", e);
            }
            return null;
        }

        private static JobInfo ToInfo(InteractableWaitWork job)
        {
            return new JobInfo(IdOf(job), job.SalaryPerHour.OC, job.SalaryPerHour.RM, (Skill)(int)job.Skill, job.JobHardness);
        }
    }
}
