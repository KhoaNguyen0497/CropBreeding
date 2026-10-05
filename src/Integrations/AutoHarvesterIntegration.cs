using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace CropBreeding.Integrations;

internal static class AutoHarvesterIntegration
{
    internal static void Register(Harmony harmony, IMonitor monitor)
    {
        Type? type = AccessTools.TypeByName("AutoHarvester.HarvesterService");
        if (type == null) return;
        var method = AccessTools.Method(type, "Plan");
        if (method == null || method.ReturnType != typeof(List<Item>)
            || !method.GetParameters().Any(p => p.Name == "crop" && p.ParameterType == typeof(Crop))
            || !method.GetParameters().Any(p => p.Name == "soil" && p.ParameterType == typeof(HoeDirtAlias)))
        {
            monitor.Log("Auto Harvester's harvest planner was not recognized; its trait integration is disabled.", LogLevel.Warn);
            return;
        }
        harmony.Patch(method, postfix: new HarmonyMethod(typeof(AutoHarvesterIntegration), nameof(PlanPostfix)));
        var run = AccessTools.Method(type, "Run");
        if (run != null && run.GetParameters().Any(p => p.Name == "location" && p.ParameterType == typeof(GameLocation))
            && run.GetParameters().Any(p => p.Name == "center" && p.ParameterType == typeof(Vector2))
            && run.GetParameters().Any(p => p.Name == "range" && p.ParameterType == typeof(int)))
            harmony.Patch(run, prefix: new HarmonyMethod(typeof(AutoHarvesterIntegration), nameof(RunPrefix)),
                postfix: new HarmonyMethod(typeof(AutoHarvesterIntegration), nameof(RunPostfix)));
        else monitor.Log("Auto Harvester's run method was not recognized; Fast Regrowth integration is disabled.", LogLevel.Warn);
        monitor.Log("Auto Harvester breeding integration installed.", LogLevel.Debug);
    }
    private static void RunPrefix(GameLocation location, Vector2 center, int range,
        out List<(Crop Crop, HoeDirtAlias Soil)> __state)
    {
        __state = [];
        // Inspect only the machine's range, never scan the entire location.
        int radius = range / 2;
        for (int y = (int)center.Y - radius; y <= (int)center.Y + radius; y++)
            for (int x = (int)center.X - radius; x <= (int)center.X + radius; x++)
                if (location.terrainFeatures.TryGetValue(new Vector2(x, y), out var feature)
                    && feature is HoeDirtAlias soil && soil.crop is Crop crop
                    && HarvestContext.Ready(crop) && Traits.Eligible(crop, soil)
                    && Traits.Has(crop.modData, "fast_regrowth"))
                    __state.Add((crop, soil));
    }
    private static void RunPostfix(List<(Crop Crop, HoeDirtAlias Soil)> __state)
    {
        foreach (var entry in __state) HarvestContext.ApplyRegrowth(entry.Crop, entry.Soil);
    }
    private static void PlanPostfix(Crop crop, HoeDirtAlias soil, List<Item>? __result)
    {
        if (__result == null || !Traits.Eligible(crop, soil) || crop.GetData() == null) return;
        var context = new HarvestContext(crop);
        var outputs = __result.SelectMany(context.Decorate).ToList();
        __result.Clear();
        __result.AddRange(outputs);
        // Plan outputs are only stored if Auto Harvester has capacity. Deterministic rolls make an
        // abandoned plan harmless, and its own storage keeps the produce and crop removal atomic.
    }
}
