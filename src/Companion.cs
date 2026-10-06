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
            growth = new(StringComparer.Ordinal);
            foreach (var pair in CropCatalog.Data)
            {
                if (!CropCatalog.EligibleSeed(pair.Key)) continue;
                int days = pair.Value.DaysInPhase.Sum(d => Math.Max(0, d));
                if (days <= 0) continue;
                string id = CropCatalog.Raw(pair.Value.HarvestItemId);
                if (ItemRegistry.GetData("(O)" + id) == null) continue;
                // Multiple actual seed mappings can share a harvest. Use the longest base time
                // consistently so an alternative fast seed cannot underprice the penalty.
                growth[id] = Math.Max(growth.GetValueOrDefault(id), days);
            }
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
    private const string AppliedDelayKey = ModEntry.Id + "/AppliedCompanionDelay";
    private const string GrowthDeltaKey = ModEntry.Id + "/GrowthPhaseDeltas";
    internal static void RemoveGrowthDelay(HoeDirtAlias soil)
    {
        if (soil.crop is not Crop crop) return;
        if (crop.modData.TryGetValue(GrowthDeltaKey, out string encoded))
        {
            string[] deltas = encoded.Split(',');
            if (deltas.Length == crop.phaseDays.Count)
                for (int i = 0; i < deltas.Length; i++)
                    if (int.TryParse(deltas[i], out int delta)) crop.phaseDays[i] = Math.Max(0, crop.phaseDays[i] - delta);
            crop.modData.Remove(GrowthDeltaKey);
        }
        if (crop.modData.TryGetValue(AppliedDelayKey, out string value) && int.TryParse(value, out int delay)
            && delay > 0 && crop.phaseDays.Count >= 2)
            crop.phaseDays[crop.phaseDays.Count - 2] = Math.Max(0, crop.phaseDays[crop.phaseDays.Count - 2] - delay);
        crop.modData.Remove(AppliedDelayKey);
    }
    internal static void ApplyGrowth(HoeDirtAlias soil)
    {
        if (soil.crop is not Crop crop || !Traits.Eligible(crop, soil)) return;
        int days = BaseDays(crop.modData);
        int level = Traits.Level(crop.modData, "fast_growth");
        if ((days > 0 || level > 0) && crop.phaseDays.Count >= 2)
        {
            int[] original = crop.phaseDays.ToArray();
            int[] adjusted = Core.TraitRules.FinalGrowthPhases(original, level, ModEntry.Instance.Config.GrowthReductionPerLevel, days);
            crop.modData[GrowthDeltaKey] = string.Join(",", adjusted.Select((value, i) => value - original[i]));
            for (int i = 0; i < adjusted.Length; i++) crop.phaseDays[i] = adjusted[i];
        }
    }
}
