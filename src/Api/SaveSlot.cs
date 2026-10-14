using System;

namespace ObenseuerCore
{
    /// <summary>
    /// A mod's save data object from SaveData.Bind, never null, saved and loaded with the mod's save.
    /// </summary>
    public sealed class SaveSlot<T> where T : class, new()
    {
        private T value = new T();

        internal SaveSlot()
        {
        }

        /// <summary>
        /// The saved object. Setting it to null throws ArgumentNullException, and a load replaces it.
        /// </summary>
        public T Value
        {
            get { return value; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                this.value = value;
            }
        }

        internal T Save()
        {
            return value;
        }

        internal void Load(T loaded)
        {
            value = loaded ?? new T();
        }
    }
}
