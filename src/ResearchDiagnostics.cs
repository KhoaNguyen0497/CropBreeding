using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.GameData.Machines;
using StardewValley.Internal;

namespace CropBreeding;

// Explicit console command only. Never loads inputs, creates outputs or rolls traits.
internal static class ResearchDiagnostics
{
    internal static void Run()
    {
        if (!Context.IsWorldReady) { Log("Load a save first."); return; }
        var location = Game1.currentLocation;
        Item? seed = Game1.player.CurrentItem;
        var automate = ModEntry.Instance.Helper.ModRegistry.Get("Pathoschild.Automate");
        Log($"Crop Breeding {ModEntry.Instance.ModManifest.Version}; Automate {automate?.Manifest.Version.ToString() ?? "not installed"}; trait cap {ModEntry.Instance.Config.MaximumTraits}.");
        Log(seed == null ? "No selected item. Select the Researcher seed in your toolbar."
            : $"Selected {seed.QualifiedItemId}, stack {seed.Stack}, traits [{string.Join(", ", Traits.Read(seed.modData))}]; eligible seed={CropCatalog.EligibleSeed(seed.ItemId)}; research accepts={ResearchMachine.CanAccept(seed)}.");
        if (!location.objects.TryGetValue(Game1.player.GetGrabTile(), out var machine) || !ResearchMachine.IsMachine(machine))
        {
            Log("Face the Research Machine and run this command again.");
            return;
        }
        var data = machine.GetMachineData();
        Log($"Machine at {location.NameOrUniqueName} {machine.TileLocation}: held={machine.heldObject.Value?.QualifiedItemId ?? "none"}, ready={machine.readyForHarvest.Value}, minutes={machine.MinutesUntilReady}; machine_input tag={machine.HasContextTag("machine_input")}; machine data={(data == null ? "missing" : "present")}.");
        if (data?.OutputRules == null) return;
        foreach (var rule in data.OutputRules)
        {
            bool matches = seed != null && MachineDataUtility.CanApplyOutput(machine, rule,
                MachineOutputTrigger.ItemPlacedInMachine, seed, Game1.player, location, out _, out _);
            Log($"Rule {rule.Id}: native input match={matches}; timer={rule.MinutesUntilReady} minutes / {rule.DaysUntilReady} days.");
            if (rule.OutputItem == null) continue;
            foreach (var output in rule.OutputItem)
            {
                bool resolved = StaticDelegateBuilder.TryCreateDelegate<MachineOutputDelegate>(output.OutputMethod, out _, out string error);
                Log($"Output {output.Id}: callback resolves={resolved}{(resolved ? "" : "; " + error)}.");
            }
        }
        Log("Check Automate's U overlay: machine and input chest must be in the same active group; chest input must be enabled. No items or results were changed.");
    }

    private static void Log(string message) => ModEntry.Instance.Monitor.Log("Research diagnostic: " + message, LogLevel.Info);
}
