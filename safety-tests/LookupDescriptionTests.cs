using CropBreeding.Core;
using CropBreeding.Integrations;
using HarmonyLib;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;
using StardewValley;

namespace CropBreeding;

internal static class LookupDescriptionTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    internal static void Run()
    {
        var config = new ModConfig();
        foreach (string id in TraitRules.Known)
            for (int level = 1; level <= 5; level++)
                Check(!string.IsNullOrWhiteSpace(TraitDescriptions.Describe($"{id}:{level}", config)), $"description for {id} {level}");
        Check(TraitDescriptions.Describe("unknown:1", config) == "", "unknown trait has no invented description");
        Check(TraitDescriptions.Describe("fast_growth:5", config).Contains("25%"), "default growth percentage");
        Check(TraitDescriptions.Describe("high_yield:5", config).Contains("100%"), "default yield percentage");
        Check(TraitDescriptions.Describe("high_quality:3", config).Contains("15%"), "quality percentage");
        Check(TraitDescriptions.Describe("researcher:1", config).Contains("two successful trait rolls")
            && TraitDescriptions.Describe("researcher:1", config).Contains("Single-level"), "Researcher explains machine conversion");
        for (int level = 1; level <= 5; level++)
            Check(!TraitDescriptions.Describe($"researcher:{level}", config).Contains("longer")
                && !TraitDescriptions.Describe($"researcher:{level}", config).Contains("growth"), "Researcher description has no growth penalty");
        Check(TraitDescriptions.Describe("evergreen:4", config).Contains("Dormant")
            && !TraitDescriptions.Describe("evergreen:5", config).Contains("Dormant"), "Evergreen activation level");
        Check(TraitDescriptions.Describe("companion:2", config).Contains("Unassigned")
            && TraitDescriptions.Describe("companion:2", config, "Parsnip").Contains("Companion: Parsnip. Up to 40%"), "Companion choice and maximum odds");
        Check(TraitDescriptions.Describe("companion:5", config).Contains("base regrowth days / 10")
            && TraitDescriptions.Describe("companion:5", config).Contains("base growth days / 7"), "Companion explains both base-time thresholds");
        Check(TraitDescriptions.Describe("nurse_crop:5", config).Contains("150%"), "Nurse Crop overflow shown");
        foreach (string id in TraitRules.MaterialDrops.Keys)
            Check(TraitDescriptions.Describe(id + ":5", config).Contains(TraitRules.MaterialDrops[id].ChancePercent == 10 ? "50%" : "25%")
                && TraitDescriptions.Describe(id + ":5", config).Contains("per full 5 base growth days"), "material base growth formula");
        config.MutationChance = .20;
        Check(TraitDescriptions.Describe("fast_growth:2", config).Contains("10%")
            && TraitDescriptions.Describe("high_yield:5", config).Contains("100%"), "trait rates stay fixed when mutation config changes");

        Check(!TraitDescriptions.Describe("researcher:1", config).Contains("%"), "Researcher no longer advertises a mutation bonus");

        var harmony = new Harmony();
        LookupTraitDescriptions.Register(harmony, typeof(LookupSubject), ModEntry.Instance.Monitor);
        Check(harmony.Postfix?.IsGenericMethod == true && !harmony.Postfix.ContainsGenericParameters, "native field type closes generic postfix");
        ModEntry.Instance.Config = new ModConfig();
        var item = new Item();
        item.modData[Traits.Key] = "companion:2,high_yield:3";
        item.modData["Companion"] = "Parsnip";
        int enumerations = 0;
        IEnumerable<ICustomField> Original()
        {
            enumerations++;
            yield return new GenericField("Vanilla", "unchanged");
        }
        IEnumerable<ICustomField> Apply(LookupSubject subject, IEnumerable<ICustomField> original)
        {
            object[] args = [subject, original];
            harmony.Postfix!.Invoke(null, args);
            return (IEnumerable<ICustomField>)args[1];
        }
        var original = Original();
        var result = Apply(new LookupSubject(item), original);
        Check(enumerations == 0, "original lookup remains lazy");
        var fields = result.ToArray();
        Check(enumerations == 1 && fields.Length == 4 && fields[^1].Label == "Vanilla", "append original fields once without changing them");
        Check(fields.Any(f => f.Label == "High Yield 3" && f.Value.Contains("60%"))
            && fields.Any(f => f.Label == "Companion 2" && f.Value.Contains("Parsnip")), "seed/produce trait rows contain current level and companion");
        var crop = new Crop(); crop.modData[Traits.Key] = "rooted:4";
        fields = Apply(new LookupSubject(item, crop), []).ToArray();
        Check(fields.Length == 2 && fields[1].Label == "Rooted 4" && fields[1].Value.Contains("40%"), "planted crop metadata takes priority over sample item");
        Check(!crop.modData.ContainsKey(MutationState.Key) && crop.modData[Traits.Key] == "rooted:4", "lookup never prepares mutations or changes traits");
        var plain = new Item();
        Check(ReferenceEquals(Apply(new LookupSubject(plain), original), original), "plain items unchanged");
        crop.dead.Value = true;
        Check(ReferenceEquals(Apply(new LookupSubject(item, crop), original), original), "dead crop lookup unchanged");
        GenericField.Throw = true;
        Check(ReferenceEquals(Apply(new LookupSubject(item), original), original), "field construction failure restores original lookup");
        GenericField.Throw = false;
        Check(Apply(new LookupSubject(item), []).Count() == 3, "later lookup succeeds after error");
        var failed = new Harmony { ThrowOnPatch = true };
        LookupTraitDescriptions.Register(failed, typeof(LookupSubject), ModEntry.Instance.Monitor);
        Check(failed.Unpatched, "partial registration cleaned up");
        var incompatible = new Harmony();
        LookupTraitDescriptions.Register(incompatible, typeof(Item), ModEntry.Instance.Monitor);
        Check(incompatible.Postfix == null, "unknown lookup contract skipped safely");
        Console.WriteLine("Passed all 16 trait descriptions at levels 1-5, config-aware values, companion text, native lookup field construction, source precedence, lazy enumeration and error fallback. Uses test doubles.");
    }
}
