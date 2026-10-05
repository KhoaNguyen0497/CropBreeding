using HarmonyLib;
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
        monitor.Log("Auto Harvester breeding integration installed.", LogLevel.Debug);
    }
    private static void PlanPostfix(Crop crop, HoeDirtAlias soil, List<Item>? __result)
    {
        if (__result == null || !Traits.Eligible(crop, soil) || crop.GetData() == null) return;
        var context = new HarvestContext(crop);
        foreach (Item item in __result) context.Decorate(item);
        // Plan outputs are only stored if Auto Harvester has capacity. Deterministic rolls make an
        // abandoned plan harmless, and its own storage keeps the produce and crop removal atomic.
    }
}
