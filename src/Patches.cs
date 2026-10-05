using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace CropBreeding;

internal static class Patches
{
    [ThreadStatic] private static SObject? placing;
    internal static void Apply(Harmony harmony)
    {
        Patch(harmony, typeof(SObject), nameof(SObject.placementAction), nameof(PlacementPrefix), finalizer: nameof(PlacementFinalizer));
        Patch(harmony, typeof(HoeDirtAlias), nameof(HoeDirtAlias.plant), nameof(PlantPrefix), nameof(PlantPostfix));
        Patch(harmony, typeof(HoeDirtAlias), nameof(HoeDirtAlias.GetFertilizerSpeedBoost), postfix: nameof(SpeedPostfix));
        harmony.Patch(AccessTools.Method(typeof(Crop), nameof(Crop.harvest)),
            prefix: Method(nameof(HarvestPrefix)), transpiler: Method(nameof(HarvestTranspiler)), finalizer: Method(nameof(HarvestFinalizer)));
        Patch(harmony, typeof(Item), nameof(Item.canStackWith), postfix: nameof(StackPostfix));
        Patch(harmony, typeof(SObject), nameof(SObject.getDescription), postfix: nameof(DescriptionPostfix));
        Patch(harmony, typeof(SObject), nameof(SObject.performObjectDropInAction), nameof(DropPrefix));
        Patch(harmony, typeof(SObject), nameof(SObject.checkForAction), nameof(ActionPrefix));
        Patch(harmony, typeof(SObject), nameof(SObject.minutesElapsed), nameof(MinutesPrefix));
        Patch(harmony, typeof(SObject), nameof(SObject.performToolAction), nameof(ToolPrefix));
        Patch(harmony, typeof(SObject), nameof(SObject.OutputMachine), postfix: nameof(ProcessPostfix));
        Patch(harmony, typeof(CraftingRecipe), nameof(CraftingRecipe.createItem), postfix: nameof(CraftedPostfix));
    }
    private static HarmonyMethod Method(string name) => new(typeof(Patches), name);
    private static void Patch(Harmony h, Type type, string name, string? prefix = null, string? postfix = null, string? finalizer = null)
        => h.Patch(AccessTools.Method(type, name), prefix == null ? null : Method(prefix), postfix == null ? null : Method(postfix),
            finalizer: finalizer == null ? null : Method(finalizer));

    private static void PlacementPrefix(SObject __instance, out SObject? __state)
    {
        __state = placing;
        placing = __instance;
    }
    private static Exception? PlacementFinalizer(Exception? __exception, SObject? __state)
    {
        placing = __state;
        return __exception;
    }
    private static bool PlantPrefix(HoeDirtAlias __instance, string itemId, Farmer who, bool isFertilizer, ref bool __result, out string[] __state)
    {
        Item? seed = placing?.ItemId == CropCatalog.Raw(itemId) ? placing : who?.ActiveObject;
        __state = !isFertilizer && seed?.ItemId == CropCatalog.Raw(itemId) ? Traits.Read(seed.modData) : [];
        if (!isFertilizer && __state.Length > 0 && (!CropCatalog.Ground(__instance) || !CropCatalog.EligibleSeed(itemId)))
        {
            __result = false;
            if (who?.IsLocalPlayer == true) Game1.showRedMessage("Trait seeds need tilled ground, outside a planter.");
            return false;
        }
        return true;
    }
    private static void PlantPostfix(HoeDirtAlias __instance, string itemId, Farmer who, bool isFertilizer, bool __result, string[] __state)
    {
        if (!__result || isFertilizer || __instance.crop == null) return;
        Crop crop = __instance.crop;
        bool eligible = CropCatalog.Ground(__instance) && CropCatalog.EligibleSeed(itemId);
        crop.modData[Traits.EligibilityKey] = eligible ? "true" : "false";
        Traits.Write(crop.modData, eligible ? __state : []);
        // Reapply vanilla speed calculation after transferring traits. This preserves profession/paddy/fertilizer effects.
        if (Core.TraitRules.Level(__state, "fast_growth") > 0) __instance.applySpeedIncreases(who);
    }
    private static void SpeedPostfix(HoeDirtAlias __instance, ref float __result)
    {
        if (__instance.crop is Crop crop && Traits.Eligible(crop, __instance) && Traits.Has(crop.modData, "fast_growth"))
            __result += (float)Math.Clamp(ModEntry.Instance.Config.FastGrowthReduction * Traits.Level(crop.modData, "fast_growth"), 0, 0.9);
    }
    private static void HarvestPrefix(Crop __instance, HoeDirtAlias soil, out HarvestContext? __state)
    {
        __state = HarvestContext.Current;
        HarvestContext.Current = Traits.Eligible(__instance, soil) && __instance.GetData() != null
            ? new HarvestContext(__instance) : null;
    }
    private static Exception? HarvestFinalizer(Exception? __exception, HarvestContext? __state)
    {
        HarvestContext? current = HarvestContext.Current;
        HarvestContext.Current = __state;
        if (__exception == null && current?.WasReady == true && current.Plant.Dirt is HoeDirtAlias soil)
            HarvestContext.ApplyRegrowth(current.Plant, soil);
        return __exception;
    }
    private static IEnumerable<CodeInstruction> HarvestTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo clone = AccessTools.Method(typeof(Item), nameof(Item.getOne));
        MethodInfo replacement = AccessTools.Method(typeof(HarvestContext), nameof(HarvestContext.CloneHarvest));
        int count = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(clone)) { instruction.opcode = OpCodes.Call; instruction.operand = replacement; count++; }
            yield return instruction;
        }
        if (count == 0) throw new InvalidOperationException("Crop.harvest has no supported outgoing item sites.");
    }
    private static void StackPostfix(Item __instance, ISalable other, ref bool __result)
    {
        if (__result && other is Item item)
            __result = Core.TraitRules.Same(string.Join(',', Traits.Read(__instance.modData)), string.Join(',', Traits.Read(item.modData)));
    }
    private static void DescriptionPostfix(SObject __instance, ref string __result)
    {
        string[] traits = Traits.Read(__instance.modData);
        if (traits.Length > 0) __result += "\n\nTraits: " + string.Join(", ", traits.Select(Core.TraitRules.Label));
    }
    private static bool DropPrefix(SObject __instance, Item dropInItem, bool probe, Farmer who, ref bool __result, bool returnFalseIfItemConsumed)
    {
        if (!Breeder.IsMachine(__instance)) return true;
        int required = __instance.heldObject.Value == null ? 1 : Breeder.SeedsRequired;
        __result = Breeder.Insert(__instance, dropInItem, probe);
        if (__result && !probe)
        {
            // Vanilla consumes drop-in items only when its own handler does so; our handler owns consumption.
            SObject.ConsumeInventoryItem(who, dropInItem, required);
            if (returnFalseIfItemConsumed) __result = false;
        }
        return false;
    }
    private static bool ActionPrefix(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (!Breeder.IsMachine(__instance)) return true;
        __result = __instance.heldObject.Value != null;
        if (!justCheckingForActivity && __instance.heldObject.Value is Item item && who.IsLocalPlayer)
        {
            if (who.addItemToInventoryBool(item)) Breeder.Clear(__instance);
            else Game1.showRedMessage("Inventory full.");
        }
        return false;
    }
    private static bool MinutesPrefix(SObject __instance, ref bool __result)
    {
        if (!Breeder.IsMachine(__instance)) return true;
        __result = false;
        return false;
    }
    private static void ToolPrefix(SObject __instance, Tool t)
    {
        if (Breeder.IsMachine(__instance) && (t == null || t is Axe || t is Pickaxe)) Breeder.Clear(__instance);
    }
    private static void ProcessPostfix(SObject __instance, Item? inputItem, bool probe, bool __result)
    {
        if (!__result || probe || __instance.heldObject.Value is not Item output) return;
        output.modData.Remove(Traits.Key);
        if (ModEntry.Instance.Config.EnableSeedMakerInheritance && __instance.QualifiedItemId == "(BC)25"
            && inputItem != null && CropCatalog.Matches(inputItem, output))
            Traits.Write(output.modData, Traits.Read(inputItem.modData));
    }
    private static void CraftedPostfix(Item __result) => __result.modData.Remove(Traits.Key);
}
