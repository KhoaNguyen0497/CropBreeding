using StardewValley;
using SObject = StardewValley.Object;

namespace CropBreeding;

internal static class StationStacksTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static SObject Item(string id, int count, params string[] traits)
    {
        var item = new SObject { ItemId = id, Stack = count, Quality = 2 };
        Traits.Write(item.modData, traits);
        return item;
    }
    internal static void Run()
    {
        ModEntry.Instance.Config = new ModConfig { BreedingCost = 3 };
        foreach (bool seedFirst in new[] { false, true })
        {
            var machine = new SObject(); Item? right = null;
            var donor = Item("24", 50, "high_yield:2");
            var seed = Item("472", 40, "high_yield:1");
            Check(StationStacks.Insert(machine, ref right, seedFirst ? seed : donor), "first quick-insert accepted");
            Check(StationStacks.Insert(machine, ref right, seedFirst ? donor : seed), "matching second quick-insert accepted");
            Check(donor.Stack == 0 && seed.Stack == 0 && machine.heldObject.Value!.Stack == 50 && right!.Stack == 40,
                "whole stacks routed without auto-crafting");
            Check(!machine.readyForHarvest.Value && StationStacks.CanProcess(machine, right), "full-stack inputs become ready for explicit action");
            Check(StationStacks.Process(machine, right, null, out var surplus), "full-stack breeding succeeds");
            Check(surplus?.Stack == 47 && surplus.Quality == 2 && Traits.Level(surplus.modData, "high_yield") == 2,
                "unused donor quantity, quality and original traits preserved");
            Check(machine.heldObject.Value!.Stack == 1 && Traits.Level(machine.heldObject.Value.modData, "high_yield") == 3,
                "merging still produces one upgraded seed");
            Check(right!.Stack == 40, "UI remains sole owner of right-input consumption");
            var extra = Item("24", 5, "high_yield:2");
            Check(!StationStacks.Insert(machine, ref right, extra) && extra.Stack == 5, "ready output cannot be overwritten");
        }
        {
            var machine = new SObject(); Item? right = null;
            Check(!StationStacks.Insert(machine, ref right, Item("24", 10)), "plain crop rejected");
            Check(!StationStacks.Insert(machine, ref right, Item("472", 10, "high_yield:2")), "ineligible merge seed rejected");
            Check(!StationStacks.Insert(machine, ref right, Item("472", 10, "high_yield:1", "rooted:1")), "multi-trait seed rejected");
            Check(StationStacks.Insert(machine, ref right, Item("24", 998, "high_yield:2")), "large stack staged");
            var excess = Item("24", 5, "high_yield:2");
            Check(StationStacks.Insert(machine, ref right, excess) && excess.Stack == 4 && machine.heldObject.Value!.Stack == 999,
                "slot capacity leaves remainder in inventory");
            var different = Item("24", 5, "high_yield:3");
            Check(!StationStacks.Insert(machine, ref right, different) && different.Stack == 5, "different traits never merged into occupied slot");
            var mismatch = Item("473", 5);
            Check(!StationStacks.Insert(machine, ref right, mismatch) && mismatch.Stack == 5 && right == null, "mismatched seeds untouched");
        }
        foreach (bool remove in new[] { false, true })
        {
            var machine = new SObject(); Item? right = null;
            machine.modData[remove ? Breeder.RemoveModeKey : Breeder.ModeKey] = "true";
            var seeds = Item("472", 12, "companion:2");
            Check(StationStacks.Insert(machine, ref right, seeds) && seeds.Stack == 0, "special modes accept whole seed stack in left slot");
            if (!remove)
            {
                var own = Item("24", 5);
                Check(!StationStacks.Insert(machine, ref right, own) && own.Stack == 5, "self-companion rejected");
                Check(StationStacks.Insert(machine, ref right, Item("192", 20)), "companion crop routes to right slot");
            }
            Check(StationStacks.Process(machine, right, remove ? "companion" : null, out var surplus), "special action succeeds");
            Check(surplus?.Stack == 11 && Traits.Level(surplus.modData, "companion") == 2,
                "special action only modifies one seed and preserves remaining traits");
            Check(machine.heldObject.Value!.Stack == 1, "special action yields exactly one seed");
        }
        {
            var machine = new SObject(); Item? right = null;
            StationStacks.Insert(machine, ref right, Item("24", 2, "high_yield:2"));
            StationStacks.Insert(machine, ref right, Item("472", 20));
            var original = machine.heldObject.Value;
            Check(!StationStacks.Process(machine, right, null, out var rejected) && rejected == null
                && ReferenceEquals(original, machine.heldObject.Value) && original!.Stack == 2, "insufficient stack remains untouched");
            StationStacks.Insert(machine, ref right, Item("24", 8, "high_yield:2"));
            ModEntry.Instance.Config.BreedingCost = 5;
            Check(StationStacks.Process(machine, right, null, out var remaining) && remaining!.Stack == 5,
                "changed cost consumes only current requirement and preserves surplus");
            ModEntry.Instance.Config.BreedingCost = 3;
        }
        Console.WriteLine("Passed quick-insert routing in all modes, full-stack quantities, explicit processing, surplus preservation and invalid/occupied input rejection. Uses test doubles.");
    }
}
