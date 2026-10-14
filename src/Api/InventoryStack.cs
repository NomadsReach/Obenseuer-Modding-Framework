using OS.Items;

namespace ObenseuerCore
{
    public sealed class ItemStackInfo
    {
        public int ItemId { get; private set; }
        public int Count { get; private set; }
        public int Slot { get; private set; }
        public int OwnerId { get; private set; }
        public float Quality { get; private set; }
        public int IntegrityPercent { get; private set; }
        public int LiquidItemId { get; private set; }
        public float LiquidAmount { get; private set; }
        public bool IsStolen { get; private set; }
        public bool HasOwnerPermission { get; private set; }
        public float Charge { get; private set; }
        public float MaxCharge { get; private set; }
        public float WalletCoins { get; private set; }
        public int WalletOc { get; private set; }
        public bool IsWritable { get; private set; }
        public string WrittenTitle { get; private set; }
        public string WrittenText { get; private set; }

        private ItemStackInfo(int itemId, int count, int slot, int ownerId, float quality, int integrityPercent, int liquidItemId, float liquidAmount, bool isStolen, bool hasOwnerPermission, float charge, float maxCharge, float walletCoins, int walletOc, bool isWritable, string writtenTitle, string writtenText)
        {
            ItemId = itemId;
            Count = count;
            Slot = slot;
            OwnerId = ownerId;
            Quality = quality;
            IntegrityPercent = integrityPercent;
            LiquidItemId = liquidItemId;
            LiquidAmount = liquidAmount;
            IsStolen = isStolen;
            HasOwnerPermission = hasOwnerPermission;
            Charge = charge;
            MaxCharge = maxCharge;
            WalletCoins = walletCoins;
            WalletOc = walletOc;
            IsWritable = isWritable;
            WrittenTitle = writtenTitle;
            WrittenText = writtenText;
        }

        internal static ItemStackInfo From(global::ItemStack stack, int slot = -1)
        {
            ItemQualityData quality = stack.GetQualitydata();
            ItemPerishabledata perishable = stack.GetMetaOfType<ItemPerishabledata>();
            ItemLiquidData liquid = stack.GetMetaOfType<ItemLiquidData>();
            ItemStolenData stolen = stack.GetMetaOfType<ItemStolenData>();
            ItemAmmodata ammo = stack.GetMetaOfType<ItemAmmodata>();
            ItemOSCoinWalletData wallet = stack.GetMetaOfType<ItemOSCoinWalletData>();
            ItemWritableData writable = stack.GetMetaOfType<ItemWritableData>();
            bool hasLiquid = liquid != null && liquid.liquidItem != null && liquid.liquidAmount > 0f;
            return new ItemStackInfo(
                stack.itemId,
                stack.itemAmount,
                slot,
                stack.ownerId,
                quality == null ? -1f : quality.quality,
                perishable == null ? -1 : perishable.GetIntegrityPercent(),
                hasLiquid ? liquid.liquidItem.ID : -1,
                hasLiquid ? liquid.GetLiquidAmount(stack.itemAmount) : 0f,
                stolen != null && stolen.playerStolen,
                PermitsOwner(stack.Meta),
                ammo == null ? -1f : ammo.ammo,
                ammo == null ? -1f : ammo.maxAmmo,
                wallet == null ? -1f : wallet.coins,
                wallet == null ? -1 : wallet.ToOC(),
                writable != null,
                writable == null ? null : writable.title,
                writable == null ? null : writable.content);
        }

        private static bool PermitsOwner(object[] meta)
        {
            if (meta != null)
            {
                foreach (object obj in meta)
                {
                    if (obj is ItemOwnerPermission && ((ItemOwnerPermission)obj).ownerPermission)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
