using CropBreeding.Core;
using StardewValley;

namespace CropBreeding;

internal static class SeedInheritance
{
    // Shared by normal planting and integrations which know the actual consumed seed stack.
    internal static void Apply(HoeDirtAlias soil, string seedId, string[] traits, string? companionId, Farmer who)
    {
        if (soil.crop is not Crop crop) return;
        CropSnapshot? snapshot = null;
        try
        {
            snapshot = new CropSnapshot(crop);
            bool eligible = CropCatalog.Ground(soil) && CropCatalog.EligibleSeed(seedId);
            crop.modData[Traits.EligibilityKey] = eligible ? "true" : "false";
            Traits.Write(crop.modData, eligible ? traits : []);
            Companion.Write(crop.modData, eligible ? companionId : null);
            // Another planting patch may have instantly grown it before traits transferred.
            crop.modData.Remove(MutationState.Key);
            if (TraitRules.Level(traits, "fast_growth") > 0 || Companion.BaseDays(crop.modData) > 0
                || Traits.GrowthPenalty(crop.modData) > 0) soil.applySpeedIncreases(who);
            MutationState.EnsurePrepared(crop);
        }
        catch (Exception ex)
        {
            if (snapshot != null) ErrorHandler.Try("Restore planted crop", snapshot.Restore);
            ErrorHandler.Report("Apply seed traits", ex);
        }
    }
}
