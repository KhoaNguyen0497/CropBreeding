namespace CropBreeding.Core;

// Demand-driven summaries: no timer, update event, or repeated exception formatting.
internal sealed class ErrorThrottle
{
    private sealed class Entry(string signature, long time)
    {
        internal string Signature = signature;
        internal long LastLog = time;
        internal int Repeats;
    }
    private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    internal (bool Full, int Repeats) Next(string action, string signature, long now)
    {
        if (!entries.TryGetValue(action, out var entry) || entry.Signature != signature)
        {
            entries[action] = new(signature, now);
            return (true, 0);
        }
        entry.Repeats++;
        if (now - entry.LastLog < 10_000) return (false, 0);
        int repeats = entry.Repeats;
        entry.Repeats = 0;
        entry.LastLog = now;
        return (false, repeats);
    }
}
