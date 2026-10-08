using StardewValley;
using SObject = StardewValley.Object;

namespace CropBreeding;

internal static class BreedingCostTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    internal static void Run()
    {
        foreach (int cost in new[] { 1, 2, 3, 5, 10 })
        {
            ModEntry.Instance.Config = new ModConfig { BreedingCost = cost };
            var donor = new SObject { ItemId = "24", Stack = cost };
            Traits.Write(donor.modData, ["high_yield:2"]);
            var seed = new SObject { ItemId = "472", Stack = 1 };
            var machine = new SObject();
            donor.Stack = cost - 1;
            Check(!Breeder.Insert(machine, donor, false), "insufficient donor rejected");
            donor.Stack = cost;
            Check(Breeder.Insert(machine, donor, true) && machine.heldObject.Value == null, "probe does not stage ingredients");
            Check(Breeder.Insert(machine, donor, false) && machine.heldObject.Value!.Stack == cost, "stages the configured crop amount");
            seed.Stack = 0;
            Check(!Breeder.Insert(machine, seed, false) && !machine.readyForHarvest.Value, "insufficient seeds cannot consume staged crops");
            seed.Stack = 1;
            Check(Breeder.Insert(machine, seed, false), "one seed accepted at every crop cost");
            Check(machine.readyForHarvest.Value && machine.heldObject.Value!.ItemId == "472"
                && machine.heldObject.Value.Stack == 1 && Traits.Level(machine.heldObject.Value.modData, "high_yield") == 2,
                "one output preserves donor traits for every cost");
            Check(donor.Stack == cost && seed.Stack == 1, "station never also decrements caller-owned input stacks; UI owns removal");
        }

        ModEntry.Instance.Config = new ModConfig { BreedingCost = 5 };
        var staged = new SObject();
        var crops = new SObject { ItemId = "24", Stack = 5 };
        Traits.Write(crops.modData, ["high_yield:2"]);
        Breeder.Insert(staged, crops, false);
        var seeds = new SObject { ItemId = "472", Stack = 10 };
        foreach (int changedCost in new[] { 1, 6 })
        {
            ModEntry.Instance.Config.BreedingCost = changedCost;
            Check(!Breeder.Insert(staged, seeds, false) && staged.heldObject.Value!.Stack == 5
                && !staged.readyForHarvest.Value, "cost changes cannot silently discard or underpay staged ingredients");
        }
        ModEntry.Instance.Config.BreedingCost = 5;
        Traits.Write(seeds.modData, ["high_yield:1"]);
        Check(Breeder.Insert(staged, seeds, false) && Traits.Level(staged.heldObject.Value!.modData, "high_yield") == 3,
            "merging follows the same configured cost");

        var removal = new SObject();
        removal.modData[Breeder.RemoveModeKey] = "true";
        var single = new SObject { ItemId = "472" };
        Traits.Write(single.modData, ["high_yield:2"]);
        Check(Breeder.Insert(removal, single, false) && Breeder.RemoveTrait(removal, "high_yield"),
            "trait removal still accepts one seed regardless of breeding cost");
        var companion = new SObject();
        companion.modData[Breeder.ModeKey] = "true";
        Traits.Write(single.modData, ["companion:1"]);
        Check(Breeder.Insert(companion, single, false), "Companion stages one seed regardless of breeding cost");
        var ownCrop = new SObject { ItemId = "24", Stack = 5 };
        var originalSeed = companion.heldObject.Value;
        Check(!Breeder.CanAssign(single, ownCrop) && !Breeder.Insert(companion, ownCrop, true)
            && !Breeder.Insert(companion, ownCrop, false), "parsnip cannot be its own Companion in preview or commit");
        Check(ReferenceEquals(companion.heldObject.Value, originalSeed) && !companion.readyForHarvest.Value
            && Companion.Read(originalSeed!.modData) == null && ownCrop.Stack == 5,
            "rejection preserves staged seed, assignment and crop stack");
        Check(Breeder.Insert(companion, new SObject { ItemId = "192" }, false)
            && Companion.Read(companion.heldObject.Value!.modData) == "192",
            "parsnip can select potato; assignment still costs one seed and crop");
        Console.WriteLine("Passed configurable breeding costs, insufficient stacks, probe behavior, staged-cost changes, merging and unchanged secondary modes. Uses test doubles; UI input remains a live check.");
    }
}
