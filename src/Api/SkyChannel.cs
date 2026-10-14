using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal interface ISkyChannel
    {
        bool Held { get; }

        void Forget();

        void Suspend();

        void Capture();
    }

    internal sealed class SkyChannel<T> : ISkyChannel where T : struct
    {
        private readonly Func<object, T> read;
        private readonly Action<object, T> write;
        private bool held;
        private T vanilla;
        private T applied;
        private object token;

        internal SkyChannel(Func<object, T> read, Action<object, T> write)
        {
            this.read = read;
            this.write = write;
        }

        public bool Held
        {
            get
            {
                return held;
            }
        }

        public void Forget()
        {
            held = false;
        }

        public void Suspend()
        {
            if (held && EqualityComparer<T>.Default.Equals(read(token), applied)) write(token, vanilla);
        }

        public void Capture()
        {
            if (held) vanilla = read(token);
        }

        internal void Apply(object target, T? want)
        {
            if (held && !ReferenceEquals(token, target)) held = false;
            if (!want.HasValue)
            {
                if (held && EqualityComparer<T>.Default.Equals(read(token), applied)) write(token, vanilla);
                held = false;
                return;
            }
            T current = read(target);
            if (!held || !EqualityComparer<T>.Default.Equals(current, applied))
            {
                vanilla = current;
                token = target;
                held = true;
            }
            write(target, want.Value);
            applied = read(target);
        }
    }
}
