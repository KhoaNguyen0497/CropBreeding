using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace CropBreeding;

internal static class JunimoHarvestTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    internal static void Run()
    {
        JunimoHarvestOutput.Initialize();
        ModEntry.Instance.Config = new ModConfig { MutationChance = 0, QualityUpgradeChance = 1, RootedChance = 1 };
        Traits.SaltRandomFactory = _ => new ZeroRandom();
        try
        {
            // Sunflower seeds, wheat hay and same-ID Companion items can all arrive later.
            foreach (var pair in new[] { ("421", "431"), ("262", "178"), ("24", "24") })
            {
                var plant = ReadyPlant(pair.Item1);
                Traits.Write(plant.modData, ["high_quality:1", "companion:1"]);
                plant.modData[Companion.Key] = pair.Item1;
                string mutation = "companion:1,fast_growth:1,high_quality:1";
                plant.modData[MutationState.Key] = Core.TraitRules.Encode(Core.TraitRules.Parse(mutation));
                var context = new HarvestContext(plant);
                HarvestContext.Current = context;
                Item primary = HarvestContext.CloneHarvest(new Item { ItemId = pair.Item1, Quality = 2 });
                Check(primary.Quality == 4, "raisin target includes High Quality upgrade");
                if (pair.Item2 != pair.Item1) HarvestContext.CloneHarvest(new Item { ItemId = pair.Item2 });
                Check(ReferenceEquals(context.LastPrimaryOutput, primary), "vanilla seed/hay byproducts don't replace main crop");
                // Finalization clears Current before delivering extras; even an identical
                // Companion item ID must not be treated as another primary crop.
                HarvestContext.Current = null;
                var junimo = new JunimoHarvester { LastItem = new Item { ItemId = pair.Item2 }, FailItem = "334" };
                var extras = new List<Item> { new() { ItemId = "334" }, new() { ItemId = pair.Item1 }, new() { ItemId = "472" } };
                JunimoHarvestOutput.DeliverExtras(junimo, extras, context.LastPrimaryOutput);
                Check(junimo.Delivered.Count == 2, "one failed bonus does not prevent later deliveries");
                Check(ReferenceEquals(junimo.LastItem, primary), "main crop restored even when bonus delivery throws");
                int randomCalls = Traits.RandomCalls;
                // The successful branch of vanilla's unchanged raisin roll.
                Item duplicate = junimo.LastItem!.getOne();
                duplicate.Quality = junimo.LastItem.Quality;
                junimo.tryToAddItemToHut(duplicate);
                Check(duplicate.ItemId == pair.Item1 && duplicate.Stack == 1 && duplicate.Quality == primary.Quality,
                    "raisin copy is one main crop with the same quality");
                Check(duplicate.modData[Traits.Key] == primary.modData[Traits.Key]
                    && duplicate.modData[Companion.Key] == primary.modData[Companion.Key], "mutation and Companion metadata copied");
                Check(Traits.RandomCalls == randomCalls, "raisin copying does not reroll trait effects");
            }

            var annual = ReadyPlant("24");
            Traits.Write(annual.modData, ["nurse_crop:5", "rooted:5"]);
            var tree = new Tree();
            annual.currentLocation.terrainFeatures[new(1, 0)] = tree;
            var harvest = new HarvestContext(annual);
            harvest.GrowNearbyTrees();
            Check(tree.growthStage.Value == 2, "Nurse Crop runs for original ready annual harvest");
            Check(harvest.TryRestart() && annual.currentPhase.Value == 0, "Rooted restarts original plant");
            var keeper = new JunimoHarvester();
            Item crop = harvest.Decorate(new Item())[0];
            JunimoHarvestOutput.DeliverExtras(keeper, [], crop);
            keeper.tryToAddItemToHut(keeper.LastItem!.getOne());
            Check(tree.growthStage.Value == 2 && annual.currentPhase.Value == 0, "copy neither nurses twice nor harvests Rooted restart");

            var natural = ReadyPlant("24");
            natural.Data!.RegrowDays = 4;
            Traits.Write(natural.modData, ["nurse_crop:5", "rooted:5"]);
            var regrowContext = new HarvestContext(natural);
            natural.currentLocation.terrainFeatures[new(1, 0)] = new Tree();
            regrowContext.GrowNearbyTrees();
            Check(!regrowContext.TryRestart(), "natural regrowers do not acquire annual Rooted behavior");
            Check(((Tree)natural.currentLocation.terrainFeatures[new(1, 0)]).growthStage.Value == 0, "natural regrowers do not nurse");

            Item fallback = new();
            var unchanged = new JunimoHarvester { LastItem = fallback };
            JunimoHarvestOutput.DeliverExtras(unchanged, [new Item { ItemId = "334" }], null);
            Check(ReferenceEquals(unchanged.LastItem, fallback), "missing captured primary preserves vanilla target");
            Console.WriteLine("Passed Junimo raisin main-crop selection, metadata/quality copies, byproduct exclusion, delivery failure recovery and annual effect checks. Uses test doubles, not live Junimo AI.");
        }
        finally { HarvestContext.Current = null; Traits.SaltRandomFactory = null; }
    }

    private static Crop ReadyPlant(string id)
    {
        var plant = new Crop { Dirt = new HoeDirt(), Data = new CropData { HarvestItemId = id } };
        plant.Dirt.crop = plant;
        plant.currentPhase.Value = plant.phaseDays.Count - 1;
        plant.dayOfCurrentPhase.Value = 0;
        return plant;
    }
    private sealed class ZeroRandom : Random { public override double NextDouble() => 0; }
}
