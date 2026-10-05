using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using Pathoschild.Stardew.Automate;
using SObject = StardewValley.Object;

namespace CropBreeding.Integrations;

public interface IAutomateApi { void AddFactory(IAutomationFactory factory); }

public sealed class BreedingFactory : IAutomationFactory
{
    public IAutomatable? GetFor(SObject obj, GameLocation location, in Vector2 tile) => Breeder.IsMachine(obj) ? new BreedingMachine(obj, location, tile) : null;
    public IAutomatable? GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile) => null;
    public IAutomatable? GetFor(Building building, GameLocation location, in Vector2 tile) => null;
    public IAutomatable? GetForTile(GameLocation location, in Vector2 tile) => null;
}

public sealed class BreedingMachine(SObject machine, GameLocation location, Vector2 tile) : IMachine
{
    public GameLocation Location { get; } = location;
    public Rectangle TileArea { get; } = new((int)tile.X, (int)tile.Y, 1, 1);
    // Automate caches failures by type; different pending donors require different seed matches.
    public string MachineTypeID => ModEntry.Id + ":" + machine.heldObject.Value?.QualifiedItemId
        + ":" + (machine.heldObject.Value is Item donor ? Core.TraitRules.Encode(Traits.Read(donor.modData)) + ":" + Companion.Read(donor.modData) : "");
    public MachineState GetState() => Breeder.CompanionMode(machine) ? MachineState.Processing : Breeder.MenuMutex(machine, Location).IsLocked() ? MachineState.Processing : machine.readyForHarvest.Value ? MachineState.Done : MachineState.Empty;
    public ITrackedStack? GetOutput() => !Breeder.CompanionMode(machine) && !Breeder.MenuMutex(machine, Location).IsLocked() && machine.readyForHarvest.Value && machine.heldObject.Value is Item item ? new BreedingOutput(machine, item) : null;
    public bool SetInput(IStorage storage)
    {
        if (Breeder.CompanionMode(machine) || Breeder.MenuMutex(machine, Location).IsLocked() || storage.HasLockedContainers() || machine.readyForHarvest.Value) return false;
        if (machine.heldObject.Value is Item donor)
        {
            if (!TryFindSeeds(storage, donor, out IConsumable? seeds)) return false;
            Item batch = seeds!.Sample.getOne();
            batch.Stack = Breeder.SeedsRequired;
            if (!Breeder.Insert(machine, batch, true)) return false;
            if (!Breeder.Insert(machine, batch, false)) return false;
            seeds.Reduce();
            return true;
        }
        var stacks = storage.GetItems().Where(s => s.Count > 0).ToArray();
        foreach (ITrackedStack candidate in stacks)
        {
            if (!Breeder.IsDonor(candidate.Sample)) continue;
            if (!TryFindSeeds(storage, candidate.Sample, out _)) continue;
            // Stage only one donor; the next pass completes with its matching seed. Both are normal
            // tracked inventories and no direct chest assumptions are needed.
            if (!Breeder.Insert(machine, candidate.Sample, false)) return false;
            candidate.Reduce(1);
            return true;
        }
        return false;
    }
    private static bool TryFindSeeds(IStorage storage, Item donor, out IConsumable? seeds)
    {
        // Aggregate split stacks, but never mix different seed traits or qualities in one batch.
        foreach (ITrackedStack stack in storage.GetItems())
        {
            if (stack.Count <= 0 || !Breeder.CanBreed(donor, stack.Sample, out _)) continue;
            Item sample = stack.Sample;
            if (storage.TryGetIngredient(s => s.Count > 0 && s.Sample.QualifiedItemId == sample.QualifiedItemId
                && s.Sample.canStackWith(sample), Breeder.SeedsRequired, out seeds)) return true;
        }
        seeds = null;
        return false;
    }
}

public sealed class BreedingOutput(SObject machine, Item item) : ITrackedStack
{
    public Item Sample { get; } = item.getOne();
    public string Type => Sample.TypeDefinitionId;
    public int Count => ReferenceEquals(machine.heldObject.Value, item) ? item.Stack : 0;
    public void Reduce(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (!ReferenceEquals(machine.heldObject.Value, item)) return;
        item.Stack = Math.Max(0, item.Stack - count);
        if (item.Stack == 0) Breeder.Clear(machine);
    }
    public Item? Take(int count)
    {
        int amount = Math.Min(Math.Max(0, count), Count);
        if (amount == 0) return null;
        Item output = Sample.getOne(); output.Stack = amount; Reduce(amount); return output;
    }
}
