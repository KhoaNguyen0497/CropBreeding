using StardewValley;
using SObject = StardewValley.Object;

namespace CropBreeding;

internal static partial class Patches
{
    internal static void CheckStationInput()
    {
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        foreach (int count in new[] { 1, 3, 999 })
        foreach (bool staged in new[] { false, true })
        {
            var machine = new SObject { ItemId = Breeder.MachineId };
            var donor = new SObject { Stack = count, Quality = 2 };
            Traits.Write(donor.modData, ["high_yield:3"]);
            SObject? contents = staged ? new SObject { Stack = 3 } : null;
            machine.heldObject.Value = contents;
            bool probe = true, accepted = true;
            Check(!DropPrefix(machine, ref probe) && !probe, "station input probe rejects held item");
            Check(!DropPrefix(machine, ref accepted), "station skips vanilla item deposit");
            // GameLocation.checkAction removes one active item only on acceptance;
            // otherwise it falls through to the station's separate checkForAction hook.
            bool reachesMenuAction = !accepted;
            if (accepted) donor.Stack--;
            Check(reachesMenuAction && donor.Stack == count, "opening station preserves held stack, including last item");
            Check(donor.Quality == 2 && Traits.Level(donor.modData, "high_yield") == 3,
                "opening station preserves quality and traits");
            Check(ReferenceEquals(machine.heldObject.Value, contents), "opening station preserves staged ingredients");
        }
        foreach (bool initial in new[] { false, true })
        {
            bool result = initial;
            Check(DropPrefix(new SObject { ItemId = "vanilla_machine" }, ref result) && result == initial,
                "other machines retain vanilla item acceptance");
        }
        Console.WriteLine("Passed station input rejection, vanilla menu-action fallthrough, held-item preservation and other-machine passthrough. Uses test doubles.");
    }
}
