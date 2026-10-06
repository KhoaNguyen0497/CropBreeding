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
        LookupTraitDescriptions.Register(harmony, subject, monitor);
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
            foreach (var target in installed)
                ErrorHandler.Try("Remove incomplete Lookup Anything patch", () => harmony.Unpatch(target, HarmonyPatchType.All, harmony.Id));
            ErrorHandler.Report("Register Lookup Anything display", ex);
        }
    }

    private static bool Supported(Crop? crop) => crop != null && crop.modData.ContainsKey(Traits.Key)
        && (Previews.TryGetValue(crop, out _) || (crop.Dirt is HoeDirtAlias soil && Traits.Eligible(crop, soil)));

    private static void SeedPostfix(Item seed, Crop? __result)
    {
        CropSnapshot? snapshot = null;
        try
        {
            if (__result == null || !seed.modData.ContainsKey(Traits.Key) || !CropCatalog.EligibleSeed(seed.ItemId)) return;
            snapshot = new CropSnapshot(__result);
            Traits.Write(__result.modData, Traits.Read(seed.modData));
            Companion.Write(__result.modData, Companion.Read(seed.modData));
            // Lookup Anything owns this detached crop. Never modify a planted crop or shared Data/Crops.
            int[] phases = TraitRules.PreviewGrowthPhases(__result.phaseDays.ToArray(),
                Traits.Level(seed.modData, "fast_growth"), ModEntry.Instance.Config.GrowthReductionPerLevel,
                Game1.player.professions.Contains(Farmer.agriculturist), Companion.BaseDays(seed.modData), Traits.GrowthPenalty(seed.modData));
            for (int i = 0; i < phases.Length; i++) __result.phaseDays[i] = phases[i];
            Previews.GetValue(__result, _ => new object());
        }
        catch (Exception ex)
        {
            if (snapshot != null) ErrorHandler.Try("Restore lookup seed preview", snapshot.Restore);
            if (__result != null) Previews.Remove(__result);
            ErrorHandler.Report("SeedPostfix", ex);
        }
    }

    private static void ParserPostfix(object __instance, Crop? crop)
    {
        object? originalFirst = null, originalRegrow = null, originalSeasons = null;
        bool captured = false;
        try
        {
            if (!Supported(crop)) return;
            originalFirst = firstDays.GetValue(__instance);
            originalRegrow = regrowDays.GetValue(__instance);
            originalSeasons = seasons.GetValue(__instance);
            captured = true;
            // Override Lookup Anything's extra Agriculturist approximation: the preview already applied it.
            firstDays.SetValue(__instance, crop!.phaseDays.Take(crop.phaseDays.Count - 1).Sum());
            int days = crop.GetData()?.RegrowDays ?? -1;
            regrowDays.SetValue(__instance, TraitRules.RegrowthDays(days, Traits.Level(crop.modData, "fast_growth"),
                ModEntry.Instance.Config.GrowthReductionPerLevel, Companion.BaseDays(crop.modData), Traits.GrowthPenalty(crop.modData)));
            if (TraitRules.EvergreenActive(Traits.Level(crop.modData, "evergreen")))
                seasons.SetValue(__instance, new[] { Season.Spring, Season.Summer, Season.Fall, Season.Winter });
        }
        catch (Exception ex)
        {
            if (captured) ErrorHandler.Try("Restore lookup fields", () =>
            {
                firstDays.SetValue(__instance, originalFirst);
                regrowDays.SetValue(__instance, originalRegrow);
                seasons.SetValue(__instance, originalSeasons);
            });
            ErrorHandler.Report("ParserPostfix", ex);
        }
    }

    private static void NextHarvestPostfix(object __instance, ref SDate __result)
    {
        SDate original = __result;
        try
        {
            if (parserCrop.GetValue(__instance) is Crop crop && Supported(crop) && crop.fullyGrown.Value)
                __result = SDate.Now().AddDays(Math.Max(0, crop.dayOfCurrentPhase.Value));
        }
        catch (Exception ex)
        {
            __result = original;
            ErrorHandler.Report("NextHarvestPostfix", ex);
        }
    }
}
