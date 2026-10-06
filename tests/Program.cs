using CropBreeding.Core;
static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
static string[] T(string value) => TraitRules.Parse(value);
static void Breed(string donor, string seed, int cap, string? expected)
{
    bool ok = TraitRules.TryBreed(T(donor), T(seed), cap, out var result);
    Check(ok == (expected != null), $"accept/reject {donor} + {seed}");
    if (ok) Check(TraitRules.Same(TraitRules.Encode(result), expected), $"output {donor} + {seed}");
}
Check(TraitRules.Same("high_yield,fast_regrowth,high_yield", "fast_regrowth:1,high_yield:1"), "legacy level-one save compatibility");
Check(!TraitRules.Same("high_yield", "high_yield:2"), "different levels never stack");
Check(TraitRules.Same("high_yield:2,high_yield:4", "high_yield:4"), "duplicate metadata keeps highest level");
Check(TraitRules.Encode(T("unknown,high_yield:0,fast_regrowth:garbage")) == "", "invalid metadata");
Check(TraitRules.Level(T("high_yield:99"), "high_yield") == 5, "loaded levels clamped");
Breed("fast_growth,high_yield", "fast_growth", 3, "fast_growth:2,high_yield");
Breed("fast_growth,high_yield", "fast_regrowth", 3, "fast_growth,high_yield,fast_regrowth");
Breed("fast_growth:4,high_yield", "fast_growth", 2, "fast_growth:5,high_yield");
Breed("fast_growth:5,high_yield", "fast_growth", 3, null);
Breed("fast_growth,high_yield", "fast_regrowth", 2, null);
Breed("fast_growth,high_yield,fast_regrowth", "fast_growth", 3, "fast_growth:2,high_yield,fast_regrowth");
Breed("fast_growth,high_yield", "fast_regrowth:2", 3, null);
Breed("fast_growth,high_yield", "fast_regrowth,high_yield", 3, null);
Breed("fast_growth:3,high_yield", "", 3, "fast_growth:3,high_yield");
Breed("fast_growth:3,high_yield", "", 1, "fast_growth:3,high_yield");
Breed("fast_growth:3,high_yield", "fast_growth", 1, null);
Breed("", "high_yield", 3, null);
Check(TraitRules.Parse("premium:5,hardy:2,fast_growth").SequenceEqual(T("fast_growth")), "removed traits ignored");
Check(TraitRules.RegrowthDays(10, 1, .1) == 9, "level one regrowth");
Check(TraitRules.RegrowthDays(10, 5, .1) == 5, "level five regrowth");
Check(TraitRules.RegrowthDays(7, 1, .1) == 7, "fraction rounds up");
Check(TraitRules.RegrowthDays(7, 5, .1) == 4, "fractional half rounds up");
Check(TraitRules.RegrowthDays(2, 5, .1) == 1, "coffee half regrowth");
Check(TraitRules.RegrowthDays(1, 5, 1) == 1, "minimum one day");
Check(TraitRules.RegrowthDays(-1, 5, .1) == -1, "non-regrowing stays non-regrowing");
Check(TraitRules.RegrowthDays(7, 0, .1) == 7, "no trait keeps normal regrowth");

Check(TraitRules.HarvestQuality(0, 1, .05, .049) == 1, "normal to silver");
Check(TraitRules.HarvestQuality(1, 1, .05, .049) == 2, "silver to gold");
Check(TraitRules.HarvestQuality(2, 1, .05, .049) == 4, "gold to iridium skips invalid quality 3");
Check(TraitRules.HarvestQuality(4, 5, .05, 0) == 4, "iridium stays iridium");
Check(TraitRules.HarvestQuality(0, 0, .05, 0) == 0, "new mutation without inherited level cannot upgrade current harvest");
Check(TraitRules.HarvestQuality(1, 2, .05, .12) == 1, "inherited level 2 cannot use mutated level 3 odds");
Check(TraitRules.HarvestQuality(1, 3, .05, .12) == 2, "replanted level 3 uses 15 percent");
Check(TraitRules.HarvestQuality(0, 5, .05, .249) == 1, "level five 25 percent success");
Check(TraitRules.HarvestQuality(0, 5, .05, .25) == 0, "level five threshold failure");
Check(TraitRules.HarvestQuality(0, 5, 1, 0) == 1, "cannot jump multiple tiers");
Check(new[]{.01,.9,.02,.8}.Select(r => TraitRules.HarvestQuality(2, 1, .05, r)).SequenceEqual(new[]{4,2,4,2}), "independent rolls can split identical base-quality harvests");
Breed("high_quality:2", "high_quality", 3, "high_quality:3");

Check(5 + TraitRules.CompanionDelay(7) == 9, "5-day main plus half 7-day companion rounds to 9");
Check(TraitRules.RegrowthDays(4, 0, .1, 7) == 8, "regrowth adds half companion base growth");
Check(TraitRules.RegrowthDays(4, 5, .1, 7) == 6, "speed applies to main regrowth only, round once");
Check(TraitRules.RegrowthDays(7, 1, .1, 7) == 10, "do not round intermediate shortened regrowth");
Check(TraitRules.RegrowthDays(-1, 5, .1, 7) == -1, "companion never creates regrowth");
Check(TraitRules.CompanionDelay(0) == 0, "unassigned companion no penalty");
Check(TraitRules.CompanionChoice("strawberry", "blueberry") == "strawberry", "donor companion wins");
Check(TraitRules.CompanionChoice(null, "blueberry") == "blueberry", "unassigned donor keeps seed choice");
Check(TraitRules.CompanionChoice(null, null) == null, "both unassigned stays unassigned");
Breed("companion:2", "companion", 3, "companion:3");
Breed("companion:5", "companion", 3, null);
Breed("fast_growth", "companion", 3, "fast_growth,companion");
Breed("high_quality,fast_growth,high_yield", "companion", 3, null);
bool sawNew = false, sawUpgrade = false;
for (int level = 0; level <= 5; level++)
    Check(TraitRules.EvergreenActive(level) == (level == 5), $"Evergreen activation level {level}");
Breed("evergreen:4", "evergreen", 3, "evergreen:5");
Breed("evergreen:5", "evergreen", 3, null);
Breed("evergreen:5", "", 3, "evergreen:5");
Check(TraitRules.Label("evergreen:4").Contains("dormant"), "dormant tooltip");
Check(TraitRules.Label("evergreen:5").Contains("all seasons"), "active tooltip");
Check(TraitRules.Mutate(T("evergreen:4"), 1, 1, new Random(0)).SequenceEqual(T("evergreen:5")), "Evergreen upgrades at trait cap");
for (int seed = 0; seed < 1000; seed++) {
    var annual = TraitRules.Mutate(T(""), 3, 1, new Random(seed), canRegrow: false);
    Check(TraitRules.Level(annual, "fast_regrowth") == 0, "single-harvest crops never gain Fast Regrowth");
    var annualFull = T("fast_growth:5,high_yield:5,high_quality:5,companion:5,evergreen:5");
    Check(TraitRules.Mutate(annualFull, 5, 1, new Random(seed), canRegrow: false).SequenceEqual(annualFull), "non-regrowing pool exhausted");
    var regrowing = TraitRules.Mutate(annualFull, 6, 1, new Random(seed), canRegrow: true);
    Check(TraitRules.Level(regrowing, "fast_regrowth") == 1, "regrowing crops can gain Fast Regrowth");
    var inherited = T("high_yield,fast_regrowth");
    var next = TraitRules.Mutate(inherited, 3, 1, new Random(seed));
    Check(next.Length <= 3, "count cap");
    Check(inherited.All(t => TraitRules.Level(next, TraitRules.Id(t)) >= TraitRules.Level(inherited, TraitRules.Id(t))), "never lose levels");
    Check(TraitRules.Known.Sum(id => TraitRules.Level(next,id) - TraitRules.Level(inherited,id)) == 1, "exactly one level gained per mutation");
    sawNew |= next.Length == 3; sawUpgrade |= next.Length == 2;
    var atCap = TraitRules.Mutate(T("high_yield:4,fast_regrowth:5,fast_growth:5"), 3, 1, new Random(seed));
    Check(TraitRules.Level(atCap,"high_yield") == 5 && atCap.Length == 3, "can upgrade at count cap");
    var full = T("high_yield:5,fast_regrowth:5,fast_growth:5");
    Check(TraitRules.Mutate(full, 3, 1, new Random(seed)).SequenceEqual(full), "maxed traits stay intact");
    Check(TraitRules.Mutate(inherited, 3, 0, new Random(seed)).SequenceEqual(inherited), "zero chance");
    Check(TraitRules.Mutate(inherited, 1, 1, new Random(seed)).Length == 2, "lowered cap never removes traits");
}
Check(sawNew && sawUpgrade, "mutations can add and upgrade");
Check(TraitRules.Mutate(Array.Empty<string>(), 0, 1, new Random(1)).Length == 0, "zero cap");
Console.WriteLine("Passed merge examples/rejections, legacy metadata, level-aware stacks and mutation invariants across 1,000 random seeds.");
