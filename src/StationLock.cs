using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;

namespace CropBreeding;

// Keep vanilla's lock while the menu is in use, then remove its idle dictionary entry.
// Removal is deferred out of FarmerTeam.Update's dictionary enumeration.
internal sealed class StationLock
{
    private static readonly Dictionary<(FarmerTeam Team, string Key), NetMutex> PendingRemoval = new();
    private readonly FarmerTeam team;
    private readonly string key;
    private readonly NetMutex mutex;

    internal StationLock(StardewValley.Object machine, GameLocation location)
    {
        team = Game1.player.team;
        key = $"{ModEntry.Id}/{location.NameOrUniqueName}/{machine.TileLocation.X}/{machine.TileLocation.Y}";
        mutex = team.GetOrCreateGlobalInventoryMutex(key);
    }

    internal bool IsLockHeld() => mutex.IsLockHeld();
    internal void RequestLock(Action acquired, Action failed)
    {
        try { mutex.RequestLock(acquired, () => { QueueRemoval(); failed(); }); }
        catch { QueueRemoval(); throw; }
    }
    internal void ReleaseLock()
    {
        if (mutex.IsLockHeld()) mutex.ReleaseLock();
        QueueRemoval();
    }
    private void QueueRemoval()
    {
        // Multiplayer support is deferred. Never remove another player's active lock.
        if (!Game1.IsMasterGame) return;
        if (PendingRemoval.Count == 0)
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += RemoveIdleLocks;
        PendingRemoval[(team, key)] = mutex;
    }
    private static void RemoveIdleLocks(object? sender, UpdateTickedEventArgs e)
    {
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= RemoveIdleLocks;
        try
        {
            if (!Context.IsWorldReady) return;
            foreach (var entry in PendingRemoval)
                ErrorHandler.Try("Remove idle station lock", () =>
                {
                    var (team, key) = entry.Key;
                    if (!entry.Value.IsLocked() && team.globalInventoryMutexes.TryGetValue(key, out var current)
                        && ReferenceEquals(current, entry.Value))
                        team.globalInventoryMutexes.Remove(key);
                });
        }
        finally { PendingRemoval.Clear(); }
    }
}
