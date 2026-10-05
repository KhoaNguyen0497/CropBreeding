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
bool sawNew = false, sawUpgrade = false;
for (int seed = 0; seed < 1000; seed++) {
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
