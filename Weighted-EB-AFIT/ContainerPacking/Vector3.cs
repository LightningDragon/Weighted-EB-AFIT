namespace ContainerPacking
{
    public struct Vector3
    {
        public static Vector3 Zero => new Vector3(0, 0, 0);

        public double X, Y, Z;

        public double Volume => X * Y * Z;

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }

        public bool Fits(Vector3 gap)
        {
            return (X <= gap.X && Y <= gap.Y && Z <= gap.Z);
        }
    }
}
