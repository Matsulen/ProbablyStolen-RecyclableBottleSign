using System;
using System.Collections.Generic;

namespace RecyclableSign
{
    /// <summary>
    /// Adds the "Buying Recyclables" neon sign. It behaves like the vanilla signs:
    /// sold by Dr. Jackson while the player does not own one, fits the file box,
    /// and attracts a scavenger seller while it sits in the display case.
    /// </summary>
    public sealed class ModEntry : IMod
    {
        public const string ModId = "recyclable_sign";

        /// <summary>
        /// Item id. Prefixed with the mod id: Directory.Add silently replaces an existing entry with the
        /// same id, so a bare "sign_recyclable" could overwrite, or be overwritten by, another mod or a
        /// future vanilla item. This id is written into saves, so it must never change after release.
        /// </summary>
        public const string SignId = "recyclable_sign_neon";

        /// <summary>Sprite key: Textures/Items/sign_recyclable.png.</summary>
        public const string SpriteKey = "sign_recyclable";

        /// <summary>Store client id of the scavenger this sign attracts.</summary>
        public const string ClientId = "recyclable_sign_scav_seller";

        /// <summary>Value stored in SHOWCASE_SPECIAL_ITEM_TAG, shown as "Seller (type)" in the clientele panel.</summary>
        /// <remarks>Stored in the item's saved state, so it must never change after release.</remarks>
        public const string SignType = "RECYCLABLE_SIGN";

        /// <summary>Same daily attraction chance as the vanilla supplier signs.</summary>
        public const int AttractChance = 75;

        /// <summary>Same price as the vanilla water / food / material signs.</summary>
        public const long SignValue = 100L;

        internal static ModLog Log;

        private static readonly Func<StoreClient> SellerFactory = RecyclableSeller.Create;

        /// <summary>The mod name as the manifest gives it: "Chinese name English name" on one line.</summary>
        internal static string ManifestName;

        /// <summary>The same name with the Chinese and English halves on separate lines, for tooltips.</summary>
        internal static string TwoLineName;

        public void Init(ModManifest manifest)
        {
            Log = new ModLog(manifest);
            VisitorSettings.Define(ModHelper.GetConfig(manifest.ID));
            ManifestName = manifest.Name;
            // The Chinese half has no spaces, so the first space is the boundary between the two names.
            int split = string.IsNullOrEmpty(ManifestName) ? -1 : ManifestName.IndexOf(' ');
            TwoLineName = split > 0 ? ManifestName.Substring(0, split) + "\n" + ManifestName.Substring(split + 1) : ManifestName;
        }

        public void OnEnable()
        {
            ModHook.OnModItemDirectoryInit += RegisterItems;
            ModHook.OnPlaceInventorInventoryItemEarly += OfferSignAtInventor;
            StoreClientListDict.storeClientDict[ClientId] = SellerFactory;
        }

        public void OnDisable()
        {
            ModHook.OnModItemDirectoryInit -= RegisterItems;
            ModHook.OnPlaceInventorInventoryItemEarly -= OfferSignAtInventor;
            if (StoreClientListDict.storeClientDict.TryGetValue(ClientId, out Func<StoreClient> current) && current == SellerFactory)
            {
                StoreClientListDict.storeClientDict.Remove(ClientId);
            }
            StoreClientListDict.ClearDisplayNameCache();
        }

        private static void RegisterItems(ModItemDirectory directory)
        {
            directory.Add(SignId, CreateSign);
        }

        private static GameItem CreateSign()
        {
            GameItem item = ItemDirectory.CreateEmptyItem(SignId);
            item.SetSpriteAndShapeFromMod(ModId, SpriteKey);
            item.name = ModHelper.GetLocalized(ModId, "sign_recyclable_name");
            item.shortDescription = ModHelper.GetLocalized(ModId, "sign_recyclable_desc");
            item.unitValue = SignValue;
            item.EnableTag("IMPORTANT_TAG");
            // Marks it as a display-case sign: the file box accepts it and the
            // clientele panel lists it as "<sign>: Seller (<type>)".
            ShowcaseHelper.InitShowcaseSpecialItem(item, SignType);
            return item;
        }

        /// <summary>Dr. Jackson stocks the sign whenever the player does not own one, like the vanilla signs.</summary>
        /// <remarks>
        /// Runs on the Early hook, before the rest of his stock. When he visits the shop his goods go on the
        /// 18x4 front counter, and anything that no longer fits is destroyed without notice. Added last, the
        /// 2x2 sign was the first thing to be dropped once his stock grew, so it never appeared there.
        /// </remarks>
        private static void OfferSignAtInventor(List<GameItem> ownedItems)
        {
            // The hook also fires for Dr. Jackson's "special stock" event, a fixed list that carries
            // none of the vanilla signs. Same condition the game uses to pick that list.
            if (MapUIManager.Instance != null && MapUIManager.Instance.Current != null
                && MapUIManager.Instance.Current.Id == "dr_jackson" && AfterhourEvents.IsActive("special_stock"))
            {
                return;
            }
            if (PlayerStore.Instance == null)
            {
                return;
            }
            if (ownedItems != null)
            {
                foreach (GameItem owned in ownedItems)
                {
                    if (owned != null && owned.identifier == SignId)
                    {
                        return;
                    }
                }
            }
            PlayerStore.Instance.AddDirectSellingItemToTable(ItemSpawner.Spawn(SignId));
        }
    }
}
