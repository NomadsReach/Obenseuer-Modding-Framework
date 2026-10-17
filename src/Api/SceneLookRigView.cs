using UnityEngine;

namespace ObenseuerCore
{
    public sealed class SceneLookRigView
    {
        private readonly GameObject holder;
        private readonly Avatar avatar;
        private readonly bool human;

        internal SceneLookRigView(GameObject holder, Avatar avatar, bool human)
        {
            this.holder = holder;
            this.avatar = avatar;
            this.human = human;
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
    }
}
