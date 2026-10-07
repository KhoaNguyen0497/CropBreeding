using CropBreeding.Integrations;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;

namespace CropBreeding
{
    internal static class BetterJunimosPlantingTests
    {
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        internal static void Run()
        {
            var registry = ModEntry.Instance.Helper.ModRegistry;
            var harmony = new Harmony();
            BetterJunimosIntegration.Register(harmony);
            Check(harmony.Installed.Count == 0, "optional mod absent: no patches");
            registry.Loaded.Add("hawkfalcon.BetterJunimos");
            AccessTools.NamedType = typeof(Ability);
            BetterJunimosIntegration.Register(harmony);
            Check(harmony.Installed.Count == 4, "recognized planting contracts install four scoped hooks");
            var failing = new Harmony { ThrowOnPatch = true };
            var unrelated = typeof(BetterJunimosPlantingTests).GetMethod(nameof(Unrelated))!;
            failing.Installed.Add(unrelated);
            BetterJunimosIntegration.Register(failing);
            Check(failing.Installed.Count == 1 && failing.Installed.Contains(unrelated), "registration rollback preserves unrelated patches");
            registry.Loaded.Clear(); AccessTools.NamedType = null;
            ModEntry.Instance.Config = new ModConfig { MutationChance = 0 };

            var ability = new Ability();
            var location = new GameLocation();
            Vector2 tile = new(2, 3);
            var soil = new HoeDirt { Tile = tile };
            location.terrainFeatures[tile] = soil;
            var chosen = new Item { ItemId = "472", Stack = 5 };
            Traits.Write(chosen.modData, ["fast_growth:3", "companion:2", "researcher:1"]);
            chosen.modData[Companion.Key] = "190";
            var otherStack = new Item { ItemId = "472", Stack = 5 };
            Traits.Write(otherStack.modData, ["high_yield:5"]);

            // Availability probes happen outside PerformAction and must not supply its seed.
            BetterJunimosIntegration.Selected(ability, location, otherStack);
            BetterJunimosIntegration.Begin(ability, location, tile, out var outer);
            BetterJunimosIntegration.Selected(ability, location, chosen);
            soil.crop = new Crop { Dirt = soil, currentLocation = location };
            BetterJunimosIntegration.Planted(ability, location, tile, "472", true);
            Check(soil.crop.modData[Traits.Key] == chosen.modData[Traits.Key], "actual selected stack determines traits, not another same-ID stack");
            Check(soil.crop.modData[Companion.Key] == "190", "Companion selection transferred");
            Check(soil.SpeedCalls == 1, "initial growth reapplied through shared vanilla speed hook");
            Check(chosen.Stack == 5 && otherStack.Stack == 5, "integration never consumes or changes seed inventory");
            BetterJunimosIntegration.Planted(ability, location, tile, "472", true);
            Check(soil.SpeedCalls == 1, "capture applied at most once");
            BetterJunimosIntegration.End(null, outer);

            // A failed/nested action cannot leak its selected seed into the next action.
            BetterJunimosIntegration.Begin(ability, location, tile, out outer);
            BetterJunimosIntegration.Selected(ability, location, chosen);
            BetterJunimosIntegration.Begin(ability, location, tile, out var inner);
            BetterJunimosIntegration.Selected(ability, location, otherStack);
            var failure = new Exception("Better Junimos planting failed");
            Check(ReferenceEquals(BetterJunimosIntegration.End(failure, inner), failure), "another mod's error is not swallowed");
            soil.crop = new Crop { Dirt = soil };
            BetterJunimosIntegration.Planted(ability, location, tile, "472", false);
            Check(!soil.crop.modData.ContainsKey(Traits.Key), "failed plant leaves metadata alone");
            BetterJunimosIntegration.Planted(ability, location, tile, "472", true);
            Check(soil.crop.modData[Traits.Key] == chosen.modData[Traits.Key], "nested action restores outer seed");
            BetterJunimosIntegration.End(null, outer);
            soil.crop = new Crop { Dirt = soil };
            BetterJunimosIntegration.Planted(ability, location, tile, "472", true);
            Check(!soil.crop.modData.ContainsKey(Traits.Key), "completed action leaves no seed capture");

            foreach (string boundary in new[] { "wrong seed", "wrong tile", "pot", "plain", "excluded" })
            {
                var seed = boundary == "plain" ? new Item { ItemId = "472" } : chosen;
                if (boundary == "excluded")
                {
                    seed = new Item { ItemId = "885" }; Traits.Write(seed.modData, ["high_yield:5"]);
                }
                soil.crop = new Crop { Dirt = soil };
                soil.Ground = boundary != "pot";
                BetterJunimosIntegration.Begin(ability, location, tile, out outer);
                BetterJunimosIntegration.Selected(ability, location, seed);
                BetterJunimosIntegration.Planted(ability, location, boundary == "wrong tile" ? new(3, 3) : tile,
                    boundary == "wrong seed" ? "473" : seed.ItemId, true);
                BetterJunimosIntegration.End(null, outer);
                Check(!soil.crop.modData.ContainsKey(Traits.Key), boundary + " does not transfer traits");
            }
            soil.Ground = true;
            soil.crop = new Crop { Dirt = soil };
            soil.crop.modData["OtherMod"] = "preserved";
            int[] before = soil.crop.phaseDays.ToArray();
            soil.OnSpeed = () => { soil.crop.phaseDays[0] = 99; throw new Exception("speed patch failure"); };
            SeedInheritance.Apply(soil, "472", ["fast_growth:3"], null, Game1.player);
            Check(!soil.crop.modData.ContainsKey(Traits.Key) && soil.crop.modData["OtherMod"] == "preserved"
                && soil.crop.phaseDays.SequenceEqual(before), "failed trait timing restores Better Junimos' planted crop");
            soil.OnSpeed = null;
            soil.crop.currentPhase.Value = soil.crop.phaseDays.Count - 1;
            soil.crop.dayOfCurrentPhase.Value = 0;
            soil.crop.modData[MutationState.Key] = "high_yield:1";
            SeedInheritance.Apply(soil, "472", ["evergreen:5"], null, Game1.player);
            Check(Traits.Level(soil.crop.modData, "evergreen") == 5
                && MutationState.ForHarvest(soil.crop, Traits.Read(soil.crop.modData)).SequenceEqual(["evergreen:5"]),
                "instant-ready crop prepares mutation only after inheriting actual seed traits");
            Console.WriteLine("Passed Better Junimos planting selection, nesting/error cleanup, registration isolation, boundaries, trait timing routing and instant-ready inheritance. Uses test doubles; live mod integration remains untested.");
        }
        public static void Unrelated() { }
        private sealed class Ability
        {
            public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) => true;
            private Item? PlantableSeed(GameLocation location, Chest chest, string cropType) => null;
            private bool Plant(GameLocation location, Vector2 pos, string index) => true;
        }
    }
}
namespace StardewValley.Objects { public sealed class Chest { } }
