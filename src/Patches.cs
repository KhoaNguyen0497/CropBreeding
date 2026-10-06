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
        foreach (string name in new[] { nameof(HoeDirtAlias.plant), nameof(HoeDirtAlias.canPlantThisSeedHere) })
            harmony.Patch(AccessTools.Method(typeof(HoeDirtAlias), name), transpiler: Method(nameof(PlantSeasonTranspiler)));
        harmony.Patch(AccessTools.Method(typeof(Crop), nameof(Crop.IsInSeason), new[] { typeof(GameLocation) }),
            postfix: Method(nameof(CropSeasonPostfix)));
        Patch(harmony, typeof(HoeDirtAlias), nameof(HoeDirtAlias.applySpeedIncreases), prefix: nameof(GrowthPrefix), postfix: nameof(GrowthPostfix));
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
    private sealed record PlantState(string[] Values, string? CompanionId);
    private static void CropSeasonPostfix(Crop __instance, ref bool __result)
    {
        if (!__result && !__instance.dead.Value && __instance.modData.ContainsKey(Traits.Key)
            && Core.TraitRules.EvergreenActive(Traits.Level(__instance.modData, "evergreen"))
            && __instance.Dirt is HoeDirtAlias soil && Traits.Eligible(__instance, soil))
            __result = true;
    }
    private static bool PlantIgnoresSeasons(GameLocation location, HoeDirtAlias soil, string itemId, Farmer? who)
    {
        if (location.SeedsIgnoreSeasonsHere()) return true;
        Item? seed = placing?.ItemId == CropCatalog.Raw(itemId) ? placing : (who ?? Game1.player)?.ActiveObject;
        return seed?.ItemId == CropCatalog.Raw(itemId) && seed.modData.ContainsKey(Traits.Key)
            && Core.TraitRules.EvergreenActive(Traits.Level(seed.modData, "evergreen"))
            && CropCatalog.Ground(soil) && CropCatalog.EligibleSeed(itemId);
    }
    // Replace only the local seasonal bypass check, not the whole planting method or global crop data.
    // Vanilla still checks occupied tiles, trellis collision, planting rules and location restrictions.
    private static IEnumerable<CodeInstruction> PlantSeasonTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        MethodInfo original = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.SeedsIgnoreSeasonsHere));
        MethodInfo replacement = AccessTools.Method(typeof(Patches), nameof(PlantIgnoresSeasons));
        int count = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                // Location is already on the stack. Move branch/exception labels to the first added load.
                var soil = new CodeInstruction(OpCodes.Ldarg_0);
                soil.labels.AddRange(instruction.labels);
                soil.blocks.AddRange(instruction.blocks);
                instruction.labels.Clear();
                instruction.blocks.Clear();
                yield return soil;
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(__originalMethod.Name == nameof(HoeDirtAlias.plant) ? OpCodes.Ldarg_2 : OpCodes.Ldnull);
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                count++;
            }
            yield return instruction;
        }
        if (count != 1) throw new InvalidOperationException($"{__originalMethod.Name} has {count} seasonal bypass sites; expected one.");
    }
    private static bool PlantPrefix(HoeDirtAlias __instance, string itemId, Farmer who, bool isFertilizer, ref bool __result, out PlantState __state)
    {
        Item? seed = placing?.ItemId == CropCatalog.Raw(itemId) ? placing : who?.ActiveObject;
        __state = !isFertilizer && seed?.ItemId == CropCatalog.Raw(itemId)
            ? new(Traits.Read(seed.modData), Companion.Read(seed.modData)) : new([], null);
        if (!isFertilizer && __state.Values.Length > 0 && (!CropCatalog.Ground(__instance) || !CropCatalog.EligibleSeed(itemId)))
        {
            __result = false;
            if (who?.IsLocalPlayer == true) Game1.showRedMessage("Trait seeds need tilled ground, outside a planter.");
            return false;
        }
        return true;
    }
    private static void PlantPostfix(HoeDirtAlias __instance, string itemId, Farmer who, bool isFertilizer, bool __result, PlantState __state)
    {
        if (!__result || isFertilizer || __instance.crop == null) return;
        Crop crop = __instance.crop;
        bool eligible = CropCatalog.Ground(__instance) && CropCatalog.EligibleSeed(itemId);
        crop.modData[Traits.EligibilityKey] = eligible ? "true" : "false";
        Traits.Write(crop.modData, eligible ? __state.Values : []);
        Companion.Write(crop.modData, eligible ? __state.CompanionId : null);
        // Reapply vanilla speed calculation after transferring traits. This preserves profession/paddy/fertilizer effects.
        if (Core.TraitRules.Level(__state.Values, "fast_growth") > 0 || Companion.BaseDays(crop.modData) > 0
            || Traits.GrowthPenalty(crop.modData) > 0) __instance.applySpeedIncreases(who);
    }
    private static void GrowthPrefix(HoeDirtAlias __instance) => Companion.RemoveGrowthDelay(__instance);
    [HarmonyPriority(Priority.Last)]
    private static void GrowthPostfix(HoeDirtAlias __instance) => Companion.ApplyGrowth(__instance);
    private static void HarvestPrefix(Crop __instance, HoeDirtAlias soil, out HarvestContext? __state)
    {
        __state = HarvestContext.Current;
        HarvestContext.Current = Traits.Eligible(__instance, soil) && __instance.GetData() != null
            ? new HarvestContext(__instance) : null;
    }
    [HarmonyPriority(Priority.Last)]
    private static Exception? HarvestFinalizer(Exception? __exception, HarvestContext? __state, ref bool __result,
        StardewValley.Characters.JunimoHarvester? junimoHarvester)
    {
        HarvestContext? current = HarvestContext.Current;
        HarvestContext.Current = __state;
        if (__exception == null && current?.WasReady == true)
        {
            bool succeeded = __result || (current.Plant.fullyGrown.Value && current.Plant.dayOfCurrentPhase.Value > 0);
            if (succeeded) current.CompleteYield();
            if (succeeded)
                foreach (Item extra in current.PendingExtras)
                {
                    if (junimoHarvester != null) junimoHarvester.tryToAddItemToHut(extra);
                    else Game1.createItemDebris(extra, current.Plant.Dirt!.Tile * 64f + new Microsoft.Xna.Framework.Vector2(32),
                        -1, current.Plant.currentLocation);
                }
            if (current.Plant.Dirt is HoeDirtAlias soil) HarvestContext.ApplyRegrowth(current.Plant, soil);
            // Commit extras first. A successful annual harvest returns true to request removal;
            // only override that result after Rooted has restarted the plant successfully.
            if (succeeded && __result && current.TryRestart()) __result = false;
        }
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
            __result = Core.TraitRules.Same(string.Join(',', Traits.Read(__instance.modData)), string.Join(',', Traits.Read(item.modData)))
                && Companion.Read(__instance.modData) == Companion.Read(item.modData);
    }
    private static void DescriptionPostfix(SObject __instance, ref string __result)
    {
        string[] traits = Traits.Read(__instance.modData);
        if (traits.Length > 0) __result += "\n\nTraits: " + string.Join(", ", traits.Select(t => Core.TraitRules.Id(t) == "companion"
                ? Core.TraitRules.Label(t) + ": " + Companion.Label(__instance.modData) : Core.TraitRules.Label(t)));
    }
    private static bool DropPrefix(SObject __instance, Item dropInItem, bool probe, Farmer who, ref bool __result, bool returnFalseIfItemConsumed)
    {
        if (!Breeder.IsMachine(__instance)) return true;
        // Held inventory items are never deposited or consumed through the world interaction.
        // Treat an actual interaction as opening the station; probes don't advertise item input.
        __result = false;
        if (!probe && who?.IsLocalPlayer == true)
            ActionPrefix(__instance, who, false, ref __result);
        return false;
    }
    private static bool ActionPrefix(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (!Breeder.IsMachine(__instance)) return true;
        if (who.CurrentTool is Axe or Pickaxe)
        {
            __result = false;
            return false;
        }
        __result = true;
        if (!justCheckingForActivity && who.IsLocalPlayer && Game1.activeClickableMenu == null)
        {
            var location = who.currentLocation;
            var mutex = Breeder.MenuMutex(__instance, location);
            mutex.RequestLock(() =>
            {
                if (Game1.activeClickableMenu == null && who.CurrentTool is not Axe and not Pickaxe
                    && location.objects.TryGetValue(__instance.TileLocation, out var placed)
                    && ReferenceEquals(placed, __instance))
                    Game1.activeClickableMenu = new UI.BreedingMenu(__instance, location, mutex);
                else mutex.ReleaseLock();
            }, () => Game1.showRedMessage("This machine is in use."));
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
        output.modData.Remove(Companion.Key);
    }
    private static void CraftedPostfix(Item __result) { __result.modData.Remove(Traits.Key); __result.modData.Remove(Companion.Key); }
}
