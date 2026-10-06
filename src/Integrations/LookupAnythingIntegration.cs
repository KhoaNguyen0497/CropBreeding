using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using CropBreeding.Core;

namespace CropBreeding.Integrations;

internal static class LookupAnythingIntegration
{
    private static readonly ConditionalWeakTable<Crop, object> Previews = new();
    private static FieldInfo firstDays = null!, regrowDays = null!, seasons = null!;
    private static PropertyInfo parserCrop = null!;

    internal static void Register(Harmony harmony, IMonitor monitor)
    {
        if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded("Pathoschild.LookupAnything")) return;
        Type? subject = AccessTools.TypeByName("Pathoschild.Stardew.LookupAnything.Framework.Lookups.Items.ItemSubject");
        Type? parser = subject?.Assembly.GetType("Pathoschild.Stardew.Common.DataParsers.CropDataParser");
        var seedMethod = subject == null ? null : AccessTools.Method(subject, "TryGetCropForSeed", new[] { typeof(Item), typeof(GameLocation) });
        var constructor = parser == null ? null : AccessTools.Constructor(parser, new[] { typeof(Crop), typeof(bool) });
        var next = parser == null ? null : AccessTools.Method(parser, "GetNextHarvest");
        firstDays = parser == null ? null! : AccessTools.Field(parser, "<DaysToFirstHarvest>k__BackingField");
        regrowDays = parser == null ? null! : AccessTools.Field(parser, "<DaysToSubsequentHarvest>k__BackingField");
        seasons = parser == null ? null! : AccessTools.Field(parser, "<Seasons>k__BackingField");
        parserCrop = parser == null ? null! : AccessTools.Property(parser, "Crop");
        if (seedMethod?.ReturnType != typeof(Crop) || constructor == null || next?.ReturnType != typeof(SDate)
            || firstDays?.FieldType != typeof(int) || regrowDays?.FieldType != typeof(int)
            || seasons?.FieldType != typeof(Season[]) || parserCrop?.PropertyType != typeof(Crop))
        {
            monitor.Log("Lookup Anything's crop display API was not recognized; breeding display integration is disabled.", LogLevel.Warn);
            return;
        }
        var installed = new List<MethodBase>();
        try
        {
            foreach (var (target, postfix) in new (MethodBase, string)[]
            {
                (seedMethod, nameof(SeedPostfix)), (constructor, nameof(ParserPostfix)), (next, nameof(NextHarvestPostfix))
            })
            {
                installed.Add(target);
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(LookupAnythingIntegration), postfix));
            }
        }
        catch (Exception ex)
        {
            foreach (var target in installed) harmony.Unpatch(target, HarmonyPatchType.All, harmony.Id);
            monitor.Log($"Lookup Anything display integration disabled: {ex}", LogLevel.Warn);
        }
    }

    private static bool Supported(Crop? crop) => crop != null && crop.modData.ContainsKey(Traits.Key)
        && (Previews.TryGetValue(crop, out _) || (crop.Dirt is HoeDirtAlias soil && Traits.Eligible(crop, soil)));

    private static void SeedPostfix(Item seed, Crop? __result)
    {
        if (__result == null || !seed.modData.ContainsKey(Traits.Key) || !CropCatalog.EligibleSeed(seed.ItemId)) return;
        Traits.Write(__result.modData, Traits.Read(seed.modData));
        Companion.Write(__result.modData, Companion.Read(seed.modData));
        // Lookup Anything owns this detached crop. Never modify a planted crop or shared Data/Crops.
        int[] phases = TraitRules.PreviewGrowthPhases(__result.phaseDays.ToArray(),
            Traits.Level(seed.modData, "fast_growth"), ModEntry.Instance.Config.GrowthReductionPerLevel,
            Game1.player.professions.Contains(Farmer.agriculturist), Companion.BaseDays(seed.modData), Traits.GrowthPenalty(seed.modData));
        for (int i = 0; i < phases.Length; i++) __result.phaseDays[i] = phases[i];
        Previews.GetValue(__result, _ => new object());
    }

    private static void ParserPostfix(object __instance, Crop? crop)
    {
        if (!Supported(crop)) return;
        // Override Lookup Anything's extra Agriculturist approximation: the preview already applied it.
        firstDays.SetValue(__instance, crop!.phaseDays.Take(crop.phaseDays.Count - 1).Sum());
        int days = crop.GetData()?.RegrowDays ?? -1;
        regrowDays.SetValue(__instance, TraitRules.RegrowthDays(days, Traits.Level(crop.modData, "fast_growth"),
            ModEntry.Instance.Config.GrowthReductionPerLevel, Companion.BaseDays(crop.modData), Traits.GrowthPenalty(crop.modData)));
        if (TraitRules.EvergreenActive(Traits.Level(crop.modData, "evergreen")))
            seasons.SetValue(__instance, new[] { Season.Spring, Season.Summer, Season.Fall, Season.Winter });
    }

    private static void NextHarvestPostfix(object __instance, ref SDate __result)
    {
        if (parserCrop.GetValue(__instance) is Crop crop && Supported(crop) && crop.fullyGrown.Value)
            __result = SDate.Now().AddDays(Math.Max(0, crop.dayOfCurrentPhase.Value));
    }
}
