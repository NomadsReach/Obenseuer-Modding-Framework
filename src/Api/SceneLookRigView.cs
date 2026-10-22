using System;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class SceneLookRigView
    {
        private readonly GameObject holder;
        private readonly Avatar avatar;
        private readonly bool human;
        private readonly SceneLookPoseDriver driver;

        internal SceneLookRigView(GameObject holder, Avatar avatar, bool human, SceneLookPoseDriver driver)
        {
            this.holder = holder;
            this.avatar = avatar;
            this.human = human;
            this.driver = driver;
        }

        public GameObject Holder
        {
            get { return holder; }
        }

        public Avatar Avatar
        {
            get { return avatar; }
        }

        public bool IsHuman
        {
            get { return human; }
        }

        public bool HasPoseDriver
        {
            get { return driver != null; }
        }

        public bool AutoPose
        {
            get { return driver != null && driver.Auto; }
            set
            {
                if (driver == null)
                {
                    ApiLog.Once(SceneLooks.Owner, "rig auto pose", "the look on this body has no pose driver");
                    return;
                }
                try
                {
                    driver.Auto = value;
                }
                catch (Exception e)
                {
                    ApiLog.Once(SceneLooks.Owner, "rig auto pose", e);
                }
            }
        }

        public bool SyncPose()
        {
            if (driver == null)
            {
                ApiLog.Once(SceneLooks.Owner, "rig sync pose", "the look on this body has no pose driver");
                return false;
            }
            try
            {
                driver.Sync();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(SceneLooks.Owner, "rig sync pose", e);
                return false;
            }
        }
    }
}
