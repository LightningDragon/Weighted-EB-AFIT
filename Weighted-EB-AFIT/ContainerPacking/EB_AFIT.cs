using System;
using System.Collections.Generic;
using System.Linq;

namespace ContainerPacking
{
    public class EB_AFIT
    {
        #region Public Methods

        public static PackingResult Run(Container container, IEnumerable<Item> itemsToPack, double dimFactor = 1)
        {
            var items = itemsToPack.Select(x => new Item(x.Id, x.Dimensions, x.Quantity, x.Weight)).ToArray();
            var itemsPackedInOrder = ExecuteIterations(container, items, dimFactor);

            return new PackingResult
            {
                Container = container,
                UnpackedItems = items.Where(x => x.Quantity > 0).SelectMany(x => Enumerable.Range(0, x.Quantity).Select(y => new Item(x.Id, x.Dimensions, 1, x.Weight))).ToArray(),
                PackedItems = itemsPackedInOrder.ToArray(),
                Weight = itemsPackedInOrder.Sum(x => x.Weight),
            };
        }

        #endregion Public Methods

        #region Private Methods

        private static Item[] ExecuteIterations(Container container, Item[] items, double dimFactor)
        {
            var hundredPercentPacked = false;
            var bestVolume = 0.0;
            var bestDimWeight = double.MaxValue;
            Layer bestIteration = null;
            var totalItemVolume = items.Sum(j => j.Volume * j.Quantity);

            var orientations = new[]
            {
                new Container(container.Id, new Vector3(container.Dimensions.X, container.Dimensions.Z, container.Dimensions.Y), container.MaximumWeight),
                new Container(container.Id, new Vector3(container.Dimensions.Y, container.Dimensions.Z, container.Dimensions.X), container.MaximumWeight),
                new Container(container.Id, new Vector3(container.Dimensions.Y, container.Dimensions.X, container.Dimensions.Z), container.MaximumWeight),
                new Container(container.Id, new Vector3(container.Dimensions.Z, container.Dimensions.X, container.Dimensions.Y), container.MaximumWeight),
                container,
                new Container(container.Id, new Vector3(container.Dimensions.Z, container.Dimensions.Y, container.Dimensions.X), container.MaximumWeight)
            };

            for (int o = container.Dimensions.X == container.Dimensions.Z && container.Dimensions.Z == container.Dimensions.Y ? 5 : 0; o < 6 && !hundredPercentPacked; o++)
            {
                var box = orientations[o];

                foreach (var layer in ListCanditLayers(box, items).TakeWhile(x => !hundredPercentPacked))
                {
                    layer.Orientation = o;

                    layer.Pack(box, items.Select(x => new Item(x.Id, x.Dimensions, x.Quantity, x.Weight)).ToArray(), false, totalItemVolume, dimFactor);
                    hundredPercentPacked = !layer.DonePacking;

                    var differenceFromIdealWeight = Math.Abs(layer.Weight - layer.DimensionalWeight);

                    if (layer.Volume > bestVolume || bestDimWeight > differenceFromIdealWeight)
                    {
                        bestVolume = layer.Volume;
                        bestDimWeight = differenceFromIdealWeight;
                        bestIteration = layer;
                    }
                }
            }

            if (bestIteration != null)
            {
                Report(bestIteration, orientations[bestIteration.Orientation], items, totalItemVolume, dimFactor);
                return bestIteration.ItemsPackedInOrder.ToArray();
            }

            return new Item[] { };
        }

        private static Layer[] ListCanditLayers(Container container, Item[] items)
        {
            var layers = new List<Layer>();

            foreach (var itemX in items)
            {
                var itemOrientations = new[]
                {
                    new Vector3(itemX.Dimensions.X, itemX.Dimensions.Y, itemX.Dimensions.Z),
                    new Vector3(itemX.Dimensions.Y, itemX.Dimensions.X, itemX.Dimensions.Z),
                    new Vector3(itemX.Dimensions.Z, itemX.Dimensions.X, itemX.Dimensions.Y)
                };

                foreach (var itemDimensions in itemOrientations)
                {
                    if (!(itemDimensions.X > container.Dimensions.Y) && (!(itemDimensions.Y > container.Dimensions.X) && !(itemDimensions.Z > container.Dimensions.Z) || !(itemDimensions.Z > container.Dimensions.X) && !(itemDimensions.Y > container.Dimensions.Z)))
                    {
                        if (!layers.Any(j => j.LayerDimension == itemDimensions.X))
                        {
                            double layereval = 0;

                            foreach (var itemZ in items)
                            {
                                if (itemX != itemZ)
                                {
                                    var Dimensiondif = Math.Abs(itemDimensions.X - itemZ.Dimensions.X);

                                    if (Math.Abs(itemDimensions.X - itemZ.Dimensions.Y) < Dimensiondif)
                                    {
                                        Dimensiondif = Math.Abs(itemDimensions.X - itemZ.Dimensions.Y);
                                    }

                                    if (Math.Abs(itemDimensions.X - itemZ.Dimensions.Z) < Dimensiondif)
                                    {
                                        Dimensiondif = Math.Abs(itemDimensions.X - itemZ.Dimensions.Z);
                                    }

                                    layereval = layereval + Dimensiondif;
                                }
                            }

                            layers.Add(new Layer { LayerEval = layereval, LayerDimension = itemDimensions.X, Thickness = itemDimensions.X, RemainingY = container.Dimensions.Y, RemainingZ = container.Dimensions.Z });
                        }
                    }
                }
            }

            return layers.OrderBy(l => l.LayerEval).ToArray();
        }

        private static void Report(Layer layer, Container container, Item[] items, double totalItemVolume, double dimFactor)
        {
            //ListCanditLayers(box, items);
            layer.Pack(container, items, true, totalItemVolume, dimFactor);
        }

        #endregion Private Methods

        #region Private Classes

        private class Layer
        {
            private Vector3 _bbox;
            private Vector3 _box;
            private Vector3 _cbox;
            private LinkedList<Pad> SmallestZList;
            private LinkedListNode<Pad> SmallestZ;

            public readonly List<Item> ItemsPackedInOrder = new List<Item>();
            public Dictionary<string, int> ItemsPackedQty;

            public int Orientation;

            public bool Done;
            public bool DonePacking;

            public double LayerDimension;
            public double LayerEval;
            public double Volume;
            public double DimensionalWeight;
            public double Weight;
            public double Thickness;
            public double RemainingY;
            public double RemainingZ;
            public double LilZ;
            public double PackedY;
            public double LayerInLayer;
            public double PreviousThickness;
            public double PreviousPackedY;
            public double PreviousRemainingY;

            public void Pack(Container container, Item[] items, bool packingBest, double totalItemVolume, double dimFactor)
            {
                DimensionalWeight = new Vector3(Math.Round(container.Dimensions.X), Math.Round(container.Dimensions.Y), Math.Round(container.Dimensions.Z)).Volume / dimFactor;
                Weight = 0;
                Volume = 0;
                PackedY = 0;
                Thickness = LayerDimension;
                RemainingY = container.Dimensions.Y;
                RemainingZ = container.Dimensions.Z;
                DonePacking = false;


                var itemDims = items.SelectMany(x => x.Orientations.Select(y => new DimHolder(x, y))).ToArray();
                ItemsPackedQty = itemDims.Select(x => x.Item.Id).Distinct().ToDictionary(x => x, y => 0);
                while (!DonePacking)
                {
                    LayerInLayer = 0;

                    PackLayer(container, itemDims, packingBest, totalItemVolume);

                    PackedY = PackedY + Thickness;
                    RemainingY = container.Dimensions.Y - PackedY;

                    if (LayerInLayer != 0)
                    {
                        PreviousPackedY = PackedY;
                        PreviousRemainingY = RemainingY;
                        RemainingY = Thickness - PreviousThickness;
                        PackedY = PackedY - Thickness + PreviousThickness;
                        RemainingZ = LilZ;
                        Thickness = LayerInLayer;

                        PackLayer(container, itemDims, packingBest, totalItemVolume);

                        PackedY = PreviousPackedY;
                        RemainingY = PreviousRemainingY;
                        RemainingZ = container.Dimensions.Z;
                    }

                    Thickness = Find(container, items);
                }
            }

            public void PackLayer(Container container, DimHolder[] items, bool packingBest, double totalItemVolume)
            {
                Done = false;
                var totalVector3Volume = container.Volume;
                var maximumWeight = container.MaximumWeight;
                SmallestZList = new LinkedList<Pad>();
                SmallestZList.AddFirst(new Pad { CumX = container.Dimensions.X, CumZ = 0 });

                if (Thickness > 0)
                {
                    while (!Done)
                    {
                        Item item;
                        SmallestZ = FindSmallestZ();

                        if (SmallestZ.Previous == null && SmallestZ.Next == null)
                        {
                            //*** SITUATION-1: NO BOXES ON THE RIGHT AND LEFT SIDES ***

                            var gapX = SmallestZ.Value.CumX;
                            var gapZStart = RemainingZ - SmallestZ.Value.CumZ;
                            var gapWeight = container.MaximumWeight - Weight;
                            item = FindBox(new Vector3(gapX, RemainingY, gapZStart), gapZStart, gapWeight, items);

                            if (item != null)
                            {
                                item.Coordinate.X = 0;
                                item.Coordinate.Y = PackedY;
                                item.Coordinate.Z = SmallestZ.Value.CumZ;
                                if (_cbox.X == SmallestZ.Value.CumX)
                                {
                                    SmallestZ.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                }
                                else
                                {
                                    SmallestZ.List.AddAfter(SmallestZ, new Pad { CumX = SmallestZ.Value.CumX, CumZ = SmallestZ.Value.CumZ });
                                    SmallestZ.Value.CumX = _cbox.X;
                                    SmallestZ.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                }
                            }
                        }
                        else
                        {
                            if (SmallestZ.Previous == null)
                            {
                                //*** SITUATION-2: NO BOXES ON THE LEFT SIDE ***

                                var gapX = SmallestZ.Value.CumX;
                                var gapZStart = SmallestZ.Next.Value.CumZ - SmallestZ.Value.CumZ;
                                var gapZ = RemainingZ - SmallestZ.Value.CumZ;
                                var gapWeight = container.MaximumWeight - Weight;
                                item = FindBox(new Vector3(gapX, RemainingY, gapZ), gapZStart, gapWeight, items);

                                if (item != null)
                                {
                                    item.Coordinate.Y = PackedY;
                                    item.Coordinate.Z = SmallestZ.Value.CumZ;
                                    if (_cbox.X == SmallestZ.Value.CumX)
                                    {
                                        item.Coordinate.X = 0;

                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Next.Value.CumZ)
                                        {
                                            SmallestZ.Value.CumZ = SmallestZ.Next.Value.CumZ;
                                            SmallestZ.Value.CumX = SmallestZ.Next.Value.CumX;

                                            SmallestZ.List.Remove(SmallestZ.Next);
                                        }
                                        else
                                        {
                                            SmallestZ.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                    else
                                    {
                                        item.Coordinate.X = SmallestZ.Value.CumX - _cbox.X;

                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Next.Value.CumZ)
                                        {
                                            SmallestZ.Value.CumX = SmallestZ.Value.CumX - _cbox.X;
                                        }
                                        else
                                        {
                                            SmallestZ.List.AddAfter(SmallestZ, new Pad());
                                            SmallestZ.Next.Value.CumX = SmallestZ.Value.CumX;
                                            SmallestZ.Value.CumX = SmallestZ.Value.CumX - _cbox.X;
                                            SmallestZ.Next.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                }
                            }
                            else if (SmallestZ.Next == null)
                            {
                                //*** SITUATION-3: NO BOXES ON THE RIGHT SIDE ***

                                var gapX = SmallestZ.Value.CumX - SmallestZ.Previous.Value.CumX;
                                var gapZStart = SmallestZ.Previous.Value.CumZ - SmallestZ.Value.CumZ;
                                var gapZ = RemainingZ - SmallestZ.Value.CumZ;
                                var gapWeight = container.MaximumWeight - Weight;
                                item = FindBox(new Vector3(gapX, RemainingY, gapZ), gapZStart, gapWeight, items);

                                if (item != null)
                                {
                                    item.Coordinate.Y = PackedY;
                                    item.Coordinate.Z = SmallestZ.Value.CumZ;
                                    item.Coordinate.X = SmallestZ.Previous.Value.CumX;

                                    if (_cbox.X == SmallestZ.Value.CumX - SmallestZ.Previous.Value.CumX)
                                    {
                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Previous.Value.CumZ)
                                        {
                                            SmallestZ.Previous.Value.CumX = SmallestZ.Value.CumX;
                                            SmallestZ.List.Remove(SmallestZ);
                                        }
                                        else
                                        {
                                            SmallestZ.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                    else
                                    {
                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Previous.Value.CumZ)
                                        {
                                            SmallestZ.Previous.Value.CumX = SmallestZ.Previous.Value.CumX + _cbox.X;
                                        }
                                        else
                                        {
                                            SmallestZ.List.AddBefore(SmallestZ, new Pad());

                                            SmallestZ.Previous.Value.CumX = SmallestZ.Previous.Previous.Value.CumX + _cbox.X;
                                            SmallestZ.Previous.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                }
                            }
                            else if (SmallestZ.Previous.Value.CumZ == SmallestZ.Next.Value.CumZ)
                            {
                                //*** SITUATION-4: THERE ARE BOXES ON BOTH OF THE SIDES ***

                                //*** SUBSITUATION-4A: SIDES ARE EQUAL TO EACH OTHER ***

                                var gapX = SmallestZ.Value.CumX - SmallestZ.Previous.Value.CumX;
                                var gapZStart = SmallestZ.Previous.Value.CumZ - SmallestZ.Value.CumZ;
                                var gapZ = RemainingZ - SmallestZ.Value.CumZ;
                                var gapWeight = container.MaximumWeight - Weight;
                                item = FindBox(new Vector3(gapX, RemainingY, gapZ), gapZStart, gapWeight, items);

                                if (item != null)
                                {
                                    item.Coordinate.Y = PackedY;
                                    item.Coordinate.Z = SmallestZ.Value.CumZ;

                                    if (_cbox.X == SmallestZ.Value.CumX - SmallestZ.Previous.Value.CumX)
                                    {
                                        item.Coordinate.X = SmallestZ.Previous.Value.CumX;

                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Next.Value.CumZ)
                                        {
                                            SmallestZ.Previous.Value.CumX = SmallestZ.Next.Value.CumX;

                                            if (SmallestZ.Next.Next != null)
                                            {
                                                SmallestZ.List.Remove(SmallestZ);
                                            }
                                            else
                                            {
                                                SmallestZ.List.Remove(SmallestZ);
                                            }
                                        }
                                        else
                                        {
                                            SmallestZ.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                    else if (SmallestZ.Previous.Value.CumX < container.Dimensions.X - SmallestZ.Value.CumX)
                                    {
                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Previous.Value.CumZ)
                                        {
                                            SmallestZ.Value.CumX = SmallestZ.Value.CumX - _cbox.X;
                                            item.Coordinate.X = SmallestZ.Value.CumX - _cbox.X;
                                        }
                                        else
                                        {
                                            item.Coordinate.X = SmallestZ.Previous.Value.CumX;
                                            SmallestZ.List.AddBefore(SmallestZ, new Pad());
                                            SmallestZ.Previous.Value.CumX =
                                                SmallestZ.Previous.Previous.Value.CumX + _cbox.X;
                                            SmallestZ.Previous.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                    else
                                    {
                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Previous.Value.CumZ)
                                        {
                                            SmallestZ.Previous.Value.CumX = SmallestZ.Previous.Value.CumX + _cbox.X;
                                            item.Coordinate.X = SmallestZ.Previous.Value.CumX;
                                        }
                                        else
                                        {
                                            item.Coordinate.X = SmallestZ.Value.CumX - _cbox.X;
                                            SmallestZ.List.AddAfter(SmallestZ, new Pad());
                                            SmallestZ.Next.Value.CumX = SmallestZ.Value.CumX;
                                            SmallestZ.Next.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                            SmallestZ.Value.CumX = SmallestZ.Value.CumX - _cbox.X;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //*** SUBSITUATION-4B: SIDES ARE NOT EQUAL TO EACH OTHER ***

                                var gapX = SmallestZ.Value.CumX - SmallestZ.Previous.Value.CumX;
                                var gapZStart = SmallestZ.Previous.Value.CumZ - SmallestZ.Value.CumZ;
                                var gapZ = RemainingZ - SmallestZ.Value.CumZ;
                                var gapWeight = container.MaximumWeight - Weight;
                                item = FindBox(new Vector3(gapX, RemainingY, gapZ), gapZStart, gapWeight, items);

                                if (item != null)
                                {
                                    item.Coordinate.Y = PackedY;
                                    item.Coordinate.Z = SmallestZ.Value.CumZ;
                                    item.Coordinate.X = SmallestZ.Previous.Value.CumX;

                                    if (_cbox.X == SmallestZ.Value.CumX - SmallestZ.Previous.Value.CumX)
                                    {
                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Previous.Value.CumZ)
                                        {
                                            SmallestZ.Previous.Value.CumX = SmallestZ.Value.CumX;
                                            SmallestZ.List.Remove(SmallestZ);
                                        }
                                        else
                                        {
                                            SmallestZ.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                    else
                                    {
                                        if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Previous.Value.CumZ)
                                        {
                                            SmallestZ.Previous.Value.CumX = SmallestZ.Previous.Value.CumX + _cbox.X;
                                        }
                                        else if (SmallestZ.Value.CumZ + _cbox.Z == SmallestZ.Next.Value.CumZ)
                                        {
                                            item.Coordinate.X = SmallestZ.Value.CumX - _cbox.X;
                                            SmallestZ.Value.CumX = SmallestZ.Value.CumX - _cbox.X;
                                        }
                                        else
                                        {
                                            SmallestZ.List.AddBefore(SmallestZ, new Pad());

                                            SmallestZ.Previous.Value.CumX =
                                                SmallestZ.Previous.Previous.Value.CumX + _cbox.X;
                                            SmallestZ.Previous.Value.CumZ = SmallestZ.Value.CumZ + _cbox.Z;
                                        }
                                    }
                                }
                            }
                        }

                        if (item != null)
                        {
                            item.Quantity -= 1;
                            var itemToPack = VolumeAndWeightCheck(new Item(item.Id, item.Dimensions, 1, item.Weight), totalVector3Volume, totalItemVolume, maximumWeight);

                            if (packingBest)
                            {
                                ItemsPackedInOrder.Add(itemToPack);
                                ItemsPackedQty[item.Id]++;
                            }
                        }
                    }
                }
                else
                {
                    DonePacking = true;
                }
            }

            private LinkedListNode<Pad> FindSmallestZ()
            {
                var item = SmallestZList.First;
                var smallestZ = item;

                while (item.Next != null)
                {
                    if (item.Next.Value.CumZ < smallestZ.Value.CumZ)
                    {
                        smallestZ = item.Next;
                    }

                    item = item.Next;
                }

                return smallestZ;
            }

            private Item FindBox(Vector3 gap, double gapZStart, double gapWeight, DimHolder[] items)
            {
                Item boxi = null;
                Item bboxi = null;

                var candidatesThatFit = new DimHolder[items.Length];

                {
                    var newSize = 0;
                    foreach (var x in items)
                    {
                        if (x.Item.Quantity > 0 && x.Dims.Fits(gap) && x.Dims.Y <= Thickness && x.Item.Weight < gapWeight)
                        {
                            candidatesThatFit[newSize++] = x;
                        }
                    }

                    Array.Resize(ref candidatesThatFit, newSize);
                }

                if (candidatesThatFit.Length > 0)
                {
                    var candidatesThatFitComparer = new Helper.CandidatesThatFitComparer();
                    candidatesThatFitComparer.Gap = gap;
                    candidatesThatFitComparer.Thickness = Thickness;
                    candidatesThatFitComparer.GapZStart = gapZStart;
                    candidatesThatFitComparer.ItemsPackedQty = ItemsPackedQty;
                    candidatesThatFitComparer.Weight = Weight;
                    candidatesThatFitComparer.DimensionalWeight = DimensionalWeight;
                    Array.Sort(candidatesThatFit, candidatesThatFitComparer);

                    var best = candidatesThatFit[0];
                    boxi = best.Item;
                    _box = best.Dims;
                }
                else
                {
                    var candidatesThatDoNotFit = new DimHolder[items.Length];

                    var newSize = 0;
                    foreach (var x in items)
                    {
                        if (x.Item.Quantity > 0 && x.Dims.Fits(gap) && !(x.Dims.Y <= Thickness) && !(x.Item.Weight < gapWeight))
                        {
                            candidatesThatDoNotFit[newSize++] = x;
                        }
                    }

                    Array.Resize(ref candidatesThatDoNotFit, newSize);

                    if (candidatesThatDoNotFit.Length > 0)
                    {
                        var candidatesThatDoNotFitComparer = new Helper.CandidatesThatDoNotFitComparer();
                        candidatesThatDoNotFitComparer.Gap = gap;
                        candidatesThatDoNotFitComparer.Thickness = Thickness;
                        candidatesThatDoNotFitComparer.GapZStart = gapZStart;
                        candidatesThatDoNotFitComparer.ItemsPackedQty = ItemsPackedQty;
                        candidatesThatDoNotFitComparer.Weight = Weight;
                        candidatesThatDoNotFitComparer.DimensionalWeight = DimensionalWeight;
                        Array.Sort(candidatesThatDoNotFit, candidatesThatDoNotFitComparer);

                        var best = candidatesThatDoNotFit[0];
                        bboxi = best.Item;
                        _bbox = best.Dims;
                    }
                }

                return CheckFound(boxi, bboxi);
            }

            private Item CheckFound(Item boxi, Item bboxi)
            {
                Item item = null;

                if (boxi != null)
                {
                    item = boxi;
                    _cbox = _box;
                }
                else
                {
                    if (bboxi != null && (LayerInLayer != 0 || SmallestZ.Previous == null && SmallestZ.Next == null))
                    {
                        if (LayerInLayer == 0)
                        {
                            PreviousThickness = Thickness;
                            LilZ = SmallestZ.Value.CumZ;
                        }

                        item = bboxi;
                        _cbox = _bbox;
                        LayerInLayer = LayerInLayer + _bbox.Y - Thickness;
                        Thickness = _bbox.Y;
                    }
                    else
                    {
                        if (SmallestZ.Previous == null && SmallestZ.Next == null)
                        {
                            Done = true;
                        }
                        else
                        {
                            if (SmallestZ.Previous == null)
                            {
                                SmallestZ.Value.CumX = SmallestZ.Next.Value.CumX;
                                SmallestZ.Value.CumZ = SmallestZ.Next.Value.CumZ;

                                SmallestZ.List.Remove(SmallestZ.Next);
                            }
                            else if (SmallestZ.Next == null)
                            {
                                SmallestZ.Previous.Value.CumX = SmallestZ.Value.CumX;
                                SmallestZ.List.Remove(SmallestZ);
                            }
                            else
                            {
                                if (SmallestZ.Previous.Value.CumZ == SmallestZ.Next.Value.CumZ)
                                {
                                    SmallestZ.Previous.Value.CumX = SmallestZ.Next.Value.CumX;
                                    SmallestZ.List.Remove(SmallestZ.Next);
                                    SmallestZ.List.Remove(SmallestZ);
                                }
                                else
                                {
                                    if (SmallestZ.Previous.Value.CumZ < SmallestZ.Next.Value.CumZ)
                                    {
                                        SmallestZ.Previous.Value.CumX = SmallestZ.Value.CumX;
                                    }

                                    SmallestZ.List.Remove(SmallestZ);
                                }
                            }
                        }
                    }
                }

                return item;
            }

            private Item VolumeAndWeightCheck(Item item, double totalVector3Volume, double totalItemVolume, double maximumWeight)
            {
                item.PackedDimensions = _cbox;
                Volume += item.Volume;
                Weight += item.Weight;

                if (Volume == totalVector3Volume || Volume == totalItemVolume || Weight == maximumWeight)
                {
                    DonePacking = true;
                }

                Vector3 packCoordinate = Vector3.Zero;
                Vector3 packDimension = Vector3.Zero;

                switch (Orientation)
                {
                    case 0:
                        packCoordinate = item.Coordinate;
                        packDimension = item.PackedDimensions;
                        break;

                    case 1:
                        packCoordinate.X = item.Coordinate.Z;
                        packCoordinate.Y = item.Coordinate.Y;
                        packCoordinate.Z = item.Coordinate.X;
                        packDimension.X = item.PackedDimensions.Z;
                        packDimension.Y = item.PackedDimensions.Y;
                        packDimension.Z = item.PackedDimensions.X;
                        break;

                    case 2:
                        packCoordinate.X = item.Coordinate.Y;
                        packCoordinate.Y = item.Coordinate.Z;
                        packCoordinate.Z = item.Coordinate.X;
                        packDimension.X = item.PackedDimensions.Y;
                        packDimension.Y = item.PackedDimensions.Z;
                        packDimension.Z = item.PackedDimensions.X;
                        break;

                    case 3:
                        packCoordinate.X = item.Coordinate.Y;
                        packCoordinate.Y = item.Coordinate.X;
                        packCoordinate.Z = item.Coordinate.Z;
                        packDimension.X = item.PackedDimensions.Y;
                        packDimension.Y = item.PackedDimensions.X;
                        packDimension.Z = item.PackedDimensions.Z;
                        break;

                    case 4:
                        packCoordinate.X = item.Coordinate.X;
                        packCoordinate.Y = item.Coordinate.Z;
                        packCoordinate.Z = item.Coordinate.Y;
                        packDimension.X = item.PackedDimensions.X;
                        packDimension.Y = item.PackedDimensions.Z;
                        packDimension.Z = item.PackedDimensions.Y;
                        break;

                    case 5:
                        packCoordinate.X = item.Coordinate.Z;
                        packCoordinate.Y = item.Coordinate.X;
                        packCoordinate.Z = item.Coordinate.Y;
                        packDimension.X = item.PackedDimensions.Z;
                        packDimension.Y = item.PackedDimensions.X;
                        packDimension.Z = item.PackedDimensions.Y;
                        break;
                }

                item.Coordinate = packCoordinate;
                item.PackedDimensions = packDimension;

                return item;
            }

            public double Find(Container container, Item[] items)
            {
                //double eval = 8388608;
                double eval = double.MaxValue;
                //double eval = 1000000;
                double layerThickness = 0;

                foreach (var itemX in items)
                {
                    var itemOrientations = new[]
                    {
                        itemX.Dimensions,
                        new Vector3(itemX.Dimensions.Y, itemX.Dimensions.X, itemX.Dimensions.Z),
                        new Vector3(itemX.Dimensions.Z, itemX.Dimensions.X, itemX.Dimensions.Y)
                        };

                    foreach (var itemDimensions in itemOrientations)
                    {
                        double layereval = 0;

                        if (itemDimensions.X <= RemainingY && (itemDimensions.Y <= container.Dimensions.X && itemDimensions.Z <= container.Dimensions.Z || itemDimensions.Z <= container.Dimensions.X && itemDimensions.Y <= container.Dimensions.Z))
                        {
                            for (int iX = 0; iX < itemX.Quantity; iX++)
                            {
                                foreach (var itemZ in items)
                                {
                                    if (itemX != itemZ)
                                    {
                                        var Dimensiondif = Math.Abs(itemDimensions.X - itemZ.Dimensions.X);

                                        if (Math.Abs(itemDimensions.X - itemZ.Dimensions.Y) < Dimensiondif)
                                        {
                                            Dimensiondif = Math.Abs(itemDimensions.X - itemZ.Dimensions.Y);
                                        }

                                        if (Math.Abs(itemDimensions.X - itemZ.Dimensions.Z) < Dimensiondif)
                                        {
                                            Dimensiondif = Math.Abs(itemDimensions.X - itemZ.Dimensions.Z);
                                        }

                                        layereval = layereval + (Dimensiondif * itemZ.Quantity);
                                    }
                                }

                                if (layereval < eval)
                                {
                                    eval = layereval;
                                    layerThickness = itemDimensions.X;
                                }
                            }
                        }
                    }
                }

                if (layerThickness == 0 || layerThickness > RemainingY) DonePacking = true;

                return layerThickness;
            }
        }

        private class Pad
        {
            public double CumX;

            public double CumZ;
        }

        #endregion Private Classes
    }
}