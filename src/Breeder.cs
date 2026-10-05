using StardewValley;
using SObject = StardewValley.Object;

namespace CropBreeding;

internal static class Breeder
{
    internal const int SeedsRequired = 10;
    internal const string MachineId = ModEntry.Id + "_Breeder";
    internal static bool IsMachine(SObject machine) => machine.QualifiedItemId == "(BC)" + MachineId;
    internal static bool IsDonor(Item item) => item is SObject && item.Stack > 0
        && Traits.Read(item.modData).Length > 0 && CropCatalog.IsProduce(item) && item.ItemId != "433";
    internal static bool Insert(SObject machine, Item item, bool probe)
    {
        if (machine.readyForHarvest.Value || item.Stack < 1) return false;
        if (machine.heldObject.Value == null)
        {
            if (!IsDonor(item)) return false;
            if (!probe)
            {
                machine.heldObject.Value = (SObject)item.getOne();
                machine.MinutesUntilReady = -1;
                machine.readyForHarvest.Value = false;
            }
            return true;
        }
        if (item.Stack < SeedsRequired || !CanBreed(machine.heldObject.Value, item, out string[] traits)) return false;
        if (!probe)
        {
            Item output = item.getOne();
            output.Stack = 1;
            output.Quality = 0;
            Traits.Write(output.modData, traits);
            machine.heldObject.Value = (SObject)output;
            machine.MinutesUntilReady = 0;
            machine.readyForHarvest.Value = true;
        }
        return true;
    }
    internal static bool CanBreed(Item donor, Item seed, out string[] traits)
    {
        traits = [];
        return CropCatalog.Matches(donor, seed) && Core.TraitRules.TryBreed(Traits.Read(donor.modData),
            Traits.Read(seed.modData), ModEntry.Instance.Config.MaximumTraits, out traits);
    }
    internal static void Clear(SObject machine)
    {
        machine.heldObject.Value = null;
        machine.readyForHarvest.Value = false;
        machine.MinutesUntilReady = -1;
        machine.showNextIndex.Value = false;
    }
}
