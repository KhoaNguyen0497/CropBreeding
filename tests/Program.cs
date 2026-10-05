using CropBreeding.Core;
static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
static string[] T(string value) => TraitRules.Parse(value);
static void Breed(string donor, string seed, int cap, string? expected)
{
    bool ok = TraitRules.TryBreed(T(donor), T(seed), cap, out var result);
    Check(ok == (expected != null), $"accept/reject {donor} + {seed}");
    if (ok) Check(TraitRules.Same(TraitRules.Encode(result), expected), $"output {donor} + {seed}");
}
Check(TraitRules.Same("premium,hardy,premium", "hardy:1,premium:1"), "legacy level-one save compatibility");
Check(!TraitRules.Same("premium", "premium:2"), "different levels never stack");
Check(TraitRules.Same("premium:2,premium:4", "premium:4"), "duplicate metadata keeps highest level");
Check(TraitRules.Encode(T("unknown,premium:0,hardy:garbage")) == "", "invalid metadata");
Check(TraitRules.Level(T("premium:99"), "premium") == 5, "loaded levels clamped");
Breed("fast_growth,premium", "fast_growth", 3, "fast_growth:2,premium");
Breed("fast_growth,premium", "hardy", 3, "fast_growth,premium,hardy");
Breed("fast_growth:4,premium", "fast_growth", 2, "fast_growth:5,premium");
Breed("fast_growth:5,premium", "fast_growth", 3, null);
Breed("fast_growth,premium,hardy", "high_yield", 3, null);
Breed("fast_growth,premium,hardy", "fast_growth", 3, "fast_growth:2,premium,hardy");
Breed("fast_growth,premium", "hardy:2", 3, null);
Breed("fast_growth,premium", "hardy,high_yield", 3, null);
Breed("fast_growth:3,premium", "", 3, "fast_growth:3,premium");
Breed("fast_growth:3,premium", "", 1, "fast_growth:3,premium");
Breed("fast_growth:3,premium", "fast_growth", 1, null);
Breed("", "premium", 3, null);
bool sawNew = false, sawUpgrade = false;
for (int seed = 0; seed < 1000; seed++) {
    var inherited = T("premium,hardy");
    var next = TraitRules.Mutate(inherited, 3, 1, new Random(seed));
    Check(next.Length <= 3, "count cap");
    Check(inherited.All(t => TraitRules.Level(next, TraitRules.Id(t)) >= TraitRules.Level(inherited, TraitRules.Id(t))), "never lose levels");
    Check(TraitRules.Known.Sum(id => TraitRules.Level(next,id) - TraitRules.Level(inherited,id)) == 1, "exactly one level gained per mutation");
    sawNew |= next.Length == 3; sawUpgrade |= next.Length == 2;
    var atCap = TraitRules.Mutate(T("premium:4,hardy:5,high_yield:5"), 3, 1, new Random(seed));
    Check(TraitRules.Level(atCap,"premium") == 5 && atCap.Length == 3, "can upgrade at count cap");
    var full = T("premium:5,hardy:5,high_yield:5");
    Check(TraitRules.Mutate(full, 3, 1, new Random(seed)).SequenceEqual(full), "maxed traits stay intact");
    Check(TraitRules.Mutate(inherited, 3, 0, new Random(seed)).SequenceEqual(inherited), "zero chance");
    Check(TraitRules.Mutate(inherited, 1, 1, new Random(seed)).Length == 2, "lowered cap never removes traits");
}
Check(sawNew && sawUpgrade, "mutations can add and upgrade");
Check(TraitRules.Mutate(Array.Empty<string>(), 0, 1, new Random(1)).Length == 0, "zero cap");
Console.WriteLine("Passed merge examples/rejections, legacy metadata, level-aware stacks and mutation invariants across 1,000 random seeds.");
