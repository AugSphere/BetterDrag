namespace BetterDrag
{
    internal readonly struct Vector3Wrapper<T>(T vector)
        where T : struct
    {
        private readonly object _wrapped = vector;

        public static implicit operator Vector3Wrapper<T>(T v) => new(v);

        public static implicit operator T(Vector3Wrapper<T> v) => (T)v._wrapped;

        internal static float Dot(T lhs, T rhs)
        {
            return (float)typeof(T).GetMethod("Dot").Invoke(null, [lhs, rhs]);
        }

        internal static Vector3Wrapper<T> Cross(T lhs, T rhs)
        {
            return (T)typeof(T).GetMethod("Cross").Invoke(null, [lhs, rhs]);
        }

        internal float x
        {
            get
            {
                var field = typeof(T).GetField("x") ?? typeof(T).GetField("X");
                return (float)field.GetValue(this._wrapped);
            }
            set
            {
                var field = typeof(T).GetField("x") ?? typeof(T).GetField("X");
                field.SetValue(this._wrapped, value);
            }
        }

        internal float magnitude
        {
            get
            {
                var prop = typeof(T).GetProperty("magnitude");
                if (prop != null)
                    return (float)prop.GetGetMethod().Invoke(this._wrapped, []);
                return (float)typeof(T).GetMethod("Length").Invoke(this._wrapped, []);
            }
        }

        internal static T right
        {
            get
            {
                Vector3Wrapper<T> v = new T();
                v.x = 1f;
                return (T)v;
            }
        }

        public static T operator -(Vector3Wrapper<T> lhs, T rhs)
        {
            return (T)typeof(T).GetMethod("op_Subtraction").Invoke(null, [(T)lhs, rhs]);
        }
    }
}
