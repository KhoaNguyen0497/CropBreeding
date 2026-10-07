using CropBreeding.Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

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
    private readonly Random? qualityRandom;
    private readonly int qualityLevel;
    internal readonly List<Item> PendingExtras = [];
    // Keep the actual outgoing main-crop item, including mutation, quality and color.
    // Byproducts and later bonus delivery must not become the Junimo's raisin target.
    internal Item? LastPrimaryOutput { get; private set; }

    internal HarvestContext(Crop crop)
    {
        Plant = crop;
        WasReady = Ready(crop);
        HarvestId = CropCatalog.Raw(crop.GetData()!.HarvestItemId);
        Inherited = Traits.Read(crop.modData);
        OutputTraits = WasReady ? MutationState.ForHarvest(crop, Inherited) : Inherited;
        // A broken bonus must not discard inherited traits or the stored mutation.
        try { companionId = Companion.Read(crop.modData); }
        catch (Exception ex) { ErrorHandler.Report("Read harvest Companion", ex); }
        try { yieldLevel = TraitRules.Level(Inherited, "high_yield"); }
        catch (Exception ex) { ErrorHandler.Report("Prepare High Yield", ex); }
        try
        {
            qualityLevel = TraitRules.Level(Inherited, "high_quality");
            qualityRandom = Traits.RandomFor(crop, 37);
        }
        catch (Exception ex) { qualityLevel = 0; ErrorHandler.Report("Prepare High Quality", ex); }
        int? baseGrowthDays = null;
        // Only inspect traits this plant actually has, not every possible material trait.
        foreach (string token in Inherited)
        {
            try
            {
                string id = TraitRules.Id(token);
                if (!TraitRules.MaterialDrops.TryGetValue(id, out var material)
                    || !Game1.objectData.ContainsKey(material.ItemId)) continue;
                int level = TraitRules.Level(Inherited, id);
                // Base crop data only: no planted speed, Companion, or regrowth countdown.
                baseGrowthDays ??= crop.GetData()!.DaysInPhase.Sum(days => Math.Max(0, days));
                int count = TraitRules.MaterialDropCount(baseGrowthDays.Value, level, Traits.RandomFor(crop, material.Salt).NextDouble());
                if (count > 0) PendingExtras.Add(ItemRegistry.Create("(O)" + material.ItemId, count, 0));
            }
            catch (Exception ex) { ErrorHandler.Report("Prepare material " + TraitRules.Id(token), ex); }
        }
        try
        {
            if (Traits.RandomFor(crop, 71).NextDouble() < Math.Clamp(TraitRules.Level(Inherited, "seed_saver") * CropBreeding.Core.TraitRules.SeedSaverChance, 0, 1))
            {
                Item seed = ItemRegistry.Create("(O)" + CropCatalog.Raw(crop.netSeedIndex.Value), 1, 0);
                Traits.Write(seed.modData, Inherited);
                Companion.Write(seed.modData, companionId);
                PendingExtras.Add(seed);
            }
        }
        catch (Exception ex) { ErrorHandler.Report("Prepare Seed Saver", ex); }
        try
        {
            if (companionId != null && Companion.BaseDays(crop.modData) > 0)
            {
                var data = crop.GetData()!;
                // Use natural regrowth even for the first harvest. Never use adjusted phases/countdowns.
                if (data.RegrowDays <= 0)
                    baseGrowthDays ??= data.DaysInPhase.Sum(days => Math.Max(0, days));
                double chance = TraitRules.CompanionOutputChance(TraitRules.Level(Inherited, "companion"),
                    baseGrowthDays.GetValueOrDefault(), data.RegrowDays);
                if (Traits.RandomFor(crop, 53).NextDouble() < chance)
                    PendingExtras.Add(ItemRegistry.Create("(O)" + companionId, 1, 0));
            }
        }
        catch (Exception ex) { ErrorHandler.Report("Prepare Companion output", ex); }
    }

    internal static bool Ready(Crop crop) => MutationState.Ready(crop);

    // Called once after a successful harvest, before Rooted can restart the crop.
    // One plant-wide roll; at most eight tile lookups, with no world scan or daily update.
    internal void GrowNearbyTrees()
    {
        int level = TraitRules.Level(Inherited, "nurse_crop");
        if (level == 0 || !WasReady || Plant.dead.Value || Plant.GetData() is not { } data
            || data.RegrowDays > 0 || Plant.Dirt is not HoeDirtAlias soil
            || Plant.currentLocation is not { } location) return;
        int stages = TraitRules.NurseCropStages(level, false, Traits.RandomFor(Plant, 113).NextDouble());
        if (stages == 0) return;
        Vector2 origin = soil.Tile;
        var changed = new List<(Tree Tree, int Stage)>();
        try
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if ((x == 0 && y == 0)
                        || !location.terrainFeatures.TryGetValue(origin + new Vector2(x, y), out var feature)
                        || feature is not Tree tree || tree.stump.Value || tree.health.Value <= 0) continue;
                    // FruitTree is a separate terrain type. Direct stage changes avoid dayUpdate's
                    // unrelated seed spreading, moss, seasonal transformations and extra growth.
                    int next = TraitRules.AdvanceImmatureTree(tree.growthStage.Value, stages, Tree.treeStage);
                    if (next != tree.growthStage.Value)
                    {
                        changed.Add((tree, tree.growthStage.Value));
                        tree.growthStage.Value = next;
                    }
                }
        }
        catch
        {
            foreach (var entry in changed)
                ErrorHandler.Try("Restore tree stage", () => entry.Tree.growthStage.Value = entry.Stage);
            throw;
        }
    }

    internal bool TryRestart()
    {
        var data = Plant.GetData();
        if (!WasReady || Plant.dead.Value || data == null || Plant.Dirt is not HoeDirtAlias soil
            || !ReferenceEquals(soil.crop, Plant) || !Traits.Eligible(Plant, soil)
            || !TraitRules.RootedTriggers(TraitRules.Level(Inherited, "rooted"), CropBreeding.Core.TraitRules.RootedChance,
                data.RegrowDays > 0, Traits.RandomFor(Plant, 107).NextDouble())) return false;

        var snapshot = new CropSnapshot(Plant);
        try
        {
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
        catch
        {
            ErrorHandler.Try("Restore crop after Rooted failure", snapshot.Restore);
            throw;
        }
    }

    internal static void ApplyRegrowth(Crop crop, HoeDirtAlias soil)
    {
        // Call only for a crop which was ready before the harvest attempt. A failed/full-storage
        // attempt never transitions to a positive regrowth countdown and must not get a speed-up.
        if (!ReferenceEquals(soil.crop, crop) || !Traits.Eligible(crop, soil)
            || !crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0
            || crop.GetData()?.RegrowDays is not > 0) return;
        crop.dayOfCurrentPhase.Value = TraitRules.RegrowthDays(crop.dayOfCurrentPhase.Value,
            Traits.Level(crop.modData, "fast_growth"), CropBreeding.Core.TraitRules.GrowthReductionPerLevel, Companion.BaseDays(crop.modData), Traits.GrowthPenalty(crop.modData));
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
        if (item.ItemId != HarvestId || qualityLevel <= 0 || qualityRandom == null || item.Quality == 4) return [item];
        // Roll once per unit, including High Yield's extra units. Preserve the source quality and
        // color until after the normal harvest calculation, then split stacks by resulting quality.
        int baseQuality = item.Quality;
        int upgraded = 0;
        int upgradedQuality = baseQuality;
        for (int i = 0; i < item.Stack; i++)
        {
            int quality = TraitRules.HarvestQuality(baseQuality, qualityLevel,
                CropBreeding.Core.TraitRules.QualityUpgradeChance, qualityRandom.NextDouble());
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
            CropBreeding.Core.TraitRules.ExtraYieldPerLevel, random.NextDouble());
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
        HarvestContext context = Current;
        int stack = copy.Stack, quality = copy.Quality, extras = context.PendingExtras.Count;
        int originalCount = context.primaryCount;
        int sources = context.primaryOutputs.Count;
        var lastSource = sources > 0 ? context.primaryOutputs[^1] : default;
        string? originalTraits = null, originalCompanion = null;
        bool captured = false;
        try
        {
            copy.modData.TryGetValue(Traits.Key, out originalTraits);
            copy.modData.TryGetValue(Companion.Key, out originalCompanion);
            captured = true;
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
            if (copy.ItemId == context.HarvestId) context.LastPrimaryOutput = outputs[0];
            return outputs[0];
        }
        catch (Exception ex)
        {
            // Return the already-created vanilla clone. Never call getOne/harvest twice.
            ErrorHandler.Try("Restore harvest output", () =>
            {
                copy.Stack = stack; copy.Quality = quality;
                if (captured)
                {
                    if (originalTraits == null) copy.modData.Remove(Traits.Key); else copy.modData[Traits.Key] = originalTraits;
                    if (originalCompanion == null) copy.modData.Remove(Companion.Key); else copy.modData[Companion.Key] = originalCompanion;
                }
                context.PendingExtras.RemoveRange(extras, context.PendingExtras.Count - extras);
                context.primaryCount = originalCount;
                if (context.primaryOutputs.Count > sources) context.primaryOutputs.RemoveRange(sources, context.primaryOutputs.Count - sources);
                if (sources > 0) context.primaryOutputs[^1] = lastSource;
            });
            ErrorHandler.Report("Decorate harvest", ex);
            return copy;
        }
    }
}
