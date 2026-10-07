using CropBreeding.Core;
using HarmonyLib;
using StardewValley;

namespace CropBreeding;

internal static class ReviewFixTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    internal static void Run()
    {
        var throttle = new ErrorThrottle();
        Check(throttle.Next("harvest", "first", 0).Full, "first error logged fully");
        for (int i = 1; i < 100; i++) Check(throttle.Next("harvest", "first", i) == (false, 0), "hot-loop errors suppressed");
        Check(throttle.Next("harvest", "first", 10_000) == (false, 100), "summary includes all repeats and current occurrence");
        Check(throttle.Next("harvest", "second", 10_001).Full, "different failure is logged immediately");
        Check(throttle.Next("draw", "second", 10_001).Full, "independent actions have independent reporting");

        var harmony = new Harmony();
        PatchInstaller.Apply(harmony, typeof(ReviewFixTests), typeof(ReviewFixTests), nameof(Target), prefix: nameof(Existing));
        var original = typeof(ReviewFixTests).GetMethod(nameof(Existing), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        Check(harmony.Installed.Contains(original), "first patch installed");
        harmony.ThrowOnPatch = true;
        PatchInstaller.Apply(harmony, typeof(ReviewFixTests), typeof(ReviewFixTests), nameof(Target), transpiler: nameof(Failing));
        Check(harmony.Installed.Count == 1 && harmony.Installed.Contains(original), "failed addition does not erase existing prefix");

        foreach (int capacity in new[] { 12, 24, 36, 48, 60, 72, 96, 120, 241 })
        {
            var first = new MenuInventoryLayout(capacity, 0);
            var seen = new HashSet<int>();
            for (int page = 0; page < first.PageCount; page++)
            {
                var backpack = new MenuInventoryLayout(capacity, page);
                Check(backpack.End - backpack.Start <= 48, "only four rows drawn per page");
                Check(!backpack.IsVisible(backpack.Start - 1) && !backpack.IsVisible(backpack.End), "hidden slots cannot be hit");
                for (int i = backpack.Start; i < backpack.End; i++)
                    Check(seen.Add(i), "paging never duplicates an inventory index");
                foreach (var viewport in new[] { (1920, 1080), (1280, 800), (1280, 720), (1024, 640), (800, 500), (640, 480), (480, 360) })
                {
                    var g = new MenuGeometry(viewport.Item1, viewport.Item2, backpack.VisibleRows);
                    Check(g.Left >= 0 && g.Top >= 0 && g.Left + g.Width <= viewport.Item1 && g.Top + g.Height <= viewport.Item2, "menu fits viewport");
                    Check(g.Y(g.InventoryBottom + 52) < g.Top + g.Height, "footer fits below inventory");
                    for (int i = backpack.Start; i < backpack.End; i++)
                    {
                        int cell = i - backpack.Start;
                        int x = g.X(MenuGeometry.InventoryLeft + cell % 12 * MenuGeometry.SlotPitch);
                        int y = g.Y(MenuGeometry.InventoryTop + cell / 12 * MenuGeometry.SlotPitch);
                        Check(x >= g.X(28) && x + g.Size(64) <= g.X(980), "slot fits inside inventory panel horizontally");
                        Check(y >= g.Y(384) && y + g.Size(64) <= g.Y(g.InventoryBottom), "all backpack rows fit inside inventory panel");
                        Check(g.Y(324) < y, "status stays clear of inventory");
                    }
                }
            }
            Check(seen.SetEquals(Enumerable.Range(0, first.Capacity)), "all expanded backpack slots reachable exactly once");
            Check(new MenuInventoryLayout(capacity, -1).PageIndex == 0
                && new MenuInventoryLayout(capacity, 999).PageIndex == first.PageCount - 1, "paging clamps at both ends");
        }

        CheckLocks();
        CheckHarvestIsolation();
        CheckCompanionHarvestChance();
        JunimoHarvestTests.Run();
        Console.WriteLine("Passed repeated-error throttling, patch rollback isolation, small-viewport geometry, deferred station-lock cleanup and injected harvest-preparation failures. Uses test doubles; live controller/rendering remains untested.");
    }
    private static void Target() { }
    private static void Existing() { }
    private static void Failing() { }

    private static void CheckLocks()
    {
        Game1.IsMasterGame = true;
        StardewModdingAPI.Context.IsWorldReady = true;
        Game1.player = new Farmer();
        var loop = ModEntry.Instance.Helper.Events.GameLoop;
        var machine = new StardewValley.Object();
        var location = new GameLocation();
        var lease = new StationLock(machine, location);
        lease.RequestLock(() => { }, () => throw new Exception("unexpected busy lock"));
        foreach (var mutex in Game1.player.team.globalInventoryMutexes.Values) lease.ReleaseLock();
        Check(Game1.player.team.globalInventoryMutexes.Count == 1 && loop.Subscribers == 1, "release does not mutate team enumeration");
        loop.Tick();
        Check(Game1.player.team.globalInventoryMutexes.Count == 0 && loop.Subscribers == 0, "idle lock and one-shot update subscription removed");

        var first = new StationLock(machine, location);
        first.RequestLock(() => { }, () => { }); first.ReleaseLock();
        var reopened = new StationLock(machine, location);
        reopened.RequestLock(() => { }, () => { });
        loop.Tick();
        Check(reopened.IsLockHeld() && Game1.player.team.globalInventoryMutexes.Count == 1, "reopening before cleanup preserves active lock");
        reopened.ReleaseLock(); loop.Tick();
        Check(Game1.player.team.globalInventoryMutexes.Count == 0, "reopened lock eventually cleaned");
        for (int i = 0; i < 20; i++)
        {
            machine.TileLocation = new(i, 0);
            var moved = new StationLock(machine, location);
            moved.RequestLock(() => { }, () => { }); moved.ReleaseLock();
        }
        Check(loop.Subscribers == 1, "many releases share one cleanup callback");
        loop.Tick();
        Check(Game1.player.team.globalInventoryMutexes.Count == 0 && loop.Subscribers == 0, "moving station leaves no ticking locks");
    }

    private static void CheckHarvestIsolation()
    {
        ModEntry.Instance.Config = new ModConfig { MutationChance = 0, MaximumTraits = 15 };
        foreach (var material in TraitRules.MaterialDrops.Values) Game1.objectData[material.ItemId] = new();
        foreach (string failure in new[] { "copper", "seed", "companion", "quality" })
        {
            var plant = new Crop { Dirt = new HoeDirt(), Data = new CropData() };
            plant.Dirt.crop = plant;
            plant.currentPhase.Value = plant.phaseDays.Count - 1; plant.dayOfCurrentPhase.Value = 0;
            Traits.Write(plant.modData, ["copper_bearing:5", "iron_bearing:5", "high_yield:5", "high_quality:1", "seed_saver:1", "companion:1"]);
            plant.modData[Companion.Key] = "gold_carrot";
            string expected = TraitRules.Encode(Traits.Read(plant.modData).Append("fast_growth:1"));
            plant.modData[MutationState.Key] = expected;
            Companion.ThrowBaseDays = failure == "companion";
            Traits.SaltRandomFactory = salt => failure == "quality" && salt == 37 ? throw new Exception("injected quality preparation") : new ZeroRandom();
            int failures = 0;
            ItemRegistry.Factory = (id, count, quality) =>
            {
                if ((failure == "copper" && id == "(O)334") || (failure == "seed" && id == "(O)472"))
                { failures++; throw new Exception("injected item creation " + id); }
                return new Item { ItemId = CropCatalog.Raw(id), Stack = count, Quality = quality };
            };
            try
            {
                var context = new HarvestContext(plant);
                Check(TraitRules.Encode(context.OutputTraits) == expected, "bonus failure preserves stored mutation");
                Check(context.PendingExtras.Any(i => i.ItemId == "335"), "independent iron bonus survives");
                Check(context.PendingExtras.Any(i => i.ItemId == "472") == (failure != "seed"), "Seed Saver isolated from unrelated failures");
                Check(context.PendingExtras.Any(i => i.ItemId == "gold_carrot") == (failure != "companion"), "Companion isolated from unrelated failures");
                HarvestContext.Current = context;
                Item crop = HarvestContext.CloneHarvest(new Item());
                Check(crop.modData[Traits.Key] == expected, "primary output still receives inherited and mutated traits");
                Check(crop.Quality == (failure == "quality" ? 0 : 1), "High Quality uses vanilla fallback only on its own failure");
                context.CompleteYield();
                Check(context.PendingExtras.Count(i => i.ItemId == "24") == 1, "High Yield survives all preparation failures");
                Check(failures == (failure is "copper" or "seed" ? 1 : 0), "failed item creation isn't retried");
            }
            finally
            {
                HarvestContext.Current = null; ItemRegistry.Factory = null; Traits.SaltRandomFactory = null; Companion.ThrowBaseDays = false;
            }
        }
    }
    private static void CheckCompanionHarvestChance()
    {
        // Exercise the real harvest path with altered planted phases/countdowns and saved mutation.
        foreach (var sample in new[] { (Growth: 10, Regrow: 2, Chance: .20), (Growth: 28, Regrow: 7, Chance: .70),
            (Growth: 4, Regrow: -1, Chance: 4 / 7.0), (Growth: 7, Regrow: -1, Chance: 1.0) })
        foreach (bool repeated in new[] { false, true })
        foreach (bool succeeds in new[] { false, true })
        {
            var plant = new Crop { Dirt = new HoeDirt(), Data = new CropData { DaysInPhase = [sample.Growth], RegrowDays = sample.Regrow } };
            plant.Dirt.crop = plant;
            plant.phaseDays = [1, 99999]; // Speed and other adjustments must not affect the probability.
            plant.currentPhase.Value = 1; plant.dayOfCurrentPhase.Value = 0;
            plant.fullyGrown.Value = repeated && sample.Regrow > 0;
            Traits.Write(plant.modData, ["companion:5", "fast_growth:5"]);
            plant.modData[Companion.Key] = "gold_carrot";
            plant.modData[MutationState.Key] = "companion:5,fast_growth:5,high_yield:1";
            double roll = succeeds ? sample.Chance - .000001 : sample.Chance;
            Traits.SaltRandomFactory = salt => new FixedCompanionRandom(salt == 53 ? roll : .999999);
            try
            {
                var context = new HarvestContext(plant);
                var extras = context.PendingExtras.Where(item => item.ItemId == "gold_carrot").ToArray();
                Check(extras.Length == (succeeds ? 1 : 0), "Companion harvest uses main crop base time on first and repeat harvests");
                if (succeeds) Check(extras[0].Stack == 1 && extras[0].Quality == 0 && extras[0].modData.Count == 0,
                    "Companion remains one plain item without inherited or newly mutated traits");
            }
            finally { Traits.SaltRandomFactory = null; }
        }
        Console.WriteLine("Passed Companion base growth/regrowth chances through the harvest path, including modified phases and first/repeat harvests. Uses test doubles.");
    }
    private sealed class FixedCompanionRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
    private sealed class ZeroRandom : Random
    {
        public override double NextDouble() => 0;
        public override int Next(int maxValue) => 0;
    }
}
