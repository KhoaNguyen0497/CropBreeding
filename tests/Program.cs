using CropBreeding.Core;
ResearchRulesTests.Run();
static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
// Compact test fixtures expand to the single explicit trait:level storage format.
static string[] T(string value) => TraitRules.Parse(string.Join(',', value.Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(token => token.Contains(':') ? token : token + ":1")));
static void Breed(string donor, string seed, int cap, string? expected)
{
    bool ok = TraitRules.TryBreed(T(donor), T(seed), cap, out var result);
    Check(ok == (expected != null), $"accept/reject {donor} + {seed}");
    if (ok) Check(result.SequenceEqual(T(expected!)), $"output {donor} + {seed}");
}
Check(TraitRules.Same("high_yield:1,evergreen:1,high_yield:1", "evergreen:1,high_yield:1"), "trait order and duplicate normalization");
Check(!TraitRules.Same("high_yield:1", "high_yield:2"), "different levels never stack");
Check(TraitRules.Encode(T("high_yield")) == "high_yield:1", "level one is explicitly encoded");
Check(TraitRules.Parse("high_yield").Length == 0, "missing level is invalid metadata");
Check(TraitRules.Same("high_yield:2,high_yield:4", "high_yield:4"), "duplicate metadata keeps highest level");
Check(TraitRules.Encode(T("unknown,high_yield:0,evergreen:garbage")) == "", "invalid metadata");
Check(TraitRules.Level(T("high_yield:99"), "high_yield") == 5, "loaded levels clamped");
Breed("fast_growth,high_yield", "fast_growth", 3, "fast_growth:2,high_yield");
Breed("fast_growth,high_yield", "evergreen", 3, "fast_growth,high_yield,evergreen");
Breed("fast_growth:4,high_yield", "fast_growth", 2, "fast_growth:5,high_yield");
Breed("fast_growth:5,high_yield", "fast_growth", 3, "fast_growth:5,high_yield");
Breed("fast_growth,high_yield", "evergreen", 2, null);
Breed("fast_growth,high_yield,evergreen", "fast_growth", 3, "fast_growth:2,high_yield,evergreen");
Breed("fast_growth,high_yield", "evergreen:2", 3, "fast_growth,high_yield,evergreen:2");
Breed("fast_growth,high_yield", "evergreen,high_yield", 3, "fast_growth,high_yield:2,evergreen");
Breed("fast_growth:3,high_yield", "", 3, "fast_growth:3,high_yield");
Breed("fast_growth:3,high_yield", "", 1, null);
Breed("fast_growth:3,high_yield", "fast_growth", 1, null);
Breed("", "high_yield", 3, null);
Breed("", "", 3, null);
Breed("", "fast_growth:5,high_yield:5,evergreen:5", 3, null);
Breed("fast_growth", "", 0, null);
Breed("fast_growth", "", -1, null);
Breed("fast_growth:3", "fast_growth:4", 3, "fast_growth:5");
Breed("fast_growth:2,high_yield", "fast_growth,evergreen:3", 3, "fast_growth:3,high_yield,evergreen:3");
Breed("fast_growth:5,high_yield:2", "high_yield:4,evergreen:5", 3, "fast_growth:5,high_yield:5,evergreen:5");
Breed("fast_growth,high_yield", "evergreen,companion", 3, null);
Breed("fast_growth:5,high_yield:5,evergreen:5", "", 3, "fast_growth:5,high_yield:5,evergreen:5");
foreach (string id in TraitRules.Known)
    for (int a = 1; a <= TraitRules.MaxLevel(id); a++)
        for (int b = 1; b <= TraitRules.MaxLevel(id); b++)
            Breed($"{id}:{a}", $"{id}:{b}", 1, $"{id}:{Math.Min(a + b, TraitRules.MaxLevel(id))}");
var mergeDonor = T("high_yield:3,companion:2");
var mergeSeed = T("high_yield:4,companion:4,evergreen:3");
var donorBefore = mergeDonor.ToArray(); var seedBefore = mergeSeed.ToArray();
Check(TraitRules.TryBreed(mergeDonor, mergeSeed, 3, out var merged)
    && TraitRules.TryBreed(mergeSeed, mergeDonor, 3, out var reversed)
    && merged.SequenceEqual(reversed), "trait combination independent of direction");
Check(mergeDonor.SequenceEqual(donorBefore) && mergeSeed.SequenceEqual(seedBefore), "merge leaves input trait arrays unchanged");
Check(TraitRules.Parse("unknown:5,fast_growth:1").SequenceEqual(T("fast_growth")), "unknown traits ignored");
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
Check(TraitRules.RegrowthDays(4, 5, .05, 7) == 6, "speed applies after companion, round once");
Check(TraitRules.RegrowthDays(7, 1, .05, 7) == 10, "do not round intermediate shortened regrowth");
Check(TraitRules.RegrowthDays(-1, 5, .1, 7) == -1, "companion never creates regrowth");
Check(TraitRules.CompanionDelay(0) == 0, "unassigned companion no penalty");
foreach (int level in Enumerable.Range(1, 5))
{
    Check(Math.Abs(TraitRules.CompanionOutputChance(level, 10, 2) - .04 * level) < 1e-12, "coffee uses 2-day regrowth at every level");
    Check(Math.Abs(TraitRules.CompanionOutputChance(level, 28, 7) - .14 * level) < 1e-12, "ancient fruit uses 7-day regrowth");
    Check(Math.Abs(TraitRules.CompanionOutputChance(level, 4, -1) - .2 * level * 4 / 7) < 1e-12, "parsnip scales initial growth");
    Check(Math.Abs(TraitRules.CompanionOutputChance(level, 6, -1) - .2 * level * 6 / 7) < 1e-12, "six-day single-harvest crop keeps fractional chance");
    Check(Math.Abs(TraitRules.CompanionOutputChance(level, 7, -1) - .2 * level) < 1e-12, "single-harvest threshold is seven days");
    Check(Math.Abs(TraitRules.CompanionOutputChance(level, 1, 10) - .2 * level) < 1e-12, "regrowth threshold is ten days");
}
Check(TraitRules.CompanionOutputChance(5, 13, -1) == 1, "long single harvest capped at full chance");
Check(TraitRules.CompanionOutputChance(5, 28, 20) == 1, "long regrowth capped at full chance");
Check(TraitRules.CompanionOutputChance(0, 28, 7) == 0, "no inherited Companion no output");
Check(TraitRules.CompanionOutputChance(5, 0, -1) == 0, "zero growth gives zero chance");
Check(TraitRules.CompanionOutputChance(5, -1, 0) == 0, "invalid growth safely gives zero chance");
Check(TraitRules.CompanionChoice("strawberry", "blueberry") == "strawberry", "donor companion wins");
Check(TraitRules.CompanionChoice(null, "blueberry") == "blueberry", "unassigned donor keeps seed choice");
Check(TraitRules.CompanionChoice(null, null) == null, "both unassigned stays unassigned");
Breed("companion:2", "companion", 3, "companion:3");
Breed("companion:5", "companion", 3, "companion:5");
Breed("fast_growth", "companion", 3, "fast_growth,companion");
Breed("high_quality,fast_growth,high_yield", "companion", 3, null);
bool sawNew = false, sawUpgrade = false;
Check(TraitRules.ExtraYieldCount(1, 1, .2, .19) == 1, "one crop fractional success");
Check(TraitRules.ExtraYieldCount(1, 1, .2, .2) == 0, "one crop fractional failure");
Check(TraitRules.ExtraYieldCount(4, 1, .2, .79) == 1, "four crops 80 percent extra");
Check(TraitRules.ExtraYieldCount(4, 1, .2, .8) == 0, "four crops fractional failure");
Check(TraitRules.ExtraYieldCount(4, 2, .2, .59) == 2, "guaranteed plus fractional extra");
Check(TraitRules.ExtraYieldCount(4, 2, .2, .61) == 1, "guaranteed extra survives failed fraction");
Check(TraitRules.ExtraYieldCount(5, 1, .2, .99) == 1, "integer bonus always granted");
Check(TraitRules.ExtraYieldCount(4, 5, .2, .99) == 4, "level five doubles");
Check(TraitRules.ExtraYieldCount(4, 0, .2, 0) == 0, "no inherited level no bonus");
int[] previewBase = [1, 2, 2, 99999];
Check(TraitRules.PreviewGrowthPhases(previewBase, 0, .1, false, 0).SequenceEqual(previewBase), "plain preview unchanged");
Check(TraitRules.PreviewGrowthPhases(previewBase, 0, .1, false, 7).SequenceEqual(new[] { 1, 2, 6, 99999 }), "preview companion rounds up");
Check(TraitRules.PreviewGrowthPhases(previewBase, 2, .05, false, 7).SequenceEqual(new[] { 1, 1, 6, 99999 }), "preview speed after companion");
Check(TraitRules.PreviewGrowthPhases(previewBase, 0, .1, true, 0).SequenceEqual(new[] { 1, 1, 2, 99999 }), "preview Agriculturist vanilla rounding");
Check(previewBase.SequenceEqual(new[] { 1, 2, 2, 99999 }), "preview never mutates source phases");
for (int level = 0; level <= 5; level++)
    Check(TraitRules.EvergreenActive(level) == (level == 5), $"Evergreen activation level {level}");
Breed("evergreen:4", "evergreen", 3, "evergreen:5");
Breed("evergreen:5", "evergreen", 3, "evergreen:5");
Breed("evergreen:5", "", 3, "evergreen:5");
Check(TraitRules.Label("evergreen:4") == "Evergreen 4", "level four tooltip");
Check(TraitRules.Label("evergreen:5") == "Evergreen 5", "level five tooltip");
Check(TraitRules.Mutate(T("evergreen:4"), 1, 1, new Random(0), isAvailable: id => id == "evergreen").SequenceEqual(T("evergreen:5")), "selected Evergreen upgrades at trait cap");
for (int seed = 0; seed < 1000; seed++) {
    var annual = TraitRules.Mutate(T(""), 3, 1, new Random(seed), canRegrow: false);
    var annualFull = T("fast_growth:5,high_yield:5,high_quality:5,companion:5,evergreen:5");
    Check(TraitRules.Mutate(annualFull, 5, 1, new Random(seed), canRegrow: false).SequenceEqual(annualFull), "non-regrowing pool exhausted");
    var inherited = T("high_yield,evergreen");
    var next = TraitRules.Mutate(inherited, 3, 1, new Random(seed));
    Check(next.Length <= 3, "count cap");
    Check(inherited.All(t => TraitRules.Level(next, TraitRules.Id(t)) >= TraitRules.Level(inherited, TraitRules.Id(t))), "never lose levels");
    Check(TraitRules.Known.Sum(id => TraitRules.Level(next,id) - TraitRules.Level(inherited,id)) == 1, "exactly one level gained per mutation");
    sawNew |= next.Length == 3; sawUpgrade |= next.Length == 2;
    var atCap = TraitRules.Mutate(T("high_yield:4,evergreen:5,fast_growth:5"), 3, 1, new Random(seed));
    Check(TraitRules.Level(atCap,"high_yield") is 4 or 5 && atCap.Length == 3, "count cap permits selected upgrade or unchanged result");
    var full = T("high_yield:5,evergreen:5,fast_growth:5");
    Check(TraitRules.Mutate(full, 3, 1, new Random(seed)).SequenceEqual(full), "maxed traits stay intact");
    Check(TraitRules.Mutate(inherited, 3, 0, new Random(seed)).SequenceEqual(inherited), "zero chance");
    Check(TraitRules.Mutate(inherited, 1, 1, new Random(seed)).Length == 2, "lowered cap never removes traits");
}
Check(sawNew && sawUpgrade, "mutations can add and upgrade");
Check(TraitRules.Mutate(Array.Empty<string>(), 0, 1, new Random(1)).Length == 0, "zero cap");
Console.WriteLine("Passed merge examples/rejections, explicit trait levels, level-aware stacks and mutation invariants across 1,000 random seeds.");

Check(TraitRules.RegrowthDays(10, 5, .05, 8) == 11, "final reduction covers companion");
Check(TraitRules.FinalGrowthPhases(new[] { 1, 2, 2, 99999 }, 5, .05, 7).Take(3).Sum() == 7, "initial growth rounds only after combined reduction");
Check(TraitRules.Level(T("researcher:5"), "researcher") == 1, "Researcher has one level");
Check(TraitRules.Label("researcher:1") == "Researcher", "single-level Researcher label");
Breed("researcher", "researcher", 3, "researcher");
Breed("fast_growth:3", "researcher", 3, "fast_growth:3,researcher");
Check(TraitRules.Mutate(T("researcher"), 3, 1, new Random(1), true, id => id == "researcher").SequenceEqual(T("researcher")), "harvest cannot upgrade Researcher");
Breed("seed_saver:4", "seed_saver", 3, "seed_saver:5");
Breed("seed_saver:5", "", 3, "seed_saver:5");
Check(TraitRules.Without(T("researcher:5,high_yield:3,companion:2"), "researcher").SequenceEqual(T("high_yield:3,companion:2")), "remove selected trait preserves other levels");
Check(TraitRules.Without(T("researcher:5"), "researcher").Length == 0, "removing last trait gives plain seed");
Check(TraitRules.Without(T("high_yield:3"), "researcher").SequenceEqual(T("high_yield:3")), "absent removal preserves traits");
Check(TraitRules.MaterialDropCount(4, 5, 0) == 0, "under five base days gives no bars");
Check(TraitRules.MaterialDropCount(5, 1, .049) == 1, "first five-day block 5 percent");
Check(TraitRules.MaterialDropCount(9, 1, .05) == 0, "partial block ignored and threshold fails");
Check(TraitRules.MaterialDropCount(28, 1, .249) == 1, "28 days level one 25 percent success");
Check(TraitRules.MaterialDropCount(28, 1, .25) == 0, "28 days level one threshold failure");
Check(TraitRules.MaterialDropCount(28, 4, .999) == 1, "100 percent guarantees one");
Check(TraitRules.MaterialDropCount(28, 5, .249) == 2, "125 percent overflow success");
Check(TraitRules.MaterialDropCount(28, 5, .25) == 1, "125 percent overflow failure still grants one");
Check(TraitRules.MaterialDropCount(40, 5, .999) == 2, "200 percent guarantees two");
Check(TraitRules.MaterialDropCount(45, 5, .249) == 3, "225 percent can grant three");
Check(TraitRules.MaterialDropCount(28, 0, 0) == 0, "newly mutated material trait has no inherited benefit");
Breed("copper_bearing:4", "copper_bearing", 3, "copper_bearing:5");
Breed("iron_bearing:2", "gold_bearing", 3, "iron_bearing:2,gold_bearing");
Check(TraitRules.RootedTriggers(1, .1, false, .099), "Rooted one 10 percent success");
Check(!TraitRules.RootedTriggers(1, .1, false, .1), "Rooted threshold failure");
Check(TraitRules.RootedTriggers(5, .1, false, .499), "Rooted five 50 percent success");
Check(!TraitRules.RootedTriggers(5, .1, false, .5), "Rooted five threshold failure");
Check(!TraitRules.RootedTriggers(5, .1, true, 0), "natural regrowers cannot restart");
Check(!TraitRules.RootedTriggers(0, .1, false, 0), "new mutation cannot restart parent");
Breed("rooted:2", "rooted", 3, "rooted:3");
bool sawRooted = false;
for (int i = 0; i < 1000; i++)
{
    Check(TraitRules.Level(TraitRules.Mutate([], 3, 1, new Random(i), true), "rooted") == 0, "regrowers cannot mutate Rooted");
    sawRooted |= TraitRules.Level(TraitRules.Mutate([], 3, 1, new Random(i), false), "rooted") == 1;
}
Check(sawRooted, "annual crops can mutate Rooted");

for (int level = 0; level <= 5; level++)
{
    int total = 0;
    for (int i = 0; i < 1000; i++)
    {
        double roll = (i + .5) / 1000;
        total += TraitRules.NurseCropStages(level, false, roll);
        Check(TraitRules.NurseCropStages(level, true, roll) == 0, "Nurse Crop never affects regrowers");
    }
    Check(total == level * 300, $"Nurse Crop level {level} distribution including overflow");
}
Check(TraitRules.NurseCropStages(1, false, .3) == 0, "Nurse Crop threshold excludes equal roll");
Check(TraitRules.NurseCropStages(4, false, .2) == 1, "Nurse Crop overflow threshold retains guaranteed stage");
Check(TraitRules.NurseCropStages(5, false, .5) == 1, "Nurse Crop level five threshold");
for (int stage = 0; stage <= 15; stage++)
    for (int extra = 0; extra <= 2; extra++)
    {
        int next = TraitRules.AdvanceImmatureTree(stage, extra, 5);
        Check(next >= stage, "Nurse Crop never shrinks existing trees");
        Check(stage >= 4 ? next == stage : next == Math.Min(4, stage + extra), "Nurse Crop leaves final maturity step");
    }
Check(TraitRules.AdvanceImmatureTree(0, 2, 5) == 2, "Nurse Crop can advance planted tree seeds");
Check(TraitRules.AdvanceImmatureTree(3, 2, 5) == 4, "Nurse Crop discards excess stages near maturity");
Breed("nurse_crop:4", "nurse_crop", 3, "nurse_crop:5");
Breed("nurse_crop:5", "nurse_crop", 3, "nurse_crop:5");
Check(TraitRules.Label("nurse_crop:3") == "Nurse Crop 3", "Nurse Crop label");
bool sawNurseCrop = false;
for (int i = 0; i < 1000; i++)
{
    Check(TraitRules.Level(TraitRules.Mutate([], 3, 1, new Random(i), true), "nurse_crop") == 0, "regrowers cannot acquire Nurse Crop");
    Check(TraitRules.Mutate(T("nurse_crop:2"), 1, 1, new Random(i), true).SequenceEqual(T("nurse_crop:2")), "regrowers cannot upgrade existing Nurse Crop");
    sawNurseCrop |= TraitRules.Level(TraitRules.Mutate([], 3, 1, new Random(i), false), "nurse_crop") == 1;
}
Check(sawNurseCrop, "annual crops can acquire Nurse Crop");
Console.WriteLine("Passed Nurse Crop overflow distribution, maturity cap, breeding and mutation eligibility checks.");

Check(TraitRules.MaterialDrops.Values.Select(v => v.Salt).Distinct().Count() == TraitRules.MaterialDrops.Count,
    "material traits have independent random salts");
Check(!TraitRules.MaterialDrops.Values.Any(v => v.ItemId is "92" or "MysticSyrup"), "Sap and Mystic Syrup excluded");
foreach (string id in TraitRules.MaterialDrops.Keys)
{
    Check(TraitRules.Known.Contains(id), "material traits are recognized");
    Breed(id + ":4", id, 3, id + ":5");
    Breed(id + ":5", id, 3, id + ":5");
    Check(TraitRules.Without(T(id), id).Length == 0, "material trait removable");
    foreach (bool regrows in new[] { false, true })
    {
        Check(TraitRules.Mutate([], 3, 1, new Random(1), regrows, available => available == id).SequenceEqual(T(id)),
            "each material can mutate on annuals and regrowers");
        Check(TraitRules.Mutate(T(id + ":2"), 1, 1, new Random(1), regrows, _ => false).SequenceEqual(T(id + ":2")),
            "missing output disables upgrades without deleting inherited trait");
    }
}
Console.WriteLine("Passed material trait availability, inheritance, annual/regrowing mutation and breeding checks.");

foreach (bool regrows in new[] { false, true })
{
    string[] pool = TraitRules.Known.Where(id => !regrows || id is not ("rooted" or "nurse_crop")).ToArray();
    string[] capped = T("high_yield:4,evergreen:5,fast_growth:5");
    for (int index = 0; index < pool.Length; index++)
    {
        var roll = new SelectedTraitRandom(index);
        var result = TraitRules.Mutate(capped, 3, 1, roll, regrows);
        var expected = pool[index] == "high_yield" ? T("high_yield:5,evergreen:5,fast_growth:5") : capped;
        Check(result.SequenceEqual(expected), "only selecting the upgradeable trait changes a capped plant");
        Check(roll.PoolSize == pool.Length && roll.SelectionCalls == 1, "full eligible pool, exactly one pick, no reroll");
        var emptyRoll = new SelectedTraitRandom(index);
        Check(TraitRules.Mutate([], 3, 1, emptyRoll, regrows).SequenceEqual(T(pool[index])), "every eligible trait selectable on an empty plant");
        Check(emptyRoll.PoolSize == roll.PoolSize, "trait ownership and levels do not change selection pool");
    }
}
var failedRoll = new SelectedTraitRandom(0, .05);
Check(TraitRules.Mutate([], 3, .05, failedRoll).Length == 0 && failedRoll.SelectionCalls == 0, "failed base roll never picks a trait");
var availableRoll = new SelectedTraitRandom(0);
Check(TraitRules.Mutate([], 3, 1, availableRoll, false, id => id == "evergreen").SequenceEqual(T("evergreen"))
    && availableRoll.PoolSize == 1, "unavailable traits excluded before selection");
Console.WriteLine("Passed full-pool mutation selection, wasted capped/maxed picks, no rerolls and intrinsic eligibility checks.");

sealed class SelectedTraitRandom(int index, double chanceRoll = 0) : Random
{
    public int PoolSize { get; private set; }
    public int SelectionCalls { get; private set; }
    public override double NextDouble() => chanceRoll;
    public override int Next(int maxValue)
    {
        PoolSize = maxValue;
        SelectionCalls++;
        if (index < 0 || index >= maxValue) throw new InvalidOperationException("Unexpected mutation pool size.");
        return index;
    }
}
