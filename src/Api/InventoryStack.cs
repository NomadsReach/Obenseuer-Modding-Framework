using OS.Items;

namespace ObenseuerCore
{
    public sealed class ItemStackInfo
    {
        public int ItemId { get; private set; }
        public int Count { get; private set; }
        public int OwnerId { get; private set; }
        public float Quality { get; private set; }
        public int IntegrityPercent { get; private set; }
        public int LiquidItemId { get; private set; }
        public float LiquidAmount { get; private set; }

        private ItemStackInfo(int itemId, int count, int ownerId, float quality, int integrityPercent, int liquidItemId, float liquidAmount)
        {
            ItemId = itemId;
            Count = count;
            OwnerId = ownerId;
            Quality = quality;
            IntegrityPercent = integrityPercent;
            LiquidItemId = liquidItemId;
            LiquidAmount = liquidAmount;
        }

        internal static ItemStackInfo From(global::ItemStack stack)
        {
            ItemQualityData quality = stack.GetQualitydata();
            ItemPerishabledata perishable = stack.GetMetaOfType<ItemPerishabledata>();
            ItemLiquidData liquid = stack.GetMetaOfType<ItemLiquidData>();
            bool hasLiquid = liquid != null && liquid.liquidItem != null && liquid.liquidAmount > 0f;
            return new ItemStackInfo(
                stack.itemId,
                stack.itemAmount,
                stack.ownerId,
                quality == null ? -1f : quality.quality,
                perishable == null ? -1 : perishable.GetIntegrityPercent(),
                hasLiquid ? liquid.liquidItem.ID : -1,
                hasLiquid ? liquid.GetLiquidAmount(stack.itemAmount) : 0f);
        }
    }
}
