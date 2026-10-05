using System;
using System.Collections.Generic;
using System.Linq;

namespace CropBreeding.Core;

public static class TraitRules
{
    public const int MaximumLevel = 5;
    public static readonly string[] Known = ["fast_growth", "high_yield", "premium", "hardy"];
    public static string Id(string trait) => trait.Split(':')[0];
    private static int TokenLevel(string token)
    {
        string[] parts = token.Split(':');
        if (parts.Length == 1) return 1; // Existing saves store bare trait IDs.
        return parts.Length == 2 && int.TryParse(parts[1], out int level) && level > 0
            ? Math.Min(level, MaximumLevel) : 0;
    }
    private static string Token(string id, int level) => level == 1 ? id : $"{id}:{level}";
    public static string[] Parse(string? value) => (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Where(t => Known.Contains(Id(t)) && TokenLevel(t) > 0)
        .GroupBy(Id).OrderBy(g => g.Key, StringComparer.Ordinal)
        .Select(g => Token(g.Key, g.Max(TokenLevel))).ToArray();
    public static string Encode(IEnumerable<string> values) => string.Join(',', Parse(string.Join(',', values)));
    public static int Level(IEnumerable<string> traits, string id) => Parse(Encode(traits))
        .Where(t => Id(t) == id).Select(TokenLevel).DefaultIfEmpty(0).Max();
    public static string[] Mutate(IEnumerable<string> inherited, int limit, double chance, Random random)
    {
        string[] current = Parse(Encode(inherited));
        if (random.NextDouble() >= Math.Clamp(chance, 0, 1)) return current;
        // Each eligible trait type has one chance: add it at level 1, or upgrade it by one.
        string[] choices = Known.Where(id => Level(current, id) is > 0 and < MaximumLevel
            || (Level(current, id) == 0 && current.Length < Math.Max(0, limit))).ToArray();
        if (choices.Length == 0) return current;
        string chosen = choices[random.Next(choices.Length)];
        return Parse(Encode(current.Where(t => Id(t) != chosen).Append(Token(chosen, Level(current, chosen) + 1))));
    }
    public static bool TryBreed(IEnumerable<string> donor, IEnumerable<string> seed, int limit, out string[] result)
    {
        result = Parse(Encode(donor));
        string[] seedTraits = Parse(Encode(seed));
        // Plain seeds copy the donor, preserving traits if the user lowered the configured count cap.
        if (seedTraits.Length == 0) return result.Length > 0;
        if (result.Length == 0 || seedTraits.Length != 1 || TokenLevel(seedTraits[0]) != 1) return false;
        string id = Id(seedTraits[0]);
        int level = Level(result, id);
        if (result.Length > Math.Max(0, limit) || level >= MaximumLevel
            || (level == 0 && result.Length >= Math.Max(0, limit))) return false;
        result = Parse(Encode(result.Where(t => Id(t) != id).Append(Token(id, level + 1))));
        return true;
    }
    public static bool Same(string? a, string? b) => Encode(Parse(a)) == Encode(Parse(b));
    public static string Label(string token)
    {
        string name = Id(token) switch
        {
            "fast_growth" => "Fast Growth", "high_yield" => "High Yield", "premium" => "Premium", "hardy" => "Hardy", _ => Id(token)
        };
        return $"{name} {TokenLevel(token)}";
    }
}
