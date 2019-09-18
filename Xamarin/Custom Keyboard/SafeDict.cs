using System;
using System.Collections.Generic;
using System.Text;

namespace KeyboardExtension
{
    public class SafeDict<TKey, TValue> : Dictionary<TKey, TValue>
    {
        TValue _default;
        public TValue DefaultValue
        {
            get { return _default; }
            set { _default = value; }
        }
        public SafeDict() : base() { }
        public SafeDict(TValue defaultValue) : base()
        {
            _default = defaultValue;
        }
        public new TValue this[TKey key]
        {
            get
            {
                TValue t;
                return base.TryGetValue(key, out t) ? t : _default;
            }
            set { base[key] = value; }
        }
    }

    public class Nullable2<T> where T : struct
    {

        private bool hasValue;
        internal T value;

        public Nullable2(T value)
        {
            this.value = value;
            this.hasValue = true;
        }

        public bool HasValue
        {
            get
            {
                return this.hasValue;
            }
        }

        public T Value
        {
            get
            {
                if (!this.HasValue)
                {
                    //ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NoValue);
                }
                return this.value;
            }
        }

        public T GetValueOrDefault()
        {
            return this.value;
        }

        public T GetValueOrDefault(T defaultValue)
        {
            if (!this.HasValue)
            {
                return defaultValue;
            }
            return this.value;
        }

        public override bool Equals(object other)
        {
            if (!this.HasValue)
            {
                return (other == null);
            }
            if (other == null)
            {
                return false;
            }
            return this.value.Equals(other);
        }

        public override int GetHashCode()
        {
            if (!this.HasValue)
            {
                return 0;
            }
            return this.value.GetHashCode();
        }

        public override string ToString()
        {
            if (!this.HasValue)
            {
                return "";
            }
            return this.value.ToString();
        }

        public static implicit operator Nullable2<T>(T value)
        {
            return new Nullable2<T>(value);
        }

        public static explicit operator T(Nullable2<T> value)
        {
            return value.Value;
        }
    }
}
