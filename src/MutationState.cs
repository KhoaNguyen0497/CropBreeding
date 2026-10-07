using CropBreeding.Core;
using StardewValley;

namespace CropBreeding;

// One saved, network-synchronised outcome per harvest cycle. Never stored on items or in Traits.Key.
internal static class MutationState
{
    internal const string Key = ModEntry.Id + "/PendingMutation";
    private const string NoMutation = "-";

    internal static bool Ready(Crop crop) => !crop.dead.Value && crop.phaseDays.Count > 0
        && crop.currentPhase.Value >= crop.phaseDays.Count - 1
        && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0);

    internal static void AfterGrowth(Crop crop)
    {
        // The host owns overnight/fairy growth. Farmhands read the synced result, not a second roll.
        if (Game1.IsMasterGame) EnsurePrepared(crop);
    }

    // Also available to future effects that advance crop growth. Harvest calls this as a fallback
    // for mods which bypass newDay/growCompletely, including a locally performed farmhand harvest.
    internal static void EnsurePrepared(Crop crop)
    {
        try
        {
            if (crop.modData.ContainsKey(Key) || !Ready(crop)
                || crop.Dirt is not HoeDirtAlias soil || !ReferenceEquals(soil.crop, crop)
                || !Traits.Eligible(crop, soil) || crop.GetData() is not { } data) return;
            string[] inherited = Traits.Read(crop.modData);
            var config = ModEntry.Instance.Config;
            bool regrows = data.RegrowDays > 0;
            double chance = regrows && !config.EnableRegrowingCropMutations ? 0
                : TraitRules.MutationRate(config.MutationChance, TraitRules.Level(inherited, "researcher"), CropBreeding.Core.TraitRules.ResearcherMutationBonus);
            string[] result = TraitRules.Mutate(inherited, config.MaximumTraits, chance, Traits.RandomFor(crop, 11),
                canRegrow: regrows, isAvailable: TraitAvailable);
            // Publish only after the complete roll succeeds. Failure/blocked picks need an explicit
            // marker so tomorrow's growth hook cannot retry them. No separate isRolled flag needed.
            crop.modData[Key] = result.SequenceEqual(inherited) ? NoMutation : TraitRules.Encode(result);
        }
        catch (Exception ex) { ErrorHandler.Report("Prepare crop mutation", ex); }
    }

    internal static string[] ForHarvest(Crop crop, string[] inherited)
    {
        EnsurePrepared(crop);
        try
        {
            if (!crop.modData.TryGetValue(Key, out string? value) || value == NoMutation) return inherited;
            string[] result = TraitRules.Parse(value);
            if (result.Length == 0 || TraitRules.Encode(result) != value)
                throw new InvalidOperationException("Invalid saved mutation outcome.");
            return result;
        }
        catch (Exception ex)
        {
            ErrorHandler.Report("Read crop mutation", ex);
            return inherited;
        }
    }

    // A failed/full-inventory harvest retains the exact outcome. Successful regrowth and Rooted
    // both start their next cycle without it; removed annual crops need no special handling.
    internal static void CompleteHarvest(Crop crop, bool succeeded)
    {
        if (succeeded) crop.modData.Remove(Key);
    }

    // Drawing does not roll, parse traits, allocate particles, or touch the world/trait catalogue.
    internal static bool HasMutation(Crop crop) => crop.modData.TryGetValue(Key, out string? value)
        && !string.IsNullOrEmpty(value) && value != NoMutation && Ready(crop);

    private static bool TraitAvailable(string id) => !TraitRules.MaterialDrops.TryGetValue(id, out var material)
        || Game1.objectData.ContainsKey(material.ItemId);
}
