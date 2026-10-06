using StardewValley;
using StardewValley.Mods;

namespace CropBreeding;

internal static class Companion
{
    internal const string Key = ModEntry.Id + "/Companion";
    private static Dictionary<string, int>? growth;
    internal static void Invalidate() => growth = null;
    private static Dictionary<string, int> Growth
    {
        get
        {
            if (growth != null) return growth;
            var loaded = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in CropCatalog.Data)
            {
                if (!CropCatalog.EligibleSeed(pair.Key)) continue;
                int days = pair.Value.DaysInPhase.Sum(d => Math.Max(0, d));
                if (days <= 0) continue;
                string id = CropCatalog.Raw(pair.Value.HarvestItemId);
                if (ItemRegistry.GetData("(O)" + id) == null) continue;
                // Multiple actual seed mappings can share a harvest. Use the longest base time
                // consistently so an alternative fast seed cannot underprice the penalty.
                loaded[id] = Math.Max(loaded.GetValueOrDefault(id), days);
            }
            growth = loaded; // Publish only a complete cache; a failed build can retry later.
            return growth;
        }
    }
    internal static string? Read(ModDataDictionary data) => Traits.Has(data, "companion")
        && data.TryGetValue(Key, out string id) && !string.IsNullOrEmpty(id) ? id : null;
    internal static void Write(ModDataDictionary data, string? id)
    {
        if (id == null || !Traits.Has(data, "companion")) data.Remove(Key);
        else data[Key] = CropCatalog.Raw(id);
    }
    internal static bool Valid(Item item) => item is StardewValley.Object && Growth.ContainsKey(item.ItemId);
    internal static int BaseDays(ModDataDictionary data) => Read(data) is string id ? Growth.GetValueOrDefault(id) : 0;
    internal static string Label(ModDataDictionary data)
    {
        string? id = Read(data);
        return id == null ? "unassigned" : ItemRegistry.GetDataOrErrorItem("(O)" + id).DisplayName;
    }
    internal static string? Merge(ModDataDictionary donor, ModDataDictionary seed)
        => Core.TraitRules.CompanionChoice(Read(donor), Read(seed));
    internal const string GrowthDeltaKey = ModEntry.Id + "/GrowthPhaseDeltas";
    internal static void RemoveGrowthDelay(HoeDirtAlias soil)
    {
        if (soil.crop is not Crop crop) return;
        if (crop.modData.TryGetValue(GrowthDeltaKey, out string encoded))
        {
            var snapshot = new CropSnapshot(crop);
            try
            {
                string[] deltas = encoded.Split(',');
                if (deltas.Length != crop.phaseDays.Count) throw new InvalidOperationException("Saved growth adjustments do not match the crop's phases.");
                int[] restored = new int[deltas.Length];
                for (int i = 0; i < deltas.Length; i++)
                {
                    if (!int.TryParse(deltas[i], out int delta)) throw new InvalidOperationException("Invalid growth phase adjustment.");
                    restored[i] = Math.Max(0, checked(crop.phaseDays[i] - delta));
                }
                for (int i = 0; i < restored.Length; i++) crop.phaseDays[i] = restored[i];
                crop.modData.Remove(GrowthDeltaKey);
            }
            catch { ErrorHandler.Try("Restore growth bookkeeping", snapshot.Restore); throw; }
        }
    }
    internal static void ApplyGrowth(HoeDirtAlias soil)
    {
        if (soil.crop is not Crop crop || !Traits.Eligible(crop, soil)) return;
        int days = BaseDays(crop.modData);
        int level = Traits.Level(crop.modData, "fast_growth");
        double penalty = Traits.GrowthPenalty(crop.modData);
        if ((days > 0 || level > 0 || penalty > 0) && crop.phaseDays.Count >= 2)
        {
            var snapshot = new CropSnapshot(crop);
            try
            {
                int[] original = crop.phaseDays.ToArray();
                int[] adjusted = Core.TraitRules.FinalGrowthPhases(original, level, ModEntry.Instance.Config.GrowthReductionPerLevel, days, penalty);
                crop.modData[GrowthDeltaKey] = string.Join(",", adjusted.Select((value, i) => value - original[i]));
                for (int i = 0; i < adjusted.Length; i++) crop.phaseDays[i] = adjusted[i];
            }
            catch { ErrorHandler.Try("Restore vanilla growth", snapshot.Restore); throw; }
        }
    }
}
