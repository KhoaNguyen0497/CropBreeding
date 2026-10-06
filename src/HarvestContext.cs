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
    private readonly int yieldLevel;
    private readonly List<(Item Source, int Count)> primaryOutputs = [];
    private int primaryCount;
    private readonly string? companionId;
    private readonly Random qualityRandom;
    private readonly int qualityLevel;
    internal readonly List<Item> PendingExtras = [];
    private static readonly (string Trait, string ItemId, int Salt)[] Materials =
    [
        ("copper_bearing", "(O)334", 83),
        ("iron_bearing", "(O)335", 89),
        ("gold_bearing", "(O)336", 97)
    ];

    internal HarvestContext(Crop crop)
    {
        Plant = crop;
        WasReady = Ready(crop);
        HarvestId = CropCatalog.Raw(crop.GetData()!.HarvestItemId);
        Inherited = Traits.Read(crop.modData);
        int? baseGrowthDays = null;
        foreach (var material in Materials)
        {
            int level = TraitRules.Level(Inherited, material.Trait);
            if (level == 0) continue;
            // Loaded crop data includes SVE/modded base growth, but no planted-phase speed,
            // Companion delay, Researcher penalty or regrowth countdown.
            baseGrowthDays ??= crop.GetData()!.DaysInPhase.Sum(days => Math.Max(0, days));
            int count = TraitRules.MaterialDropCount(baseGrowthDays.Value, level, Traits.RandomFor(crop, material.Salt).NextDouble());
            if (count > 0) PendingExtras.Add(ItemRegistry.Create(material.ItemId, count, 0));
        }
        companionId = Companion.Read(crop.modData);
        if (Traits.RandomFor(crop, 71).NextDouble() < Math.Clamp(TraitRules.Level(Inherited, "seed_saver") * ModEntry.Instance.Config.SeedSaverChance, 0, 1))
        {
            Item seed = ItemRegistry.Create("(O)" + CropCatalog.Raw(crop.netSeedIndex.Value), 1, 0);
            Traits.Write(seed.modData, Inherited);
            Companion.Write(seed.modData, companionId);
            PendingExtras.Add(seed);
        }
        if (companionId != null && Companion.BaseDays(crop.modData) > 0
            && Traits.RandomFor(crop, 53).NextDouble() < Math.Clamp(TraitRules.Level(Inherited, "companion") * ModEntry.Instance.Config.CompanionChance, 0, 1))
            PendingExtras.Add(ItemRegistry.Create("(O)" + companionId, 1, 0));
        qualityLevel = TraitRules.Level(Inherited, "high_quality");
        qualityRandom = Traits.RandomFor(crop, 37);
        bool regrows = crop.GetData()!.RegrowDays > 0;
        double chance = regrows && !ModEntry.Instance.Config.EnableRegrowingCropMutations
            ? 0 : TraitRules.MutationRate(ModEntry.Instance.Config.MutationChance,
                TraitRules.Level(Inherited, "researcher"), ModEntry.Instance.Config.ResearcherMutationBonus);
        OutputTraits = TraitRules.Mutate(Inherited, ModEntry.Instance.Config.MaximumTraits, chance, Traits.RandomFor(crop, 11), canRegrow: regrows);
        // Existing plant traits determine the current harvest effects. A new mutation starts working after replanting.
        yieldLevel = TraitRules.Level(Inherited, "high_yield");
    }

    internal static bool Ready(Crop crop) => !crop.dead.Value && crop.currentPhase.Value >= crop.phaseDays.Count - 1
        && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0);

    internal bool TryRestart()
    {
        var data = Plant.GetData();
        if (!WasReady || Plant.dead.Value || data == null || Plant.Dirt is not HoeDirtAlias soil
            || !ReferenceEquals(soil.crop, Plant) || !Traits.Eligible(Plant, soil)
            || !TraitRules.RootedTriggers(TraitRules.Level(Inherited, "rooted"), ModEntry.Instance.Config.RootedChance,
                data.RegrowDays > 0, Traits.RandomFor(Plant, 107).NextDouble())) return false;

        // Reuse the plant so its inherited traits, color and other mods' metadata survive.
        // Remove our saved phase deltas before rebuilding vanilla growth, avoiding accumulation.
        Companion.RemoveGrowthDelay(soil);
        Plant.ResetPhaseDays();
        Plant.currentPhase.Value = 0;
        Plant.dayOfCurrentPhase.Value = 0;
        Plant.fullyGrown.Value = false;
        Plant.phaseToShow.Value = -1;
        // Sunflower harvest temporarily changes this to its bonus seed item.
        Plant.indexOfHarvest.Value = CropCatalog.Raw(data.HarvestItemId);
        Plant.raisedSeeds.Value = data.IsRaised;
        soil.nearWaterForPaddy.Value = -1;
        soil.applySpeedIncreases(Game1.player);
        if (soil.hasPaddyCrop() && soil.paddyWaterCheck())
        {
            soil.state.Value = 1;
            soil.updateNeighbors();
        }
        Plant.updateDrawMath(soil.Tile);
        return true;
    }

    internal static void ApplyRegrowth(Crop crop, HoeDirtAlias soil)
    {
        // Call only for a crop which was ready before the harvest attempt. A failed/full-storage
        // attempt never transitions to a positive regrowth countdown and must not get a speed-up.
        if (!ReferenceEquals(soil.crop, crop) || !Traits.Eligible(crop, soil)
            || !crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0
            || crop.GetData()?.RegrowDays is not > 0) return;
        crop.dayOfCurrentPhase.Value = TraitRules.RegrowthDays(crop.dayOfCurrentPhase.Value,
            Traits.Level(crop.modData, "fast_growth"), ModEntry.Instance.Config.GrowthReductionPerLevel, Companion.BaseDays(crop.modData), Traits.GrowthPenalty(crop.modData));
    }

    internal List<Item> Decorate(Item item)
    {
        if (item.ItemId == HarvestId)
        {
            Traits.Write(item.modData, OutputTraits);
            Companion.Write(item.modData, companionId);
        }
        else if (HarvestId == "421" && item.ItemId == "431")
        {
            Traits.Write(item.modData, Inherited);
            Companion.Write(item.modData, companionId);
        }
        if (item.ItemId != HarvestId || qualityLevel <= 0 || item.Quality == 4) return [item];
        // Roll once per unit, including High Yield's extra units. Preserve the source quality and
        // color until after the normal harvest calculation, then split stacks by resulting quality.
        int baseQuality = item.Quality;
        int upgraded = 0;
        int upgradedQuality = baseQuality;
        for (int i = 0; i < item.Stack; i++)
        {
            int quality = TraitRules.HarvestQuality(baseQuality, qualityLevel,
                ModEntry.Instance.Config.QualityUpgradeChance, qualityRandom.NextDouble());
            if (quality != baseQuality) { upgraded++; upgradedQuality = quality; }
        }
        if (upgraded == 0) return [item];
        if (upgraded == item.Stack) { item.Quality = upgradedQuality; return [item]; }
        Item better = item.getOne();
        better.Quality = upgradedQuality;
        better.Stack = upgraded;
        item.Stack -= upgraded;
        return [item, better];
    }

    internal void CompleteYield()
    {
        if (primaryCount == 0 || yieldLevel == 0) return;
        Random random = Traits.RandomFor(Plant, 23);
        int extra = TraitRules.ExtraYieldCount(primaryCount, yieldLevel,
            ModEntry.Instance.Config.ExtraYieldChance, random.NextDouble());
        for (int i = 0; i < extra; i++)
        {
            // Sample original output quality/color before High Quality, weighted by item count.
            int index = random.Next(primaryCount);
            foreach (var entry in primaryOutputs)
            {
                if (index < entry.Count)
                {
                    PendingExtras.AddRange(Decorate(entry.Source.getOne()));
                    break;
                }
                index -= entry.Count;
            }
        }
    }

    // Called only at the outgoing clone sites inside Crop.harvest. Does not alter crop data,
    // source templates, quality, colors, or unrelated item creation elsewhere in the game.
    internal static Item CloneHarvest(Item source)
    {
        Item copy = source.getOne();
        if (Current == null) return copy;
        if (Current.yieldLevel > 0 && copy.ItemId == Current.HarvestId)
        {
            Current.primaryCount += copy.Stack;
            // Vanilla reuses source templates. Keep counts rather than an item clone per unit.
            var entries = Current.primaryOutputs;
            if (entries.Count > 0 && ReferenceEquals(entries[^1].Source, source))
                entries[^1] = (source, entries[^1].Count + copy.Stack);
            else entries.Add((source, copy.Stack));
        }
        List<Item> outputs = Current.Decorate(copy);
        // The vanilla clone site accepts one item stack. Only commit split-off extras if the
        // enclosing harvest succeeds (e.g. not when the player's inventory rejects the crop).
        if (outputs[0].Stack > 1)
        {
            Item extra = outputs[0].getOne();
            extra.Stack = outputs[0].Stack - 1;
            outputs[0].Stack = 1;
            Current.PendingExtras.Add(extra);
        }
        Current.PendingExtras.AddRange(outputs.Skip(1));
        return outputs[0];
    }
}
