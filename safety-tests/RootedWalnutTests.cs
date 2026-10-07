using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace CropBreeding
{
    internal static class RootedWalnutTests
    {
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        internal static void Run()
        {
            Random previousRandom = Game1.random;
            Farmer previousPlayer = Game1.player;
            Game1.player = new Farmer();
            var random = new CountingRandom(0.049);
            Game1.random = random;
            var soil = new HoeDirt { Location = new IslandLocation() };
            var crop = new Crop { Dirt = soil };
            soil.crop = crop;
            Vector2 tile = new(8, 12);
            try
            {
                RootedWalnuts.Begin(soil, tile, out var state);
                RootedWalnuts.Restarted(crop);
                RootedWalnuts.Restarted(crop);
                RootedWalnuts.End(null, state);
                RootedWalnuts.End(null, state);
                var requests = Game1.player.team.NutRequests;
                Check(random.Calls == 1 && requests.Count == 1, "successful Rooted action rolls once, even with repeated notification/finalization");
                Check(requests[0] == ("IslandFarming", soil.Location, 512, 768, 5), "vanilla shared key, location, coordinates and limit retained");

                random.Value = .05;
                RootedWalnuts.Begin(soil, tile, out state);
                RootedWalnuts.Restarted(crop);
                RootedWalnuts.End(null, state);
                Check(random.Calls == 2 && requests.Count == 1, "exact 5 percent boundary does not award");
                // No successful restart = vanilla is responsible for any original roll.
                RootedWalnuts.Begin(soil, tile, out state);
                RootedWalnuts.End(null, state);
                RootedWalnuts.Restarted(crop); // direct Crop.harvest callers have no soil action scope.
                Check(random.Calls == 2, "failed/non-Rooted/regrowing/direct automated harvests get no supplemental roll");

                RootedWalnuts.Begin(soil, tile, out state);
                RootedWalnuts.Restarted(new Crop());
                RootedWalnuts.End(null, state);
                Check(random.Calls == 2, "unrelated nested crop cannot trigger another soil's roll");
                soil.Location = new GameLocation();
                RootedWalnuts.Begin(soil, tile, out state);
                RootedWalnuts.Restarted(crop);
                RootedWalnuts.End(null, state);
                Check(random.Calls == 2, "main farm uses no extra RNG or requests");

                soil.Location = new IslandLocation();
                RootedWalnuts.Begin(soil, tile, out state);
                RootedWalnuts.Restarted(crop);
                var failure = new Exception("original soil action error");
                Check(ReferenceEquals(RootedWalnuts.End(failure, state), failure) && random.Calls == 2,
                    "failed soil action doesn't roll or swallow the original error");

                RootedWalnuts.Begin(soil, tile, out var outer);
                var ordinarySoil = new HoeDirt { Location = new GameLocation(), crop = new Crop() };
                RootedWalnuts.Begin(ordinarySoil, tile, out var inner);
                RootedWalnuts.Restarted(crop);
                RootedWalnuts.End(null, inner);
                RootedWalnuts.Restarted(crop);
                RootedWalnuts.End(null, outer);
                Check(random.Calls == 3, "nested non-island action isolates and then restores outer scope");

                random.Value = 0;
                Game1.player.team.FailNutRequest = true;
                RootedWalnuts.Begin(soil, tile, out state);
                RootedWalnuts.Restarted(crop);
                Check(RootedWalnuts.End(null, state) == null, "request error logged without breaking harvest");
                Game1.player.team.FailNutRequest = false;
                RootedWalnuts.Begin(soil, tile, out state);
                RootedWalnuts.Restarted(crop);
                RootedWalnuts.End(null, state);
                Check(random.Calls == 5 && requests.Count == 2, "next harvest works after request failure without retrying prior award");
                Console.WriteLine("Passed Rooted walnut scope, single roll, 5-percent boundary, vanilla cap request, nesting and error cleanup. Uses doubles; live island harvesting remains untested.");
            }
            finally { Game1.random = previousRandom; Game1.player = previousPlayer; }
        }
        private sealed class CountingRandom(double value) : Random
        {
            internal double Value = value;
            internal int Calls;
            public override double NextDouble() { Calls++; return Value; }
        }
    }
}
namespace StardewValley.Locations { public sealed class IslandLocation : GameLocation { } }
