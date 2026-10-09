using CropBreeding.Core;
using StardewValley;

namespace CropBreeding;

internal static class RetiredTraitTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    internal static void Run()
    {
        foreach (string id in new[] { "472", "24" })
        {
            var item = new StardewValley.Object { ItemId = id, Stack = 137, Quality = 4 };
            item.modData[Traits.Key] = "rooted:5";
            Check(RetiredTraits.CleanItem(item) && !item.modData.ContainsKey(Traits.Key), "Rooted-only seed/produce loses trait key and badge");
            Check(item.canStackWith(new StardewValley.Object { ItemId = id, Quality = 4 }), "retired-only item stacks with matching plain item");
            Check(item.Stack == 137 && item.Quality == 4 && item.ItemId == id, "item identity, stack and quality preserved");
            Check(!RetiredTraits.CleanItem(item), "repeated load cleanup is harmless");

            item.modData[Traits.Key] = "companion:3,rooted:4,seed_saver:5,unknown_future:2";
            item.modData[Companion.Key] = "190";
            item.modData["AnotherMod/Data"] = "unchanged";
            Check(RetiredTraits.CleanItem(item) && item.modData[Traits.Key] == "companion:3,seed_saver:5,unknown_future:2", "only retired token removed, without normalizing other traits");
            Check(item.modData[Companion.Key] == "190" && item.modData["AnotherMod/Data"] == "unchanged"
                && item.Stack == 137 && item.Quality == 4, "Companion, other mod data, quantity and quality survive");
            Check(!RetiredTraits.CleanItem(item), "mixed-item cleanup is idempotent");
            item.modData[Traits.Key] = "rooted,rooted:1,rooted:5";
            Check(RetiredTraits.CleanItem(item) && !item.modData.ContainsKey(Traits.Key), "duplicate and unlevelled retired tokens removed");
        }
        var machine = new StardewValley.Object();
        var output = new StardewValley.Object { ItemId = "472", Stack = 8 };
        machine.heldObject.Value = output;
        machine.readyForHarvest.Value = true;
        output.modData[Traits.Key] = "rooted:3,high_yield:4";
        RetiredTraits.CleanItem(output);
        Check(ReferenceEquals(machine.heldObject.Value, output) && machine.readyForHarvest.Value
            && output.Stack == 8 && output.modData[Traits.Key] == "high_yield:4", "cleanup preserves held output and machine state");
        Check(TraitDescriptions.Describe("rooted:5", new ModConfig()) == "", "retired trait has no active lookup description");
        Console.WriteLine("Passed retired Rooted seed/produce cleanup, stacking, trait/Companion/quantity preservation, held output and repeated-load checks. Uses test doubles.");
    }
}
