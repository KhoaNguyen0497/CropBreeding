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
            return $"Each full 5 days of base initial growth gives {Percent(.05 * level)} chance to produce 1 {material} per harvest. Each full 100% guarantees an item; the remainder can add one more. Uses initial growth even on regrowers. Drops are normal quality and trait-free; growth buffs and High Yield do not affect them.";

        return id switch
        {
            "fast_growth" => $"Reduces initial growth and natural regrowth time by {Chance(CropBreeding.Core.TraitRules.GrowthReductionPerLevel)}, after other bonuses, Companion delay and Researcher penalty. Rounded up to whole days, minimum 1 day. Does not make single-harvest crops regrow.",
            "high_yield" => $"Produces {Chance(CropBreeding.Core.TraitRules.ExtraYieldPerLevel)} more primary crops per harvest. Whole extra items are guaranteed; the fractional remainder is a chance for one more. Does not multiply Companion output, material drops or byproducts.",
            "high_quality" => $"Each primary harvested item has a {Chance(CropBreeding.Core.TraitRules.QualityUpgradeChance)} chance to rise one quality tier after normal farming/fertilizer quality: normal to silver, silver to gold, gold to iridium. Iridium stays unchanged. Includes High Yield extras, not byproducts.",
            "companion" => (companionName == null
                ? $"Unassigned: no extra crop or delay yet. Choose a companion in the Breeding Machine. Once assigned: {Chance(CropBreeding.Core.TraitRules.CompanionChance)} chance per harvest for 1 normal-quality, trait-free companion crop."
                : $"Companion: {companionName}. {Chance(CropBreeding.Core.TraitRules.CompanionChance)} chance per harvest for 1 normal-quality, trait-free companion crop.")
                + " Adds half the companion's base initial growth time to both initial growth and natural regrowth, before Researcher and Fast Growth. High Yield does not multiply the extra crop.",
            "evergreen" => level < TraitRules.MaximumLevel
                ? "Dormant at levels 1-4. Level 5 allows planting and survival in every season, including winter. Does not make single-harvest crops regrow."
                : "Allows planting and survival in every season, including winter. Natural regrowers keep producing across seasons. Single-harvest crops remain single-harvest.",
            "researcher" => $"Adds {Points(Math.Max(0, CropBreeding.Core.TraitRules.ResearcherMutationBonus) * level)} percentage points to the readiness mutation roll: {Percent(TraitRules.MutationRate(config.MutationChance, level, CropBreeding.Core.TraitRules.ResearcherMutationBonus))} total with current settings. Initial growth and natural regrowth take {Percent(Math.Max(0, CropBreeding.Core.TraitRules.ResearcherGrowthPenalty) * level)} longer, including Companion delay, before Fast Growth. Blocked trait picks are not rerolled; disabled regrowing mutations stay disabled.",
            "seed_saver" => $"{Chance(CropBreeding.Core.TraitRules.SeedSaverChance)} chance per harvest to return 1 matching seed with the parent's original traits and Companion choice, not the pending mutation. Works on regrowers and coffee. High Yield and High Quality do not modify it.",
            "rooted" => $"{Chance(CropBreeding.Core.TraitRules.RootedChance)} chance after a single-harvest crop is harvested to restart it from seed stage without consuming a seed. Keeps original traits, Companion choice, colour and soil fertilizer. Normal seasonal restrictions still apply. No effect on natural regrowers.",
            "nurse_crop" => $"Single-harvest crops only: {Percent(.30 * level)} chance on harvest to advance non-fruit trees in all 8 neighboring tiles. Each full 100% guarantees a stage; the remainder can add one more. All affected trees get the same stage bonus, stopping one stage short of maturity. No effect on fruit trees, stumps or mature trees.",
            _ => ""
        };
    }

    private static string Points(double value) => (value * 100).ToString("0.##", CultureInfo.InvariantCulture);
    private static string Percent(double value) => Points(value) + "%";
}
