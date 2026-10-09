using CropBreeding.Core;
using StardewValley;

namespace CropBreeding;

internal static class MutationLifecycleTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Crop Plant(bool ready = true, bool regrows = false, string inherited = "")
    {
        var crop = new Crop { Dirt = new HoeDirt(), Data = new CropData { RegrowDays = regrows ? 3 : -1 } };
        crop.Dirt.crop = crop;
        crop.currentPhase.Value = ready ? crop.phaseDays.Count - 1 : 0;
        crop.dayOfCurrentPhase.Value = 0;
        crop.fullyGrown.Value = regrows;
        if (inherited.Length > 0) crop.modData[Traits.Key] = inherited;
        return crop;
    }
    internal static void Run()
    {
        Game1.IsMasterGame = true;
        foreach (var material in TraitRules.MaterialDrops.Values) Game1.objectData[material.ItemId] = new();
        ModEntry.Instance.Config = new ModConfig { MutationChance = 1 };
        Traits.RandomCalls = 0;
        Traits.RandomFactory = () => new FixedRandom(0, 0); // Fast Growth, first eligible trait.

        var plant = Plant(ready: false);
        MutationState.AfterGrowth(plant);
        Check(Traits.RandomCalls == 0 && !plant.modData.ContainsKey(MutationState.Key), "unready daily update never rolls");
        plant.currentPhase.Value = plant.phaseDays.Count - 1; // vanilla growth/fairy makes it ready first.
        MutationState.AfterGrowth(plant);
        string outcome = plant.modData[MutationState.Key];
        Check(outcome == "fast_growth:1" && MutationState.HasMutation(plant), "ready transition stores success and shows marker");
        Check(!plant.modData.ContainsKey(Traits.Key), "pending mutation does not modify parent traits");
        for (int day = 0; day < 10; day++) MutationState.AfterGrowth(plant);
        ModEntry.Instance.Config.MutationChance = 0;
        Check(MutationState.ForHarvest(plant, []).SequenceEqual(new[] { "fast_growth:1" }) && Traits.RandomCalls == 1,
            "waiting, repeated growth and config edits cannot change stored success");
        MutationState.CompleteHarvest(plant, false);
        Check(plant.modData[MutationState.Key] == outcome, "failed/full-storage harvest retains outcome");

        var reloaded = Plant();
        foreach (var pair in plant.modData) reloaded.modData[pair.Key] = pair.Value;
        MutationState.AfterGrowth(reloaded);
        Check(MutationState.ForHarvest(reloaded, []).SequenceEqual(new[] { "fast_growth:1" }) && Traits.RandomCalls == 1,
            "serialized metadata alone restores success without rerolling");

        var failed = Plant();
        MutationState.AfterGrowth(failed); // chance now zero.
        int afterFailure = Traits.RandomCalls;
        Check(failed.modData.ContainsKey(MutationState.Key) && !MutationState.HasMutation(failed), "failed roll explicitly saved without icon");
        ModEntry.Instance.Config.MutationChance = 1;
        MutationState.AfterGrowth(failed);
        Check(MutationState.ForHarvest(failed, []).Length == 0 && Traits.RandomCalls == afterFailure, "failed outcome cannot reroll tomorrow");
        var failedReload = Plant();
        foreach (var pair in failed.modData) failedReload.modData[pair.Key] = pair.Value;
        MutationState.AfterGrowth(failedReload);
        Check(!MutationState.HasMutation(failedReload) && Traits.RandomCalls == afterFailure, "failed outcome survives reload too");

        foreach (string inherited in new[] { "fast_growth:5", "evergreen:4" })
        {
            ModEntry.Instance.Config.MaximumTraits = 1;
            var blocked = Plant(inherited: inherited);
            MutationState.AfterGrowth(blocked);
            Check(!MutationState.HasMutation(blocked) && MutationState.ForHarvest(blocked, Traits.Read(blocked.modData)).SequenceEqual(new[] { inherited }),
                "maxed pick and new trait at cap both store no-mutation outcome");
        }
        var upgrade = Plant(inherited: "fast_growth:4");
        MutationState.AfterGrowth(upgrade);
        Check(MutationState.ForHarvest(upgrade, Traits.Read(upgrade.modData)).SequenceEqual(new[] { "fast_growth:5" }), "selected existing trait upgrades");
        Check(upgrade.modData[Traits.Key] == "fast_growth:4", "upgrade never changes parent's bonuses");

        ModEntry.Instance.Config.MaximumTraits = 3;
        var regrow = Plant(regrows: true);
        MutationState.AfterGrowth(regrow);
        MutationState.CompleteHarvest(regrow, true);
        Check(!regrow.modData.ContainsKey(MutationState.Key), "successful regrowing harvest clears cycle outcome");
        regrow.dayOfCurrentPhase.Value = 3;
        int beforeRegrowth = Traits.RandomCalls;
        MutationState.AfterGrowth(regrow);
        Check(Traits.RandomCalls == beforeRegrowth && !MutationState.HasMutation(regrow), "regrowth countdown cannot roll or show icon");
        regrow.dayOfCurrentPhase.Value = 0;
        Traits.RandomFactory = () => new FixedRandom(0, 1); // High Yield on the next ready cycle.
        MutationState.AfterGrowth(regrow);
        Check(regrow.modData[MutationState.Key] == "high_yield:1" && Traits.RandomCalls == beforeRegrowth + 1, "next regrowing harvest rolls anew");

        ModEntry.Instance.Config.MutationChance = .05;
        Traits.RandomFactory = () => new FixedRandom(.06, 1);
        var researcher = Plant(regrows: true, inherited: "researcher:1");
        MutationState.AfterGrowth(researcher);
        Check(!MutationState.HasMutation(researcher), "Researcher no longer boosts harvest mutation chance");
        Traits.RandomFactory = () => new FixedRandom(0, 1);
        ModEntry.Instance.Config.EnableRegrowingCropMutations = false;
        var disabled = Plant(regrows: true, inherited: "researcher:5");
        MutationState.AfterGrowth(disabled);
        Check(!MutationState.HasMutation(disabled), "disabled regrowing mutations beat Researcher");
        ModEntry.Instance.Config.EnableRegrowingCropMutations = true;
        MutationState.AfterGrowth(disabled);
        Check(!MutationState.HasMutation(disabled), "disabled outcome remains locked for this cycle");

        var excluded = Plant(); excluded.Eligible = false;
        var dead = Plant(); dead.dead.Value = true;
        var missing = Plant(); missing.Data = null;
        var orphan = Plant(); orphan.Dirt!.crop = null;
        var emptyPhases = Plant(); emptyPhases.phaseDays.Clear();
        int beforeExcluded = Traits.RandomCalls;
        foreach (var invalid in new[] { excluded, dead, missing, orphan, emptyPhases }) MutationState.AfterGrowth(invalid);
        Check(Traits.RandomCalls == beforeExcluded, "excluded, dead, missing, detached and empty-phase crops never roll");

        Game1.IsMasterGame = false;
        var farmhand = Plant();
        MutationState.AfterGrowth(farmhand);
        Check(!farmhand.modData.ContainsKey(MutationState.Key), "farmhand growth does not race host rolls");
        Check(MutationState.ForHarvest(farmhand, []).SequenceEqual(new[] { "high_yield:1" }), "local harvest fallback handles directly advanced crops");
        int afterFallback = Traits.RandomCalls;
        MutationState.ForHarvest(farmhand, []);
        Check(Traits.RandomCalls == afterFallback, "fallback never rerolls a stored outcome");
        Game1.IsMasterGame = true;

        Traits.RandomFactory = () => throw new Exception("Injected mutation failure");
        var error = Plant(inherited: "fast_growth:2");
        Check(MutationState.ForHarvest(error, Traits.Read(error.modData)).SequenceEqual(new[] { "fast_growth:2" }), "mutation error leaves ordinary inherited output intact");
        Check(!error.modData.ContainsKey(MutationState.Key), "failed calculation does not publish partial result");
        Traits.RandomFactory = () => new FixedRandom(0, 0);
        MutationState.AfterGrowth(error);
        Check(error.modData[MutationState.Key] == "fast_growth:3", "later action still works after mutation error");
        error.modData[MutationState.Key] = "broken data";
        Check(MutationState.ForHarvest(error, Traits.Read(error.modData)).SequenceEqual(new[] { "fast_growth:2" }), "corrupt pending outcome falls back to inherited traits");
        Console.WriteLine("Passed stored mutation success/failure, readiness, waiting/reload, blocked picks, harvest fallback, regrowth cycle state, host ownership and error recovery. Uses test doubles.");
    }

    private sealed class FixedRandom(double roll, int index) : Random
    {
        public override double NextDouble() => roll;
        public override int Next(int maxValue) => index < maxValue ? index : throw new Exception("Invalid test selection.");
    }
}
