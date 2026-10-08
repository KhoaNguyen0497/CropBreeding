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
                ? $"Unassigned: choose a crop in the Breeding Machine to activate. Up to {Chance(CropBreeding.Core.TraitRules.CompanionChance)} chance per harvest for 1 plain companion crop."
                : $"Companion: {companionName}. Up to {Chance(CropBreeding.Core.TraitRules.CompanionChance)} chance per harvest for 1 plain companion crop.")
                + " Chance scales with the main crop's base regrowth days / 10, or base growth days / 7 for single-harvest crops, capped at full chance. Adds half the companion's base growth days to growth and regrowth. Bonus crop has no traits or quality/yield bonuses.",
            "evergreen" => level < TraitRules.MaximumLevel
                ? "Dormant until level 5: allows planting and growing in all seasons."
                : "Allows planting and growing in all seasons.",
            "researcher" => "Single-level trait. Research Machine replaces it with two successful trait rolls in 2 in-game hours. Existing traits can be upgraded; trait and level caps apply.",
            "seed_saver" => $"{Chance(CropBreeding.Core.TraitRules.SeedSaverChance)} chance per harvest for 1 matching seed. Keeps the plant's original traits and Companion.",
            "rooted" => $"{Chance(CropBreeding.Core.TraitRules.RootedChance)} chance to restart a single-harvest crop from seed stage for free. Keeps original traits and Companion.",
            "nurse_crop" => $"Single-harvest crops: {Percent(.30 * level)} chance on harvest to advance adjacent non-fruit trees by one stage, stopping before maturity.",
            _ => ""
        };
    }

    private static string Points(double value) => (value * 100).ToString("0.##", CultureInfo.InvariantCulture);
    private static string Percent(double value) => Points(value) + "%";
}
