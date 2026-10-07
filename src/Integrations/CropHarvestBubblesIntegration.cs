using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace CropBreeding.Integrations;

// Optional aedenthorn Crop Harvest Bubbles integration (Nexus 17761, checked against 0.6.2).
// Wrap only that mod's crop-icon draws. Its own early returns still control visibility,
// and its actual draw arguments provide position, bobbing, size, opacity and depth.
internal static class CropHarvestBubblesIntegration
{
    internal static void Register(Harmony harmony)
    {
        if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded("aedenthorn.CropHarvestBubbles")) return;
        try
        {
            Type type = AccessTools.TypeByName("CropHarvestBubbles.ModEntry")
                ?? throw new TypeLoadException("Crop Harvest Bubbles ModEntry was not found.");
            Type[] args = [typeof(Crop), typeof(SpriteBatch), typeof(Vector2), typeof(int)];
            MethodInfo? target = AccessTools.Method(type, "DrawHarvestBubble", args);
            if (target == null || !target.IsStatic || target.ReturnType != typeof(void))
                throw new MissingMethodException(type.FullName, "DrawHarvestBubble");
            PatchInstaller.Apply(harmony, typeof(CropHarvestBubblesIntegration), type, target.Name,
                transpiler: nameof(Transpiler), parameters: args);
        }
        catch (Exception ex) { ErrorHandler.Report("Register Crop Harvest Bubbles badge", ex); }
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = instructions.ToList();
        try
        {
            MethodInfo draw = AccessTools.Method(typeof(SpriteBatch), nameof(SpriteBatch.Draw),
                [typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float),
                    typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float)]);
            // The verified method draws the bubble, the item, then an optional colored layer.
            // Fail closed if another version changes this contract; keep all original draws.
            if (original.Count(i => i.Calls(draw)) != 3
                || original.Count(i => i.operand is MethodInfo m && m.Name == "GetSourceRect"
                    && m.DeclaringType?.FullName == "StardewValley.ItemTypeDefinitions.ParsedItemData") != 2)
                throw new InvalidOperationException("Unsupported Crop Harvest Bubbles draw layout; normal bubbles are unchanged.");
            MethodInfo wrapper = AccessTools.Method(typeof(CropHarvestBubblesIntegration), nameof(DrawCropIcon));
            var result = new List<CodeInstruction>(original.Count + 4);
            int drawIndex = 0;
            foreach (var instruction in original)
            {
                var copy = new CodeInstruction(instruction);
                if (copy.Calls(draw) && ++drawIndex > 1)
                {
                    var crop = new CodeInstruction(OpCodes.Ldarg_0);
                    crop.labels.AddRange(copy.labels);
                    crop.blocks.AddRange(copy.blocks);
                    copy.labels.Clear(); copy.blocks.Clear();
                    result.Add(crop);
                    result.Add(new CodeInstruction(drawIndex == 3 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                    copy.opcode = OpCodes.Call;
                    copy.operand = wrapper;
                }
                result.Add(copy);
            }
            return result;
        }
        catch (Exception ex)
        {
            ErrorHandler.Report("Patch Crop Harvest Bubbles badge", ex);
            return original;
        }
    }

    internal static void DrawCropIcon(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source,
        Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth,
        Crop crop, bool coloredLayer)
    {
        // Always execute the original draw exactly once. Badge errors must never hide it.
        batch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
        try
        {
            // Colored crops have two item layers. Add one badge only, after the last layer.
            if (coloredLayer != crop.programColored.Value || !MutationState.HasMutation(crop)
                || source is not Rectangle rect || scale <= 0 || !float.IsFinite(scale) || color.A == 0
                || rotation != 0 || effects != SpriteEffects.None) return;
            // Native 7x8 plus symbol from the options controls, not an item-quality star.
            // At the normal 64px crop-icon size this is just 14x16px in its upper-right corner.
            Rectangle badge = OptionsPlusMinus.plusButtonSource;
            float badgeScale = scale / 2f;
            Vector2 corner = new(position.X + (rect.Width - origin.X) * scale - badge.Width * badgeScale,
                position.Y - origin.Y * scale);
            batch.Draw(Game1.mouseCursors, corner, badge, Color.White * (color.A / 255f),
                0f, Vector2.Zero, badgeScale, SpriteEffects.None, Math.Clamp(depth + .00002f, 0f, 1f));
        }
        catch (Exception ex) { ErrorHandler.Report("Draw Crop Harvest Bubbles mutation badge", ex); }
    }
}
