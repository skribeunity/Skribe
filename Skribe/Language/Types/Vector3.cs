namespace Skribe.Language.Types
{
    public class Vector3
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"Vector3({X}, {Y}, {Z})";
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3 operator *(Vector3 a, double s)
        {
            return new Vector3(a.X * s, a.Y * s, a.Z * s);
        }

        public static Vector3 operator /(Vector3 a, double s)
        {
            return new Vector3(a.X / s, a.Y / s, a.Z / s);
        }
    }
}