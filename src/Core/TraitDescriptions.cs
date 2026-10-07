using System.Globalization;

namespace CropBreeding.Core;

// Lookup-only text. No game/world access, RNG, or mutation of the supplied configuration.
internal static class TraitDescriptions
{
    internal static string Describe(string token, ModConfig config, string? companionName = null)
    {
        string id = TraitRules.Id(token);
        int level = TraitRules.Level(new[] { token }, id);
        if (level == 0) return "";
        string Chance(double perLevel) => Percent(Math.Clamp(perLevel * level, 0, 1));
        string? material = id switch
        {
            "copper_bearing" => "Copper Bar", "iron_bearing" => "Iron Bar", "gold_bearing" => "Gold Bar",
            "maple_bearing" => "Maple Syrup", "resin_bearing" => "Oak Resin", "tar_bearing" => "Pine Tar", _ => null
        };
        if (material != null)
            return $"{Percent(.05 * level)} chance for 1 {material} per harvest per full 5 base growth days.";

        return id switch
        {
            "fast_growth" => $"Growth and regrowth time reduced by {Chance(CropBreeding.Core.TraitRules.GrowthReductionPerLevel)} after all other modifiers. Rounded up; minimum 1 day.",
            "high_yield" => $"{Chance(CropBreeding.Core.TraitRules.ExtraYieldPerLevel)} more main crops per harvest.",
            "high_quality" => $"{Chance(CropBreeding.Core.TraitRules.QualityUpgradeChance)} chance per main crop to improve quality by one tier, up to iridium.",
            "companion" => (companionName == null
                ? $"Unassigned: choose a crop in the Breeding Machine to activate. {Chance(CropBreeding.Core.TraitRules.CompanionChance)} chance per harvest for 1 plain companion crop."
                : $"Companion: {companionName}. {Chance(CropBreeding.Core.TraitRules.CompanionChance)} chance per harvest for 1 plain companion crop.")
                + " When assigned, adds half its base growth days to growth and regrowth. Bonus crop has no traits or quality/yield bonuses.",
            "evergreen" => level < TraitRules.MaximumLevel
                ? "Dormant until level 5: allows planting and growing in all seasons."
                : "Allows planting and growing in all seasons.",
            "researcher" => $"Mutation chance +{Points(Math.Max(0, CropBreeding.Core.TraitRules.ResearcherMutationBonus) * level)} percentage points ({Percent(TraitRules.MutationRate(config.MutationChance, level, CropBreeding.Core.TraitRules.ResearcherMutationBonus))} total). Growth and regrowth take {Percent(Math.Max(0, CropBreeding.Core.TraitRules.ResearcherGrowthPenalty) * level)} longer.",
            "seed_saver" => $"{Chance(CropBreeding.Core.TraitRules.SeedSaverChance)} chance per harvest for 1 matching seed. Keeps the plant's original traits and Companion.",
            "rooted" => $"{Chance(CropBreeding.Core.TraitRules.RootedChance)} chance to restart a single-harvest crop from seed stage for free. Keeps original traits and Companion.",
            "nurse_crop" => $"Single-harvest crops: {Percent(.30 * level)} chance on harvest to advance adjacent non-fruit trees by one stage, stopping before maturity.",
            _ => ""
        };
    }

    private static string Points(double value) => (value * 100).ToString("0.##", CultureInfo.InvariantCulture);
    private static string Percent(double value) => Points(value) + "%";
}
