namespace BetterDrag
{
    internal interface IVector3<T>
        where T : struct
    {
        T Value { get; }
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }

        float Magnitude { get; }

        float Dot(IVector3<T> other);
        IVector3<T> Subtract(IVector3<T> other);
        IVector3<T> Cross(IVector3<T> other);
    }

    internal class UnityVector3(UnityEngine.Vector3 vector) : IVector3<UnityEngine.Vector3>
    {
        private UnityEngine.Vector3 _vector = vector;

        public UnityEngine.Vector3 Value
        {
            get { return _vector; }
        }
        public float X
        {
            get { return _vector.x; }
            set { _vector.x = value; }
        }

        public float Y
        {
            get { return _vector.y; }
            set { _vector.y = value; }
        }

        public float Z
        {
            get { return _vector.z; }
            set { _vector.z = value; }
        }

        public float Magnitude
        {
            get { return _vector.magnitude; }
        }

        public IVector3<UnityEngine.Vector3> Cross(IVector3<UnityEngine.Vector3> other)
        {
            return new UnityVector3(UnityEngine.Vector3.Cross(_vector, other.Value));
        }

        public IVector3<UnityEngine.Vector3> Subtract(IVector3<UnityEngine.Vector3> other)
        {
            return new UnityVector3(_vector - other.Value);
        }

        public float Dot(IVector3<UnityEngine.Vector3> other)
        {
            return UnityEngine.Vector3.Dot(_vector, other.Value);
        }
    }

    internal class NumericsVector3(System.Numerics.Vector3 vector)
        : IVector3<System.Numerics.Vector3>
    {
        private System.Numerics.Vector3 _vector = vector;

        public System.Numerics.Vector3 Value
        {
            get { return _vector; }
        }
        public float X
        {
            get { return _vector.X; }
            set { _vector.X = value; }
        }

        public float Y
        {
            get { return _vector.Y; }
            set { _vector.Y = value; }
        }

        public float Z
        {
            get { return _vector.Z; }
            set { _vector.Z = value; }
        }

        public float Magnitude
        {
            get { return _vector.Length(); }
        }

        public IVector3<System.Numerics.Vector3> Cross(IVector3<System.Numerics.Vector3> other)
        {
            return new NumericsVector3(System.Numerics.Vector3.Cross(_vector, other.Value));
        }

        public IVector3<System.Numerics.Vector3> Subtract(IVector3<System.Numerics.Vector3> other)
        {
            return new NumericsVector3(_vector - other.Value);
        }

        public float Dot(IVector3<System.Numerics.Vector3> other)
        {
            return System.Numerics.Vector3.Dot(_vector, other.Value);
        }
    }
}
