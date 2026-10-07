using CropBreeding.UI;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SObject = StardewValley.Object;

namespace CropBreeding;

internal static class TraitBadgeTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    internal static void Run()
    {
        var batch = new SpriteBatch();
        var item = new SObject { ItemId = "472", Quality = 4, Stack = 99 };
        item.modData[Traits.Key] = "fast_growth:2";
        void Render(Item? subject = null, float scale = 1, float opacity = 1, byte alpha = 255, float depth = .5f)
            => TraitBadge.InventoryPostfix(subject ?? item, batch, new(100, 200), scale, opacity, depth, new Color(alpha));

        ItemRegistry.ThrowData = true; TraitBadge.Load(); Render();
        Check(batch.Calls.Count == 0, "missing badge asset leaves original rendering alone");
        ItemRegistry.ThrowData = false; ItemRegistry.MissingData = true; TraitBadge.Load(); Render();
        Check(batch.Calls.Count == 0, "missing Qi Gem data leaves original rendering alone");
        ItemRegistry.MissingData = false; ItemRegistry.Gem.Source = new Rectangle(0, 0, 64, 64); TraitBadge.Load(); Render();
        Check(batch.Calls.Count == 0, "invalid badge dimensions are rejected");
        ItemRegistry.Gem.Source = new Rectangle(380, 620, 16, 16); TraitBadge.Load(); Render();
        Check(batch.Calls.Count == 0, "out-of-texture sprite is rejected");
        ItemRegistry.Gem.Source = new Rectangle(288, 560, 16, 16); TraitBadge.Load();
        int loads = ItemRegistry.DataLoads, rolls = Traits.RandomCalls;
        var harmony = new Harmony(); TraitBadge.Register(harmony);
        Check(harmony.Installed.Count == 1 && harmony.Postfix?.Name == "InventoryPostfix", "one shared vanilla overlay hook");
        var failed = new Harmony { ThrowOnPatch = true }; TraitBadge.Register(failed);
        Check(failed.Installed.Count == 0, "failed inventory badge registration is rolled back");

        Render();
        var badge = batch.Calls.Single();
        Check(badge.Position == new Vector2(132, 200) && badge.Source == new Rectangle(288, 560, 16, 16)
            && badge.Scale == 2 && badge.Color.A == 255 && badge.Depth > .5f,
            "32x32 native Qi Gem in top right, away from bottom quality and stack overlays");
        batch.Calls.Clear(); Render(scale: .5f, opacity: .5f, alpha: 128, depth: 1f);
        Check(batch.Calls.Single().Position == new Vector2(132, 216) && batch.Calls[0].Scale == 1
            && batch.Calls[0].Color.A == 64 && batch.Calls[0].Depth == 1f, "badge follows scale, combined opacity and bounded depth");
        foreach (float scale in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        { batch.Calls.Clear(); Render(scale: scale); Check(batch.Calls.Count == 0, "invalid or hidden size has no badge"); }
        batch.Calls.Clear(); Render(opacity: 0); Render(alpha: 0); Render(depth: float.NaN);
        Check(batch.Calls.Count == 0, "invisible or invalid draws skipped");
        item.IsRecipe = true; Render(); item.IsRecipe = false;
        item.bigCraftable.Value = true; Render(); item.bigCraftable.Value = false;
        Render(new Item()); Render(new SObject());
        item.modData[Traits.Key] = ""; Render();
        Check(batch.Calls.Count == 0, "recipes, machines, non-objects and untraited/empty items excluded");
        item.ItemId = "24"; item.modData[Traits.Key] = "fast_growth:2"; Render();
        Check(batch.Calls.Count == 1, "harvested produce uses same badge as seeds");
        batch.Calls.Clear(); batch.FailBadge = true; Render();
        Check(batch.Calls.Count == 0, "badge draw failure does not escape into game UI");
        batch.FailBadge = false; Render();
        Check(batch.Calls.Count == 1 && ItemRegistry.DataLoads == loads, "later draw recovers without per-frame asset loads");
        Check(Traits.RandomCalls == rolls && item.modData[Traits.Key] == "fast_growth:2"
            && item.Stack == 99 && item.Quality == 4, "drawing preserves traits, quantity, quality and RNG");
        Console.WriteLine("Passed trait badge asset failures, optional-free registration, metadata gating, placement, scale/opacity/depth and error isolation. Uses draw doubles, not an in-game render.");
    }
}
