namespace RecyclableSign
{
    /// <summary>Which kinds of scavenger the sign may attract.</summary>
    internal enum VisitorMode
    {
        Both,
        SellerOnly,
        BarterOnly,
    }

    /// <summary>
    /// The player's choice of visitor, shown in the mod's config panel (gear button in the Mods menu).
    /// The game draws a dropdown of the stored strings themselves, so each option is written in both
    /// languages and never changes: a translated value would stop matching once the language switches.
    /// </summary>
    internal static class VisitorSettings
    {
        // Saved in ModConfigs/recyclable_sign.json. Never change these strings after release.
        private const string Both = "两种都有 Both";
        private const string SellerOnly = "只有卖家 Seller only";
        private const string BarterOnly = "只有以物易物 Barter only";

        private static ConfigEntry<string> entry;

        public static void Define(ModConfig config)
        {
            entry = config.Define(
                "visitor_mode",
                Both,
                "拾荒者 Scavenger",
                () => ModHelper.GetLocalized(ModEntry.ModId, "config_visitor_mode"),
                choices: new[] { Both, SellerOnly, BarterOnly },
                tooltip: () => ModHelper.GetLocalized(ModEntry.ModId, "config_visitor_mode_tip"));
        }

        /// <summary>Current choice; anything unrecognised in the saved file counts as both.</summary>
        public static VisitorMode Mode
        {
            get
            {
                string value = entry != null ? entry.Value : null;
                if (value == SellerOnly) return VisitorMode.SellerOnly;
                if (value == BarterOnly) return VisitorMode.BarterOnly;
                return VisitorMode.Both;
            }
        }
    }
}
