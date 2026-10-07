using System.Reflection;
using CropBreeding.Core;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CropBreeding.Integrations;

// Lookup Anything has no compile-time dependency. Validate its native field contract once;
// reflection and text generation below run only when a lookup's fields are requested.
internal static class LookupTraitDescriptions
{
    private static FieldInfo target = null!, fromCrop = null!;
    private static ConstructorInfo fieldConstructor = null!;

    internal static void Register(Harmony harmony, Type? subject, IMonitor monitor)
    {
        MethodInfo? method = null;
        bool attempted = false;
        try
        {
            if (subject == null) return;
            method = AccessTools.Method(subject, "GetData", Type.EmptyTypes);
            target = AccessTools.Field(subject, "Target");
            fromCrop = AccessTools.Field(subject, "FromCrop");
            Type? field = subject.Assembly.GetType("Pathoschild.Stardew.LookupAnything.Framework.Fields.GenericField");
            fieldConstructor = field == null ? null! : AccessTools.Constructor(field, [typeof(string), typeof(string), typeof(bool?)]);
            Type? result = method?.ReturnType;
            Type? fieldType = result?.IsGenericType == true && result.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? result.GetGenericArguments()[0] : null;
            if (method == null || target?.FieldType != typeof(Item) || fromCrop?.FieldType != typeof(Crop)
                || fieldType == null || !fieldType.IsInterface || field == null || !fieldType.IsAssignableFrom(field) || fieldConstructor == null)
            {
                monitor.Log("Lookup Anything's trait field API was not recognized; trait descriptions are disabled. Other integrations are unaffected.", LogLevel.Warn);
                return;
            }
            MethodInfo postfix = AccessTools.Method(typeof(LookupTraitDescriptions), nameof(FieldsPostfix)).MakeGenericMethod(fieldType);
            attempted = true;
            harmony.Patch(method, postfix: new HarmonyMethod(postfix));
        }
        catch (Exception ex)
        {
            if (attempted && method != null)
                ErrorHandler.Try("Remove incomplete lookup trait patch", () => harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id));
            ErrorHandler.Report("Register lookup trait descriptions", ex);
        }
    }

    private static void FieldsPostfix<T>(object __instance, ref IEnumerable<T> __result)
    {
        // Build additions atomically, before wrapping the original lazy sequence. Never catch or
        // suppress exceptions from Lookup Anything's own enumerator, or enumerate it twice.
        IEnumerable<T> original = __result;
        try
        {
            if (target.GetValue(__instance) is not Item item) return;
            Crop? crop = fromCrop.GetValue(__instance) as Crop;
            var metadata = crop?.modData ?? item.modData;
            if (!metadata.ContainsKey(Traits.Key) || crop?.dead.Value == true) return;
            string[] traits = Traits.Read(metadata);
            if (traits.Length == 0) return;
            string? companion = traits.Any(t => TraitRules.Id(t) == "companion") && Companion.Read(metadata) != null
                ? Companion.Label(metadata) : null;
            var additions = new List<T>();
            Add("Breeding traits", crop == null
                ? "Effects apply to plants grown with these inherited traits, not to processing this item."
                : "Inherited plant effects. A pending mutation changes harvested produce, not this plant's bonuses.");
            if (crop != null)
                Add("Settings timing", "Trait percentages use current settings. Existing growth/countdowns keep their stored timing until recalculated; regrowth settings apply after the next harvest. Ready crops keep their saved mutation roll.");
            foreach (string token in traits)
            {
                string description = TraitDescriptions.Describe(token, ModEntry.Instance.Config, companion);
                if (description.Length > 0) Add(TraitRules.Label(token), description);
            }
            __result = additions.Concat(original);

            void Add(string label, string value) => additions.Add((T)fieldConstructor.Invoke([label, value, null]));
        }
        catch (Exception ex)
        {
            __result = original;
            ErrorHandler.Report("Lookup trait descriptions", ex);
        }
    }
}
