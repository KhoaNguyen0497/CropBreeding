using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;

namespace CropBreeding.Integrations;

internal static class BetterJunimosFertilizerIntegration
{
    internal static void Register(Harmony harmony)
    {
        if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded("hawkfalcon.BetterJunimos")) return;
        var installed = new List<(MethodInfo Target, MethodInfo Patch)>();
        try
        {
            Type type = AccessTools.TypeByName("BetterJunimos.Abilities.FertilizeAbility")
                ?? throw new TypeLoadException("Better Junimos FertilizeAbility was not found.");
            MethodInfo Require(string name, Type result, bool isStatic, params Type[] parameters)
            {
                var method = AccessTools.Method(type, name, parameters);
                if (method == null || method.IsStatic != isStatic || method.ReturnType != result)
                    throw new MissingMethodException(type.FullName, name);
                return method;
            }
            var available = Require("IsActionAvailable", typeof(bool), false, typeof(GameLocation), typeof(Vector2), typeof(Guid));
            var perform = Require("PerformAction", typeof(bool), false, typeof(GameLocation), typeof(Vector2), typeof(JunimoHarvester), typeof(Guid));
            var speed = Require("CheckSpeedGro", typeof(void), true, typeof(HoeDirtAlias), typeof(Crop));
            foreach (var entry in new[]
            {
                (available, nameof(Available), false), (perform, nameof(Perform), true), (speed, nameof(Speed), true)
            })
            {
                MethodInfo patch = AccessTools.Method(typeof(BetterJunimosFertilizerIntegration), entry.Item2);
                installed.Add((entry.Item1, patch));
                var method = new HarmonyMethod(patch);
                harmony.Patch(entry.Item1, prefix: entry.Item3 ? method : null, postfix: entry.Item3 ? null : method);
            }
        }
        catch (Exception ex)
        {
            // Fertilizer integration failure must not disable the separate planting integration.
            foreach (var entry in installed)
                ErrorHandler.Try("Remove incomplete Better Junimos fertilizer patch", () => harmony.Unpatch(entry.Target, entry.Patch));
            ErrorHandler.Report("Register Better Junimos fertilizing", ex);
        }
    }

    private static bool PastPlantingPhase(GameLocation location, Vector2 pos)
        => location.terrainFeatures.TryGetValue(pos, out var feature) && feature is HoeDirtAlias { crop: { } crop }
            && crop.currentPhase.Value != 0;

    [HarmonyPriority(Priority.Last)]
    internal static void Available(GameLocation location, Vector2 pos, ref bool __result)
    {
        try { if (__result && PastPlantingPhase(location, pos)) __result = false; }
        catch (Exception ex) { ErrorHandler.Report("Check Junimo fertilizer phase", ex); }
    }

    internal static bool Perform(GameLocation location, Vector2 pos, ref bool __result)
    {
        try
        {
            // Recheck at execution in case growth changed after the availability check.
            // Skip before Better Junimos changes fertilizer or consumes an inventory item.
            if (PastPlantingPhase(location, pos)) { __result = false; return false; }
        }
        catch (Exception ex) { ErrorHandler.Report("Check Junimo fertilizer action", ex); }
        return true;
    }

    internal static bool Speed(HoeDirtAlias hd, Crop? crop)
    {
        if (crop == null) return false; // Empty soil: fertilizer applies normally; nothing to recalculate.
        if (!ReferenceEquals(hd.crop, crop)) return true;
        CropSnapshot? snapshot = null;
        try
        {
            snapshot = new CropSnapshot(crop);
            // Our normal growth hooks remove previous trait deltas, let vanilla handle
            // fertilizer/profession/paddy speed, then apply Companion/Researcher/Fast Growth once.
            hd.applySpeedIncreases(Game1.player);
            return false;
        }
        catch (Exception ex)
        {
            if (snapshot != null) ErrorHandler.Try("Restore Junimo fertilizer timing", snapshot.Restore);
            ErrorHandler.Report("Apply Junimo fertilizer timing", ex);
            // Fall back to Better Junimos' original calculation. Do not replay fertilizing
            // or seed/fertilizer consumption, and do not suppress errors from its original code.
            return true;
        }
    }
}
