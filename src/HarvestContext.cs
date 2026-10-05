using CropBreeding.Core;
using StardewValley;

namespace CropBreeding;

internal sealed class HarvestContext
{
    [ThreadStatic] internal static HarvestContext? Current;
    internal readonly Crop Plant;
    internal readonly bool WasReady;
    internal readonly string HarvestId;
    internal readonly string[] Inherited;
    internal readonly string[] OutputTraits;
    internal readonly bool Bonus;
    private bool bonusApplied;

    internal HarvestContext(Crop crop)
    {
        Plant = crop;
        WasReady = Ready(crop);
        HarvestId = CropCatalog.Raw(crop.GetData()!.HarvestItemId);
        Inherited = Traits.Read(crop.modData);
        bool regrows = crop.GetData()!.RegrowDays > 0;
        double chance = regrows && !ModEntry.Instance.Config.EnableRegrowingCropMutations
            ? 0 : ModEntry.Instance.Config.MutationChance;
        OutputTraits = TraitRules.Mutate(Inherited, ModEntry.Instance.Config.MaximumTraits, chance, Traits.RandomFor(crop, 11));
        // Existing plant traits determine the current harvest effects. A new mutation starts working after replanting.
        Bonus = TraitRules.Level(Inherited, "high_yield") > 0 && Traits.RandomFor(crop, 23).NextDouble()
            < Math.Clamp(ModEntry.Instance.Config.ExtraYieldChance * TraitRules.Level(Inherited, "high_yield"), 0, 1);
    }

    internal static bool Ready(Crop crop) => !crop.dead.Value && crop.currentPhase.Value >= crop.phaseDays.Count - 1
        && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0);

    internal static void ApplyRegrowth(Crop crop, HoeDirtAlias soil)
    {
        // Call only for a crop which was ready before the harvest attempt. A failed/full-storage
        // attempt never transitions to a positive regrowth countdown and must not get a speed-up.
        if (!ReferenceEquals(soil.crop, crop) || !Traits.Eligible(crop, soil)
            || !crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0
            || crop.GetData()?.RegrowDays is not > 0) return;
        crop.dayOfCurrentPhase.Value = TraitRules.RegrowthDays(crop.dayOfCurrentPhase.Value,
            Traits.Level(crop.modData, "fast_regrowth"), ModEntry.Instance.Config.FastRegrowthReduction);
    }

    internal Item Decorate(Item item)
    {
        if (item.ItemId == HarvestId)
        {
            Traits.Write(item.modData, OutputTraits);
            if (Bonus && !bonusApplied)
            {
                item.Stack++;
                bonusApplied = true;
            }
        }
        else if (HarvestId == "421" && item.ItemId == "431")
            Traits.Write(item.modData, Inherited);
        return item;
    }

    // Called only at the outgoing clone sites inside Crop.harvest. Does not alter crop data,
    // source templates, quality, colors, or unrelated item creation elsewhere in the game.
    internal static Item CloneHarvest(Item source)
    {
        Item copy = source.getOne();
        return Current?.Decorate(copy) ?? copy;
    }
}
