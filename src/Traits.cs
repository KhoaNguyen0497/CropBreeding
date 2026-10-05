using CropBreeding.Core;
using StardewValley;
using StardewValley.Mods;

namespace CropBreeding;

internal static class Traits
{
    internal const string Key = ModEntry.Id + "/Traits";
    internal const string EligibilityKey = ModEntry.Id + "/Eligible";
    internal static string[] Read(ModDataDictionary data) => TraitRules.Parse(data.TryGetValue(Key, out string value) ? value : null);
    internal static void Write(ModDataDictionary data, IEnumerable<string> values)
    {
        string value = TraitRules.Encode(values);
        if (value.Length == 0) data.Remove(Key); else data[Key] = value;
    }
    internal static int Level(ModDataDictionary data, string value) => TraitRules.Level(Read(data), value);
    internal static bool Has(ModDataDictionary data, string value) => Level(data, value) > 0;
    internal static bool Eligible(Crop crop, HoeDirtAlias soil) => CropCatalog.Ground(soil) && !crop.forageCrop.Value
        && CropCatalog.EligibleSeed(crop.netSeedIndex.Value)
        && (!crop.modData.TryGetValue(EligibilityKey, out string allowed) || allowed != "false");
    internal static Random RandomFor(Crop crop, double salt) => Utility.CreateRandom(
        crop.Dirt?.Tile.X ?? 0, (crop.Dirt?.Tile.Y ?? 0) * 1009,
        Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed,
        salt + StableHash(crop.currentLocation?.NameOrUniqueName ?? ""));
    private static int StableHash(string value)
    {
        unchecked { int hash = 17; foreach (char c in value) hash = hash * 31 + c; return hash; }
    }
}
