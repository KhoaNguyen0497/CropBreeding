using StardewValley;
using SObject = StardewValley.Object;

namespace CropBreeding;

// On-demand menu operations. The station keeps one input/output slot; surplus left
// input is returned after processing, instead of being overwritten by the output.
internal static class StationStacks
{
    private static int LeftCost(SObject machine) => Breeder.RemoveMode(machine) || Breeder.CompanionMode(machine) ? 1 : Breeder.CropsRequired;
    private static int RightCost(SObject machine) => Breeder.CompanionMode(machine) ? 1 : Breeder.SeedsRequired;
    private static SObject Batch(SObject machine)
    {
        var batch = (SObject)machine.heldObject.Value!.getOne();
        batch.Stack = LeftCost(machine);
        return batch;
    }
    private static bool Pair(SObject machine, Item left, Item right)
    {
        if (Breeder.CompanionMode(machine)) return Breeder.CanAssign(left, right);
        var donor = left.getOne(); donor.Stack = Breeder.CropsRequired;
        var seed = right.getOne(); seed.Stack = Breeder.SeedsRequired;
        return Breeder.CanBreed(donor, seed, out _);
    }
    internal static bool CanProcess(SObject machine, Item? right)
    {
        if (machine.readyForHarvest.Value || machine.heldObject.Value is not Item left || left.Stack < LeftCost(machine)) return false;
        return Breeder.RemoveMode(machine) ? Breeder.CanRemoveFrom(left)
            : right != null && right.Stack >= RightCost(machine) && Pair(machine, left, right);
    }
    internal static bool Insert(SObject machine, ref Item? right, Item source)
    {
        if (machine.readyForHarvest.Value || source is not SObject || source.Stack <= 0) return false;
        bool toLeft;
        if (Breeder.RemoveMode(machine))
        {
            if (!Breeder.CanRemoveFrom(source)) return false;
            toLeft = true;
        }
        else if (Breeder.CompanionMode(machine))
        {
            toLeft = CropCatalog.EligibleSeed(source.ItemId) && Traits.Has(source.modData, "companion");
            if (!toLeft && !Companion.Valid(source)) return false;
        }
        else if (CropCatalog.EligibleSeed(source.ItemId))
        {
            toLeft = false;
        }
        else
        {
            if (!Breeder.IsDonor(source)) return false;
            toLeft = true;
        }
        if (!Breeder.RemoveMode(machine))
        {
            Item? other = toLeft ? right : machine.heldObject.Value;
            if (other != null && !(toLeft ? Pair(machine, source, other) : Pair(machine, other, source))) return false;
        }
        Item? target = toLeft ? machine.heldObject.Value : right;
        if (ReferenceEquals(source, target) || (target != null && !target.canStackWith(source))) return false;
        int count = Math.Min(source.Stack, (target ?? source).maximumStackSize() - (target?.Stack ?? 0));
        if (count <= 0) return false;
        if (target == null)
        {
            target = source.getOne();
            target.Stack = count;
            if (toLeft) { machine.heldObject.Value = (SObject)target; machine.MinutesUntilReady = -1; }
            else right = target;
        }
        else target.Stack += count;
        source.Stack -= count;
        return true;
    }
    internal static bool Process(SObject machine, Item? right, string? removeTrait, out Item? surplus)
    {
        surplus = null;
        if (!CanProcess(machine, right)) return false;
        var original = machine.heldObject.Value!;
        int count = original.Stack - LeftCost(machine);
        Item? remainder = count > 0 ? original.getOne() : null;
        if (remainder != null) remainder.Stack = count;
        var batch = Batch(machine);
        bool completed = false;
        try
        {
            machine.heldObject.Value = batch;
            completed = Breeder.RemoveMode(machine)
                ? removeTrait != null && Breeder.RemoveTrait(machine, removeTrait)
                : Breeder.Insert(machine, right!, false);
            if (completed) surplus = remainder;
            return completed;
        }
        finally { if (!completed) machine.heldObject.Value = original; }
    }
}
