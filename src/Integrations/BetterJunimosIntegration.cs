using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;

namespace CropBreeding.Integrations;

// Better Junimos constructs Crop directly. Capture the actual seed it selects, not a
// same-ID stack in the hut or the player's held seed. No scanning or repeating event.
internal static class BetterJunimosIntegration
{
    [ThreadStatic] private static PlantingAttempt? current;

    internal sealed class PlantingAttempt(object ability, GameLocation location, Vector2 tile)
    {
        internal readonly object Ability = ability;
        internal readonly GameLocation Location = location;
        internal readonly Vector2 Tile = tile;
        internal Item? Seed;
    }

    internal static void Register(Harmony harmony)
    {
        if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded("hawkfalcon.BetterJunimos")) return;
        var installed = new List<(MethodInfo Target, MethodInfo Patch)>();
        try
        {
            Type type = AccessTools.TypeByName("BetterJunimos.Abilities.PlantCropsAbility")
                ?? throw new TypeLoadException("Better Junimos PlantCropsAbility was not found.");
            MethodInfo Require(string name, Type result, params Type[] parameters)
            {
                var method = AccessTools.Method(type, name, parameters);
                if (method == null || method.IsStatic || method.ReturnType != result)
                    throw new MissingMethodException(type.FullName, name);
                return method;
            }
            var perform = Require("PerformAction", typeof(bool), typeof(GameLocation), typeof(Vector2), typeof(JunimoHarvester), typeof(Guid));
            var select = Require("PlantableSeed", typeof(Item), typeof(GameLocation), typeof(Chest), typeof(string));
            var plant = Require("Plant", typeof(bool), typeof(GameLocation), typeof(Vector2), typeof(string));
            // Validate all contracts before installing; remove only this integration's own additions on error.
            foreach (var entry in new[]
            {
                (perform, nameof(Begin), "prefix"), (perform, nameof(End), "finalizer"),
                (select, nameof(Selected), "postfix"), (plant, nameof(Planted), "postfix")
            })
            {
                MethodInfo patch = AccessTools.Method(typeof(BetterJunimosIntegration), entry.Item2);
                installed.Add((entry.Item1, patch));
                var method = new HarmonyMethod(patch);
                harmony.Patch(entry.Item1, prefix: entry.Item3 == "prefix" ? method : null,
                    postfix: entry.Item3 == "postfix" ? method : null, finalizer: entry.Item3 == "finalizer" ? method : null);
            }
        }
        catch (Exception ex)
        {
            foreach (var entry in installed)
                ErrorHandler.Try("Remove incomplete Better Junimos planting patch", () => harmony.Unpatch(entry.Target, entry.Patch));
            ErrorHandler.Report("Register Better Junimos planting", ex);
        }
    }

    internal static void Begin(object __instance, GameLocation location, Vector2 pos, out PlantingAttempt? __state)
    {
        __state = current;
        current = null;
        try { current = new(__instance, location, pos); }
        catch (Exception ex) { ErrorHandler.Report("Begin Better Junimos planting", ex); }
    }

    internal static Exception? End(Exception? __exception, PlantingAttempt? __state)
    {
        current = __state;
        return __exception;
    }

    [HarmonyPriority(Priority.Last)]
    internal static void Selected(object __instance, GameLocation location, Item? __result)
    {
        if (current is { } attempt && ReferenceEquals(attempt.Ability, __instance)
            && ReferenceEquals(attempt.Location, location)) attempt.Seed = __result;
    }

    [HarmonyPriority(Priority.Last)]
    internal static void Planted(object __instance, GameLocation location, Vector2 pos, string index, bool __result)
    {
        try
        {
            if (!__result || current is not { Seed: { } seed } attempt
                || !ReferenceEquals(attempt.Ability, __instance) || !ReferenceEquals(attempt.Location, location)
                || attempt.Tile != pos || seed.ItemId != CropCatalog.Raw(index)) return;
            // Consume the capture once, even if metadata preparation fails. Better Junimos
            // still owns successful planting, seed consumption, visuals and paddy watering.
            attempt.Seed = null;
            if (!seed.modData.ContainsKey(Traits.Key) || !CropCatalog.EligibleSeed(seed.ItemId)
                || !location.terrainFeatures.TryGetValue(pos, out var feature) || feature is not HoeDirtAlias soil
                || !CropCatalog.Ground(soil) || soil.crop is not { } crop
                || CropCatalog.Raw(crop.netSeedIndex.Value) != seed.ItemId) return;
            SeedInheritance.Apply(soil, seed.ItemId, Traits.Read(seed.modData), Companion.Read(seed.modData), Game1.player);
        }
        catch (Exception ex) { ErrorHandler.Report("Better Junimos seed traits", ex); }
    }
}
