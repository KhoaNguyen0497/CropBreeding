using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SObject = StardewValley.Object;

namespace CropBreeding.UI;

// One cached texture shared by item icons and the optional harvest-bubble integration.
// Rendering only reads metadata; it never scans inventories or prepares mutation rolls.
internal static class TraitBadge
{
    internal static readonly Rectangle Source = new(0, 0, 9, 9);
    private static Texture2D? texture;

    internal static void Load()
    {
        try
        {
            var loaded = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/trait-sparkle.png");
            if (loaded.Width != Source.Width || loaded.Height != Source.Height)
                throw new InvalidOperationException("Trait sparkle must be a 9x9 sprite.");
            texture = loaded;
        }
        catch (Exception ex) { ErrorHandler.Report("Load trait sparkle", ex); }
    }

    internal static void Register(Harmony harmony)
        => PatchInstaller.Apply(harmony, typeof(TraitBadge), typeof(Item), nameof(Item.DrawMenuIcons),
            postfix: nameof(InventoryPostfix), parameters:
            [typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float),
                typeof(StackDrawType), typeof(Color)]);

    [HarmonyPriority(Priority.Last)]
    internal static void InventoryPostfix(Item __instance, SpriteBatch sb, Vector2 location,
        float scale_size, float transparency, float layer_depth, Color color)
    {
        try
        {
            if (__instance is not SObject item || item.bigCraftable.Value || item.IsRecipe
                || !item.modData.TryGetValue(Traits.Key, out string? traits) || string.IsNullOrEmpty(traits)) return;
            Draw(sb, location.X + 32f + 32f * scale_size, location.Y + 32f - 32f * scale_size,
                scale_size * 2f, transparency * (color.A / 255f), layer_depth);
        }
        catch (Exception ex) { ErrorHandler.Report("Draw inventory trait badge", ex); }
    }

    internal static void Draw(SpriteBatch batch, float right, float top, float scale, float alpha, float depth)
    {
        if (texture == null || scale <= 0 || alpha <= 0 || !float.IsFinite(scale) || !float.IsFinite(alpha)
            || !float.IsFinite(right) || !float.IsFinite(top) || !float.IsFinite(depth)) return;
        try
        {
            batch.Draw(texture, new Vector2(right - Source.Width * scale, top), Source,
                Color.White * Math.Clamp(alpha, 0f, 1f), 0f, Vector2.Zero, scale,
                SpriteEffects.None, Math.Clamp(depth + .00004f, 0f, 1f));
        }
        catch (Exception ex) { ErrorHandler.Report("Draw trait sparkle", ex); }
    }
}
