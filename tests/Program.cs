using CropBreeding.Core;
static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
Check(TraitRules.Same("premium,hardy,premium", "hardy,premium"), "canonical stacks");
Check(TraitRules.Parse("unknown,premium").SequenceEqual(new[]{"premium"}), "unknown traits");
for (int seed = 0; seed < 1000; seed++) {
    var inherited = new[]{"premium", "hardy"};
    var next = TraitRules.Mutate(inherited, 3, 1, new Random(seed));
    Check(next.Length == 3 && inherited.All(next.Contains), "one unique mutation preserves parents");
    Check(TraitRules.Mutate(next, 2, 1, new Random(seed)).SequenceEqual(next), "lowered cap preserves existing traits");
    Check(TraitRules.Mutate(inherited, 3, 0, new Random(seed)).SequenceEqual(TraitRules.Parse("premium,hardy")), "zero chance");
    Check(TraitRules.Mutate(TraitRules.Known, 99, 1, new Random(seed)).Length == 4, "exhausted pool");
}
Check(TraitRules.Mutate(Array.Empty<string>(), 0, 1, new Random(1)).Length == 0, "zero cap");
Console.WriteLine("Passed trait normalization, inheritance, mutation, cap and exhausted-pool checks across 1,000 random seeds.");
