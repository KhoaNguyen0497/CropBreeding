using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.GameData.Crops;

namespace CropBreeding;

internal static class CropCatalog
{
    // These are actual vanilla item IDs, not name heuristics. Tea/tree seeds have no Data/Crops record.
    private static readonly HashSet<string> Excluded = ["770", "MixedFlowerSeeds", "495", "496", "497", "498", "885", "890"];
    internal static string Raw(string id) => id.StartsWith("(O)") ? id[3..] : id;
    internal static Dictionary<string, CropData> Data => Game1.content.Load<Dictionary<string, CropData>>("Data/Crops");
    internal static bool EligibleSeed(string id) => !Excluded.Contains(Raw(id)) && Data.ContainsKey(Raw(id));
    internal static bool Matches(Item donor, Item seed) => EligibleSeed(seed.ItemId)
        && Raw(Data[Raw(seed.ItemId)].HarvestItemId) == Raw(donor.ItemId);
    internal static bool IsProduce(Item item) => Data.Any(p => EligibleSeed(p.Key) && Raw(p.Value.HarvestItemId) == item.ItemId);
    internal static bool Ground(HoeDirtAlias soil) => soil.Pot == null && soil.Location != null
        && soil.Location.terrainFeatures.TryGetValue(soil.Tile, out var feature) && ReferenceEquals(feature, soil);
}
