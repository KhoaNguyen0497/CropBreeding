using System;
using System.Collections.Generic;
using System.Linq;

namespace CropBreeding.Core;

public static class TraitRules
{
    public const int MaximumLevel = 5;
    // Fixed gameplay balance; only the base mutation chance is configurable.
    public const double GrowthReductionPerLevel = .05;
    public const double ExtraYieldPerLevel = .20;
    public const double CompanionChance = .20;
    public const double QualityUpgradeChance = .05;
    public const double SeedSaverChance = .10;
    public const double RootedChance = .10;
    public const double ResearcherMutationBonus = .05;
    public const double ResearcherGrowthPenalty = .10;

    public static readonly string[] Known = ["fast_growth", "high_yield", "high_quality", "companion", "evergreen", "researcher", "seed_saver", "copper_bearing", "iron_bearing", "gold_bearing", "rooted", "nurse_crop",
        "maple_bearing", "resin_bearing", "tar_bearing"];
    // Exact object IDs, with independent stable rolls.
    public static readonly IReadOnlyDictionary<string, (string ItemId, int Salt)> MaterialDrops =
        new Dictionary<string, (string ItemId, int Salt)>(StringComparer.Ordinal)
        {
            ["copper_bearing"] = ("334", 83),
            ["iron_bearing"] = ("335", 89),
            ["gold_bearing"] = ("336", 97),
            ["maple_bearing"] = ("724", 127),
            ["resin_bearing"] = ("725", 131),
            ["tar_bearing"] = ("726", 137)
        };
    public static int NurseCropStages(int level, bool canRegrow, double roll)
    {
        if (canRegrow) return 0;
        int percent = Math.Clamp(level, 0, MaximumLevel) * 30;
        return percent / 100 + (roll < (percent % 100) / 100.0 ? 1 : 0);
    }
    public static int AdvanceImmatureTree(int currentStage, int stages, int matureStage)
        => currentStage < 0 || currentStage >= matureStage - 1 || stages <= 0
            ? currentStage : currentStage + Math.Min(stages, matureStage - 1 - currentStage);
    public static bool RootedTriggers(int level, double chancePerLevel, bool canRegrow, double roll)
        => !canRegrow && roll < Math.Clamp(Math.Clamp(level, 0, MaximumLevel) * chancePerLevel, 0, 1);
    public static int MaterialDropCount(int baseGrowthDays, int level, double roll)
    {
        // Each full five-day block contributes one 5% unit per inherited level.
        int units = (Math.Max(0, baseGrowthDays) / 5) * Math.Clamp(level, 0, MaximumLevel);
        return units / 20 + (roll < (units % 20) / 20.0 ? 1 : 0);
    }
    public static double MutationRate(double baseline, int researcherLevel, double bonusPerLevel)
        => Math.Clamp(baseline + Math.Clamp(researcherLevel, 0, MaximumLevel) * Math.Max(0, bonusPerLevel), 0, 1);
    public static bool EvergreenActive(int level) => level >= MaximumLevel;
    public static int ExtraYieldCount(int count, int level, double increasePerLevel, double roll)
    {
        double extra = Math.Max(0, count) * Math.Clamp(increasePerLevel * Math.Clamp(level, 0, MaximumLevel), 0, 1);
        int guaranteed = (int)Math.Floor(extra);
        return guaranteed + (roll < extra - guaranteed ? 1 : 0);
    }
    // Detached seed preview: mirror vanilla applySpeedIncreases' phase rounding and three-pass limit.
    // No fertilizer or paddy bonus can be assumed before a planting tile is selected.
    public static int[] PreviewGrowthPhases(int[] original, int fastGrowthLevel, double reductionPerLevel, bool agriculturist, int companionBaseDays, double growthPenalty = 0)
    {
        int[] phases = (int[])original.Clone();
        float speed = agriculturist ? .1f : 0;
        int remove = (int)Math.Ceiling(phases.Take(Math.Max(0, phases.Length - 1)).Sum() * speed);
        for (int pass = 0; pass < 3 && remove > 0; pass++)
            for (int i = 0; i < phases.Length && remove > 0; i++)
                if ((i > 0 || phases[i] > 1) && phases[i] != 99999 && phases[i] > 0)
                { phases[i]--; remove--; }
        return FinalGrowthPhases(phases, fastGrowthLevel, reductionPerLevel, companionBaseDays, growthPenalty);
    }
    public static int[] FinalGrowthPhases(int[] original, int level, double reductionPerLevel, int companionBaseDays, double growthPenalty = 0)
    {
        int[] phases = (int[])original.Clone();
        if (phases.Length < 2) return phases;
        int baseDays = phases.Take(phases.Length - 1).Sum();
        int target = RegrowthDays(baseDays, level, reductionPerLevel, companionBaseDays, growthPenalty);
        phases[phases.Length - 2] += CompanionDelay(companionBaseDays);
        int remove = phases.Take(phases.Length - 1).Sum() - target;
        if (remove < 0) phases[phases.Length - 2] -= remove;
        while (remove > 0)
        {
            bool changed = false;
            for (int i = 0; i < phases.Length - 1 && remove > 0; i++)
                if (phases[i] > (i == 0 ? 1 : 0))
                { phases[i]--; remove--; changed = true; }
            if (!changed) break;
        }
        return phases;
    }
    public static string Id(string trait) => trait.Split(':')[0];
    private static int TokenLevel(string token)
    {
        string[] parts = token.Split(':');
        return parts.Length == 2 && int.TryParse(parts[1], out int level) && level > 0
            ? Math.Min(level, MaximumLevel) : 0;
    }
    private static string Token(string id, int level) => $"{id}:{level}";
    public static string[] Parse(string? value) => (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Where(t => Known.Contains(Id(t)) && TokenLevel(t) > 0)
        .GroupBy(Id).OrderBy(g => g.Key, StringComparer.Ordinal)
        .Select(g => Token(g.Key, g.Max(TokenLevel))).ToArray();
    public static string Encode(IEnumerable<string> values) => string.Join(',', Parse(string.Join(',', values)));
    public static string[] Without(IEnumerable<string> values, string id)
        => Parse(Encode(values)).Where(t => Id(t) != id).ToArray();
    public static int Level(IEnumerable<string> traits, string id) => Parse(Encode(traits))
        .Where(t => Id(t) == id).Select(TokenLevel).DefaultIfEmpty(0).Max();
    public static string[] Mutate(IEnumerable<string> inherited, int limit, double chance, Random random, bool canRegrow = true,
        Func<string, bool>? isAvailable = null)
    {
        string[] current = Parse(Encode(inherited));
        if (random.NextDouble() >= Math.Clamp(chance, 0, 1)) return current;
        // Roll the full crop-eligible pool before checking slots/levels. Blocked picks are wasted, never rerolled.
        string[] choices = Known.Where(id => (isAvailable?.Invoke(id) ?? true)
            && (id is not ("rooted" or "nurse_crop") || !canRegrow)).ToArray();
        if (choices.Length == 0) return current;
        string chosen = choices[random.Next(choices.Length)];
        int level = Level(current, chosen);
        if (level >= MaximumLevel || (level == 0 && current.Length >= Math.Max(0, limit))) return current;
        return Parse(Encode(current.Where(t => Id(t) != chosen).Append(Token(chosen, level + 1))));
    }
    public static bool TryBreed(IEnumerable<string> donor, IEnumerable<string> seed, int limit, out string[] result)
    {
        result = Parse(Encode(donor));
        string[] seedTraits = Parse(Encode(seed));
        // Plain seeds copy the donor, preserving traits if the user lowered the configured count cap.
        if (seedTraits.Length == 0) return result.Length > 0;
        if (result.Length == 0 || seedTraits.Length != 1 || TokenLevel(seedTraits[0]) != 1) return false;
        string id = Id(seedTraits[0]);
        int level = Level(result, id);
        if (result.Length > Math.Max(0, limit) || level >= MaximumLevel
            || (level == 0 && result.Length >= Math.Max(0, limit))) return false;
        result = Parse(Encode(result.Where(t => Id(t) != id).Append(Token(id, level + 1))));
        return true;
    }
    public static int CompanionDelay(int baseDays) => (int)Math.Ceiling(Math.Max(0, baseDays) * .5);
    public static string? CompanionChoice(string? donor, string? seed) => !string.IsNullOrEmpty(donor) ? donor : seed;
    public static int HarvestQuality(int quality, int inheritedLevel, double chancePerLevel, double roll)
    {
        if (inheritedLevel <= 0 || roll >= Math.Clamp(chancePerLevel * Math.Clamp(inheritedLevel, 0, MaximumLevel), 0, 1))
            return quality;
        return quality switch { 0 => 1, 1 => 2, 2 => 4, _ => quality };
    }
    public static int RegrowthDays(int days, int level, double reductionPerLevel, int companionBaseDays = 0, double growthPenalty = 0)
    {
        if (days <= 0) return days;
        // Whole-day countdown: round up, with a minimum of one day.
        double reduction = Math.Clamp(reductionPerLevel * Math.Clamp(level, 0, MaximumLevel), 0, 1);
        return Math.Max(1, (int)Math.Ceiling((days + Math.Max(0, companionBaseDays) * .5) * (1 + Math.Max(0, growthPenalty)) * (1 - reduction) - 1e-9));
    }
    public static bool Same(string? a, string? b) => Encode(Parse(a)) == Encode(Parse(b));
    public static string Label(string token)
    {
        string name = Id(token) switch
        {
            "fast_growth" => "Fast Growth", "high_yield" => "High Yield", "high_quality" => "High Quality", "companion" => "Companion", "evergreen" => "Evergreen", "researcher" => "Researcher", "seed_saver" => "Seed Saver",
            "copper_bearing" => "Copper Bearing", "iron_bearing" => "Iron Bearing", "gold_bearing" => "Gold Bearing", "rooted" => "Rooted", "nurse_crop" => "Nurse Crop",
            "maple_bearing" => "Maple Bearing", "resin_bearing" => "Resin Bearing", "tar_bearing" => "Tar Bearing", _ => Id(token)
        };
        return $"{name} {TokenLevel(token)}";
    }
}
