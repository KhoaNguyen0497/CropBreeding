using CropBreeding.Core;

internal static class ResearchRulesTests
{
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
    internal static void Run()
    {
        for (int seed = 0; seed < 1000; seed++)
        foreach (bool regrows in new[] { false, true })
        foreach (int cap in new[] { 1, 2, 3 })
        {
            var input = cap == 1 ? new[] { "researcher:1" } : new[] { "researcher:1", "fast_growth:4" };
            Check(ResearchRules.TryResearch(input, cap, regrows, new Random(seed), out var result), "valid research always succeeds");
            Check(TraitRules.Level(result, "researcher") == 0 && result.Length <= cap, "Researcher removed and trait cap respected");
            int before = input.Where(t => TraitRules.Id(t) != "researcher").Sum(t => TraitRules.Level([t], TraitRules.Id(t)));
            int after = result.Sum(t => TraitRules.Level([t], TraitRules.Id(t)));
            Check(after == before + 2, "exactly two successful increases");
            Check(result.All(t => TraitRules.Level([t], TraitRules.Id(t)) <= 5), "level cap respected");
            Check(!regrows || !result.Any(t => TraitRules.Id(t) is "rooted" or "nurse_crop"), "regrowing eligibility respected");
        }
        Check(ResearchRules.TryResearch(["researcher:1"], 3, true, new Random(1), out var doubled, id => id == "fast_growth")
            && doubled.SequenceEqual(new[] { "fast_growth:2" }), "same trait twice gives level two");
        Check(ResearchRules.TryResearch(["researcher:1", "fast_growth:3"], 1, true, new Random(1), out var upgraded, id => id == "fast_growth")
            && upgraded.SequenceEqual(new[] { "fast_growth:5" }), "two upgrades work at trait cap after removing Researcher");
        Check(!ResearchRules.CanResearch(["researcher:1", "fast_growth:4"], 1, true, id => id == "fast_growth"), "only one available increase rejects input");
        Check(!ResearchRules.CanResearch(["researcher:1"], 0, true), "zero trait cap rejects input");
        Check(!ResearchRules.CanResearch(["fast_growth:3"], 3, true), "Researcher required");
        Check(!ResearchRules.CanResearch(["researcher:1"], 3, true, _ => false), "empty eligible pool rejects input");
        Check(ResearchRules.TryResearch(["researcher:1", "fast_growth:5", "high_yield:5"], 3, true, new Random(7), out var capped)
            && TraitRules.Level(capped, "fast_growth") == 5 && TraitRules.Level(capped, "high_yield") == 5, "maxed inherited traits preserved and never selected");
        Check(ResearchRules.TryResearch(["researcher:1", "fast_growth:4", "high_yield:5"], 3, true, new FirstChoiceRandom(), out var refiltered,
            id => id is "fast_growth" or "high_yield" or "high_quality")
            && TraitRules.Level(refiltered, "fast_growth") == 5 && TraitRules.Level(refiltered, "high_yield") == 5
            && TraitRules.Level(refiltered, "high_quality") == 1,
            "R X4 Y5 becomes X5 Y5 Z1: second pick cannot waste an upgrade on newly maxed X");
        Console.WriteLine("Passed guaranteed research rolls, same-trait upgrades, inherited traits, intrinsic eligibility and cap rejection across 6,000 cases.");
    }
    private sealed class FirstChoiceRandom : Random
    {
        public override int Next(int maximum) => 0;
    }
}
