using System.Linq;

namespace ContainerPacking
{
    public class Item
    {
        public readonly string Id;
        public readonly Vector3 Dimensions;
        public readonly double Volume;

        public int Quantity;
        public double Weight;
        public Vector3 Coordinate;
        public Vector3 PackedDimensions;
        public Vector3[] Orientations;

        public Item(string id, Vector3 dim, int quantity, double weight)
        {
            Id = id;
            Dimensions = dim;
            Volume = Dimensions.Volume;
            Quantity = quantity;
            Coordinate = new Vector3(double.MaxValue, double.MaxValue, double.MaxValue);
            Weight = weight;

            Orientations = new[]
            {
                Dimensions,
                new Vector3(Dimensions.X, Dimensions.Z, Dimensions.Y),
                new Vector3(Dimensions.Y, Dimensions.X, Dimensions.Z),
                new Vector3(Dimensions.Y, Dimensions.Z, Dimensions.X),
                new Vector3(Dimensions.Z, Dimensions.X, Dimensions.Y),
                new Vector3(Dimensions.Z, Dimensions.Y, Dimensions.X)
            };
        }

        public bool Fits(Vector3 gap)
        {
            return Orientations.Any(dims => dims.Fits(gap));
        }
    }
}
