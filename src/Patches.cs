using System.Collections.Generic;
using HarmonyLib;

namespace RecyclableSign
{
    /// <summary>
    /// The vanilla signs are hard-coded in ShowcaseHelper.GetDisplayCasePrediction, which feeds both
    /// the daily customer roll (StoreClientManager.HandleNormalClient) and the clientele panel.
    /// There is no hook for it, so the new sign is appended to its result.
    /// </summary>
    [HarmonyPatch(typeof(ShowcaseHelper), nameof(ShowcaseHelper.GetDisplayCasePrediction))]
    internal static class DisplayCasePredictionPatch
    {
        private static void Postfix(List<(string, int, string)> __result)
        {
            if (__result != null && ShowcaseHelper.IsShowcaseContainItemById(ModEntry.SignId))
            {
                // Front of the list: the day's queue is filled in list order with no shuffle, so this
                // makes the scavenger the first display-case customer, and the first to claim one of the
                // day's normal-customer slots. The game looks entries up by client id, never by index.
                __result.Insert(0, (ModEntry.ClientId, ModEntry.AttractChance, ModEntry.SignId));
            }
        }
    }

    /// <summary>
    /// The clientele panel prints a sign as "Seller (type name)" and looks the type name up in the
    /// game's own string table, which has no entry for the mod's type.
    /// </summary>
    [HarmonyPatch(typeof(TypeHelper), nameof(TypeHelper.GetTypeDisplayName))]
    internal static class TypeDisplayNamePatch
    {
        private static bool Prefix(string typeConst, ref string __result)
        {
            if (typeConst != ModEntry.SignType)
            {
                return true;
            }
            __result = ModHelper.GetLocalized(ModEntry.ModId, "type_recyclable");
            return false;
        }
    }

    /// <summary>
    /// The item tooltip prints the mod name on one wrapped line, which breaks the bilingual name in the
    /// middle of the English half. Only this mod's own name is touched; the manifest name stays on one
    /// line for the mod menu and logs.
    /// </summary>
    [HarmonyPatch(typeof(ModSourceHelper), nameof(ModSourceHelper.GetItemModName))]
    internal static class ItemModNamePatch
    {
        private static void Postfix(ref string __result)
        {
            if (__result != null && __result == ModEntry.ManifestName)
            {
                __result = ModEntry.TwoLineName;
            }
        }
    }

    /// <summary>Same fix for the "from mod" line the recorder prints when this mod's customer arrives.</summary>
    [HarmonyPatch(typeof(ModSourceHelper), nameof(ModSourceHelper.GetClientModName))]
    internal static class ClientModNamePatch
    {
        private static void Postfix(ref string __result)
        {
            if (__result != null && __result == ModEntry.ManifestName)
            {
                __result = ModEntry.TwoLineName;
            }
        }
    }
}
