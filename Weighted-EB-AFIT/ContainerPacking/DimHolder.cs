
namespace ContainerPacking
{
    class DimHolder
    {
        public Item Item;
        public Vector3 Dims;

        public DimHolder(Item item, Vector3 dims)
        {
            Item = item;
            Dims = dims;
        }
    }
}
