using System;
using System.Collections.Generic;
using System.Linq;

namespace CropBreeding.Core;

// Research guarantees two successful increases. Ordinary harvest mutation still
// rolls the full eligible pool and can waste a capped pick.
internal static class ResearchRules
{
    private static string[] Eligible(bool canRegrow, Func<string, bool>? available) => TraitRules.Known
        .Where(id => id != "researcher" && (!canRegrow || id is not ("rooted" or "nurse_crop"))
            && (available?.Invoke(id) ?? true)).ToArray();

    internal static bool CanResearch(IEnumerable<string> traits, int limit, bool canRegrow, Func<string, bool>? available = null)
    {
        string[] input = TraitRules.Parse(TraitRules.Encode(traits));
        if (TraitRules.Level(input, "researcher") == 0) return false;
        string[] remaining = TraitRules.Without(input, "researcher");
        if (remaining.Length > limit) return false;
        int capacity = 0;
        foreach (string id in Eligible(canRegrow, available))
        {
            int level = TraitRules.Level(remaining, id);
            if (level == 0)
            {
                if (remaining.Length < limit) return true; // Any new research trait has five levels available.
            }
            else capacity += TraitRules.MaxLevel(id) - level;
        }
        return capacity >= 2;
    }

    internal static bool TryResearch(IEnumerable<string> traits, int limit, bool canRegrow, Random random,
        out string[] result, Func<string, bool>? available = null)
    {
        result = TraitRules.Parse(TraitRules.Encode(traits));
        if (!CanResearch(result, limit, canRegrow, available)) return false;
        string[] current = TraitRules.Without(result, "researcher");
        string[] eligible = Eligible(canRegrow, available);
        for (int roll = 0; roll < 2; roll++)
        {
            string[] choices = eligible.Where(id =>
            {
                int level = TraitRules.Level(current, id);
                return level > 0 ? level < TraitRules.MaxLevel(id) : current.Length < limit;
            }).ToArray();
            if (choices.Length == 0) return false;
            string chosen = choices[random.Next(choices.Length)];
            int next = TraitRules.Level(current, chosen) + 1;
            current = TraitRules.Parse(TraitRules.Encode(TraitRules.Without(current, chosen).Append($"{chosen}:{next}")));
        }
        result = current;
        return true;
    }
}
