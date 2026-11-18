using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

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

namespace RecyclableSign
{
    /// <summary>
    /// Items that use a mod sprite carry the mod id as their "atlas path". The game's own item renderer
    /// knows that and looks the sprite up in the mod, but code that loads an item icon straight from the
    /// atlas (RenderHandler.TryLoadFromAtlas / LoadFromAtlas) gets nothing back, because no Unity atlas has
    /// that name. Better Cashier draws its item cards that way, so the sign showed up blank there.
    /// This answers lookups for this mod's own atlas path with the mod's sprite; every other atlas goes
    /// through untouched.
    /// </summary>
    /// <remarks>
    /// Patches the private LoadFromAtlasRaw, which both public loaders call, rather than the two one-line
    /// wrappers that the runtime may inline. It runs for every sprite the game loads, so the guard is a
    /// single string comparison and the sprite is built once and cached.
    /// </remarks>
    [HarmonyPatch(typeof(RenderHandler), "LoadFromAtlasRaw")]
    internal static class ModAtlasSpritePatch
    {
        private static Sprite cached;

        /// <summary>Set after a failed build so a missing or broken texture is not re-read on every lookup.</summary>
        private static bool failed;

        private static bool Prefix(string atlasPath, string name, ref Sprite __result)
        {
            if (atlasPath != ModEntry.ModId || name != ModEntry.SpriteKey)
            {
                return true;
            }
            if (cached == null)
            {
                if (failed)
                {
                    return true;
                }
                cached = LoadSignSprite();
                if (cached == null)
                {
                    failed = true;
                    ModEntry.Log?.Warning("Could not build the sign sprite for direct atlas lookups.");
                    return true;
                }
            }
            __result = cached;
            return false;
        }

        /// <summary>
        /// Builds the sprite from the mod's own texture, the same way the game's mod loader does (point
        /// filtering, clamped, 100 pixels per unit, centred pivot). Reads the file through ModHelper, which
        /// needs no declared capability; the loader's own sprite table is off limits to mods.
        /// </summary>
        private static Sprite LoadSignSprite()
        {
            byte[] png = ModHelper.ReadModFileBytes(ModEntry.ModId, "Textures/Items/" + ModEntry.SpriteKey + ".png");
            if (png == null)
            {
                return null;
            }
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(png))
            {
                Object.Destroy(texture);
                return null;
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
