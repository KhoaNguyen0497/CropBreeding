using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace CropBreeding;

// Vanilla's soil actions roll IslandFarming walnuts only when harvest returns true.
// Rooted changes that removal result to false. Restore only the skipped soil-action roll.
internal static class RootedWalnuts
{
    [ThreadStatic] private static Attempt? current;
    internal sealed class Attempt(HoeDirtAlias soil, Crop crop, GameLocation location, Vector2 tile)
    {
        internal readonly HoeDirtAlias Soil = soil;
        internal readonly Crop Crop = crop;
        internal readonly GameLocation Location = location;
        internal readonly Vector2 Tile = tile;
        internal bool Restarted;
        internal bool Finished;
    }
    internal readonly record struct State(Attempt? Previous, Attempt? Active);

    internal static void Register(Harmony harmony)
    {
        foreach (string method in new[] { nameof(HoeDirtAlias.performUseAction), nameof(HoeDirtAlias.performToolAction) })
            PatchInstaller.Apply(harmony, typeof(RootedWalnuts), typeof(HoeDirtAlias), method,
                prefix: nameof(Begin), finalizer: nameof(End));
    }

    internal static void Begin(HoeDirtAlias __instance, Vector2 tileLocation, out State __state)
    {
        __state = new(current, null);
        current = null;
        try
        {
            if (__instance.Location is IslandLocation location && __instance.crop is { } crop)
                current = new(__instance, crop, location, tileLocation);
            __state = __state with { Active = current };
        }
        catch (Exception ex) { ErrorHandler.Report("Prepare Rooted walnut check", ex); }
    }

    // Called only after an otherwise successful annual harvest actually restarts through Rooted.
    internal static void Restarted(Crop crop)
    {
        if (current is { } attempt && ReferenceEquals(attempt.Crop, crop)
            && ReferenceEquals(attempt.Soil.crop, crop)) attempt.Restarted = true;
    }

    internal static Exception? End(Exception? __exception, State __state)
    {
        current = __state.Previous;
        Attempt? attempt = __state.Active;
        if (attempt == null || attempt.Finished) return __exception;
        attempt.Finished = true;
        if (__exception != null || !attempt.Restarted) return __exception;
        try
        {
            // Match vanilla: one roll, normal shared cap, same player/team and tile coordinates.
            // The team method owns counting/awarding; never directly create a walnut item.
            if (Game1.random.NextDouble() < 0.05)
                Game1.player.team.RequestLimitedNutDrops("IslandFarming", attempt.Location,
                    (int)attempt.Tile.X * 64, (int)attempt.Tile.Y * 64, 5);
        }
        catch (Exception ex) { ErrorHandler.Report("Rooted island walnut", ex); }
        return __exception;
    }
}
