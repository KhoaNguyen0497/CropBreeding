using CropBreeding.Integrations;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;

namespace CropBreeding;

internal static class BetterJunimosFertilizerTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    internal static void Run()
    {
        var registry = ModEntry.Instance.Helper.ModRegistry;
        var harmony = new Harmony();
        BetterJunimosFertilizerIntegration.Register(harmony);
        Check(harmony.Installed.Count == 0, "absent Better Junimos needs no fertilizer patch");
        registry.Loaded.Add("hawkfalcon.BetterJunimos");
        AccessTools.NamedType = typeof(Ability);
        BetterJunimosFertilizerIntegration.Register(harmony);
        Check(harmony.Installed.Count == 3, "recognized fertilizer contracts patched");
        var unrelated = typeof(BetterJunimosFertilizerTests).GetMethod(nameof(Unrelated))!;
        var broken = new Harmony { ThrowOnPatch = true };
        broken.Installed.Add(unrelated);
        BetterJunimosFertilizerIntegration.Register(broken);
        Check(broken.Installed.Count == 1 && broken.Installed.Contains(unrelated), "failed fertilizer registration preserves other patches");
        AccessTools.NamedType = typeof(object);
        var unsupported = new Harmony();
        BetterJunimosFertilizerIntegration.Register(unsupported);
        Check(unsupported.Installed.Count == 0, "unsupported fertilizer signature installs nothing");
        registry.Loaded.Clear(); AccessTools.NamedType = null;

        var location = new GameLocation();
        Vector2 tile = new(2, 3);
        var soil = new HoeDirt { Tile = tile };
        location.terrainFeatures[tile] = soil;
        foreach (int? phase in new int?[] { null, 0, 1, 2, 4 })
        {
            soil.crop = phase == null ? null : new Crop { Dirt = soil };
            if (phase != null) soil.crop!.currentPhase.Value = phase.Value;
            bool allowed = phase == null || phase == 0;
            bool available = true;
            BetterJunimosFertilizerIntegration.Available(location, tile, ref available);
            Check(available == allowed, "only empty soil and phase zero are offered");
            available = false;
            BetterJunimosFertilizerIntegration.Available(location, tile, ref available);
            Check(!available, "never override Better Junimos' other rejection rules");
            bool result = true;
            bool executeOriginal = BetterJunimosFertilizerIntegration.Perform(location, tile, ref result);
            Check(executeOriginal == allowed && (allowed || !result), "advanced phase action blocked before consumption");
        }
        soil.crop = new Crop { Dirt = soil };
        soil.crop.currentPhase.Value = 0;
        bool candidate = true;
        BetterJunimosFertilizerIntegration.Available(location, tile, ref candidate);
        soil.crop.currentPhase.Value = 1;
        Check(!BetterJunimosFertilizerIntegration.Perform(location, tile, ref candidate) && !candidate,
            "phase advancement between selection and execution is rechecked");

        soil.crop.currentPhase.Value = 0;
        Traits.Write(soil.crop.modData, ["fast_growth:3", "companion:2", "researcher:1"]);
        soil.crop.modData[Companion.Key] = "190";
        soil.crop.modData[MutationState.Key] = "-";
        var metadata = soil.crop.modData.ToArray();
        soil.OnSpeed = () => soil.crop.phaseDays[0] = 4;
        Check(!BetterJunimosFertilizerIntegration.Speed(soil, soil.crop) && soil.SpeedCalls == 1,
            "normal speed method is called once and copied Better Junimos formula skipped");
        Check(soil.crop.phaseDays[0] == 4 && metadata.All(p => soil.crop.modData[p.Key] == p.Value),
            "speed result retained without changing inherited traits or mutation state");
        int[] phases = soil.crop.phaseDays.ToArray();
        soil.OnSpeed = () =>
        {
            soil.crop.phaseDays[0] = 99; soil.crop.currentPhase.Value = 2;
            soil.crop.modData.Clear(); soil.state.Value = 0;
            throw new Exception("injected speed callback failure");
        };
        Check(BetterJunimosFertilizerIntegration.Speed(soil, soil.crop), "speed failure falls back to original Better Junimos formula");
        Check(soil.crop.phaseDays.SequenceEqual(phases) && soil.crop.currentPhase.Value == 0 && soil.state.Value == 1
            && metadata.All(p => soil.crop.modData[p.Key] == p.Value), "failure restores phase metadata and water before fallback");
        int calls = soil.SpeedCalls;
        Check(!BetterJunimosFertilizerIntegration.Speed(soil, null) && soil.SpeedCalls == calls, "empty soil does not recalculate growth");
        Check(BetterJunimosFertilizerIntegration.Speed(soil, new Crop()) && soil.SpeedCalls == calls,
            "mismatched crop retains original behavior");
        soil.OnSpeed = null;
        Console.WriteLine("Passed Better Junimos phase-0 fertilizer restriction, execution recheck, normal growth routing, rollback and optional registration. Uses doubles; live fertilizer/trait timing remains untested.");
    }
    public static void Unrelated() { }
    private sealed class Ability
    {
        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) => true;
        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) => true;
        private static void CheckSpeedGro(HoeDirt hd, Crop crop) { }
    }
}
