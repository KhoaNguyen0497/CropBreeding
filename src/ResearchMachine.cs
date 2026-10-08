using CropBreeding.Core;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;

namespace CropBreeding;

public static class ResearchMachine
{
    internal const string MachineId = ModEntry.Id + "_Researcher";
    internal const string InputQuery = ModEntry.Id + "_RESEARCH_SEED";
    // Type-check the string-addressed callback against the actual game delegate at build time.
    private static readonly MachineOutputDelegate OutputCallback = CreateOutput;
    internal static bool IsMachine(SObject machine) => machine.QualifiedItemId == "(BC)" + MachineId;
    private static bool Available(string id) => !TraitRules.MaterialDrops.TryGetValue(id, out var material)
        || Game1.objectData.ContainsKey(material.ItemId);

    internal static bool CanAccept(Item? item)
    {
        try
        {
            return item is SObject { IsRecipe: false } && item.Stack > 0
                && CropCatalog.EligibleSeed(item.ItemId)
                && ResearchRules.CanResearch(Traits.Read(item.modData), ModEntry.Instance.Config.MaximumTraits,
                    CropCatalog.Data[CropCatalog.Raw(item.ItemId)].RegrowDays > 0, Available);
        }
        catch (Exception ex) { ErrorHandler.Report("Validate research seed", ex); return false; }
    }

    internal static MachineData CreateData() => new()
    {
        HasInput = true, HasOutput = true, AllowFairyDust = false,
        WobbleWhileWorking = true, ShowNextIndexWhileWorking = true, ShowNextIndexWhenReady = false,
        InvalidItemMessage = "Insert a Researcher seed with room for two trait increases.",
        OutputRules =
        [
            new()
            {
                Id = "ResearchSeed", DaysUntilReady = -1, MinutesUntilReady = 120, RecalculateOnCollect = false,
                Triggers = [new() { Trigger = MachineOutputTrigger.ItemPlacedInMachine, RequiredCount = 1, Condition = InputQuery }],
                OutputItem = [new() { Id = "ResearchedSeed", OutputMethod = $"CropBreeding.ResearchMachine, CropBreeding: {OutputCallback.Method.Name}", MinStack = 1, MaxStack = 1, CopyQuality = true }]
            }
        ]
    };

    // Called by the native machine system, including Automate's data-based loader.
    // Vanilla stores this result on the machine before consuming its one input.
    public static Item? CreateOutput(SObject machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
    {
        overrideMinutesUntilReady = null;
        try
        {
            if (!CanAccept(inputItem)) return null;
            if (probe) return inputItem.getOne(); // Never roll or alter state during an input probe.
            if (!ResearchRules.TryResearch(Traits.Read(inputItem.modData), ModEntry.Instance.Config.MaximumTraits,
                CropCatalog.Data[CropCatalog.Raw(inputItem.ItemId)].RegrowDays > 0, Game1.random, out var traits, Available)) return null;
            Item output = inputItem.getOne();
            output.Stack = 1;
            Traits.Write(output.modData, traits);
            // Existing Companion assignment survives; a newly rolled Companion is unassigned.
            Companion.Write(output.modData, Companion.Read(inputItem.modData));
            return output;
        }
        catch (Exception ex) { ErrorHandler.Report("Research seed output", ex); return null; }
    }
}
