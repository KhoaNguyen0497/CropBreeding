using System;
using System.Collections.Generic;
using System.Linq;

namespace CropBreeding.Core;

public static class TraitRules
{
    public static readonly string[] Known = ["fast_growth", "high_yield", "premium", "hardy"];
    public static string[] Parse(string? value) => (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Where(Known.Contains).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
    public static string Encode(IEnumerable<string> values) => string.Join(',', Parse(string.Join(',', values)));
    public static string[] Mutate(IEnumerable<string> inherited, int limit, double chance, Random random)
    {
        string[] current = Parse(Encode(inherited));
        if (current.Length >= Math.Max(0, limit) || random.NextDouble() >= Math.Clamp(chance, 0, 1))
            return current;
        string[] available = Known.Except(current).ToArray();
        return available.Length == 0 ? current : Parse(Encode(current.Append(available[random.Next(available.Length)])));
    }
    public static bool Same(string? a, string? b) => Encode(Parse(a)) == Encode(Parse(b));
    public static string Label(string id) => id switch
    {
        "fast_growth" => "Fast Growth", "high_yield" => "High Yield", "premium" => "Premium", "hardy" => "Hardy", _ => id
    };
}
