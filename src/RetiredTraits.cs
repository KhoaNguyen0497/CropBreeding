using StardewValley;

namespace CropBreeding;

internal static class RetiredTraits
{
    // Change only our retired token, keeping the original item, quantity, quality,
    // other traits (including unfamiliar tokens), and other mods' metadata intact.
    internal static bool CleanItem(Item item)
    {
        if (!item.modData.TryGetValue(Traits.Key, out string? value) || value == null) return false;
        string[] tokens = value.Split(',');
        string[] kept = tokens.Where(t => Core.TraitRules.Id(t.Trim()) != "rooted").ToArray();
        if (kept.Length == tokens.Length) return false;
        string remaining = string.Join(',', kept);
        if (string.IsNullOrWhiteSpace(remaining.Trim(','))) item.modData.Remove(Traits.Key);
        else item.modData[Traits.Key] = remaining;
        return true;
    }
}
