using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContainerPacking
{
    public class Container
    {
        public readonly string Id;
        public readonly Vector3 Dimensions;
        public readonly double Volume;
        public double MaximumWeight;

        public Container(string id, Vector3 dim, double maximumWeight = 31.5)
        {
            Id = id;
            Dimensions = dim;
            Volume = Dimensions.Volume;
            MaximumWeight = maximumWeight;
        }
    }
}
