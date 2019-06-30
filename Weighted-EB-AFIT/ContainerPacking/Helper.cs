using System;
using System.Collections;
using System.Collections.Generic;

namespace ContainerPacking
{
    static class Helper
    {
        public class CandidatesThatFitComparer : IComparer
        {
            public Vector3 Gap;
            public double GapZStart;
            public double Weight;
            public double DimensionalWeight;
            public double Thickness;
            public Dictionary<string, int> ItemsPackedQty;

            public int Compare(object x, object y)
            {
                return Compare((DimHolder)x, (DimHolder)y);
            }

            public int Compare(DimHolder x, DimHolder y)
            {
                int result;

                if (x.Item.Weight + Weight > DimensionalWeight)
                {
                    result = -(ItemsPackedQty[x.Item.Id].CompareTo(ItemsPackedQty[y.Item.Id]));

                    if (result != 0)
                    {
                        return result;
                    }

                }

                result = -((x.Item.Weight + Weight - DimensionalWeight).CompareTo((y.Item.Weight + Weight - DimensionalWeight)));

                if (result != 0)
                {
                    return result;
                }

                result = (Thickness - x.Dims.Y).CompareTo(Thickness - y.Dims.Y);

                if (result != 0)
                {
                    return result;
                }

                result = (Gap.X - x.Dims.X).CompareTo(Gap.X - y.Dims.X);

                if (result != 0)
                {
                    return result;
                }

                result = Math.Abs(GapZStart - x.Dims.Z).CompareTo(Math.Abs(GapZStart - y.Dims.Z));

                if (result != 0)
                {
                    return result;
                }

                return 0;
            }
        }

        public class CandidatesThatDoNotFitComparer : IComparer
        {
            public Vector3 Gap;
            public double GapZStart;
            public double Weight;
            public double DimensionalWeight;
            public double Thickness;
            public Dictionary<string, int> ItemsPackedQty;

            public int Compare(object x, object y)
            {
                return Compare((DimHolder)x, (DimHolder)y);
            }

            public int Compare(DimHolder x, DimHolder y)
            {
                int result;

                //Uncomment this to resort original functionality
                //if (x.Item.Weight + Weight > DimensionalWeight)
                {
                    result = -(ItemsPackedQty[x.Item.Id].CompareTo(ItemsPackedQty[y.Item.Id]));

                    if (result != 0)
                    {
                        return result;
                    }

                }

                result = -((x.Item.Weight + Weight - DimensionalWeight).CompareTo((y.Item.Weight + Weight - DimensionalWeight)));

                if (result != 0)
                {
                    return result;
                }

                result = (x.Dims.Y - Thickness).CompareTo(y.Dims.Y - Thickness);

                if (result != 0)
                {
                    return result;
                }

                result = (Gap.X - x.Dims.X).CompareTo(Gap.X - y.Dims.X);

                if (result != 0)
                {
                    return result;
                }

                result = Math.Abs(GapZStart - x.Dims.Z).CompareTo(Math.Abs(GapZStart - y.Dims.Z));

                if (result != 0)
                {
                    return result;
                }

                return 0;
            }
        }
    }
}
