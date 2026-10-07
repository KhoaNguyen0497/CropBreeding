using System.Reflection.Emit;
using CropBreeding.Integrations;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace CropBreeding;

internal static class CropHarvestBubblesTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    internal static void Run()
    {
        var registry = ModEntry.Instance.Helper.ModRegistry;
        var harmony = new Harmony();
        CropHarvestBubblesIntegration.Register(harmony);
        Check(harmony.Installed.Count == 0, "absent bubble mod installs no draw patch");
        registry.Loaded.Add("aedenthorn.CropHarvestBubbles");
        AccessTools.NamedType = typeof(BubbleContract);
        CropHarvestBubblesIntegration.Register(harmony);
        Check(harmony.Installed.Count == 1, "recognized method receives only a local transpiler");
        var failed = new Harmony { ThrowOnPatch = true };
        CropHarvestBubblesIntegration.Register(failed);
        Check(failed.Installed.Count == 0, "failed bubble registration rolls back its addition");
        registry.Loaded.Remove("aedenthorn.CropHarvestBubbles"); AccessTools.NamedType = null;

        var draw = AccessTools.Method(typeof(SpriteBatch), nameof(SpriteBatch.Draw));
        var getSource = AccessTools.Method(typeof(ParsedItemData), nameof(ParsedItemData.GetSourceRect));
        var wrapper = AccessTools.Method(typeof(CropHarvestBubblesIntegration), nameof(CropHarvestBubblesIntegration.DrawCropIcon));
        // Representative call sites plus an early-return branch. We don't simulate JIT/detouring.
        var exit = new DynamicMethod("test", typeof(void), Type.EmptyTypes).GetILGenerator().DefineLabel();
        var original = new List<CodeInstruction>
        {
            new(OpCodes.Brtrue, exit), new(OpCodes.Callvirt, draw), new(OpCodes.Callvirt, getSource),
            new(OpCodes.Callvirt, draw), new(OpCodes.Callvirt, getSource), new(OpCodes.Callvirt, draw), new(OpCodes.Ret)
        };
        original[3].labels.Add(default); original[3].blocks.Add(new object()); original[^1].labels.Add(exit);
        var changed = CropHarvestBubblesIntegration.Transpiler(original).ToList();
        Check(changed.Count(i => i.Calls(draw)) == 1 && changed.Count(i => i.Calls(wrapper)) == 2,
            "bubble frame unchanged; only crop and colored-layer draws are wrapped");
        Check(changed[0].operand!.Equals(exit) && changed[^1].labels.Contains(exit), "hidden-bubble early return preserved");
        int first = changed.FindIndex(i => i.Calls(wrapper));
        Check(changed[first - 2].opcode == OpCodes.Ldarg_0 && changed[first - 1].opcode == OpCodes.Ldc_I4_0
            && changed[first - 2].labels.Count == 1 && changed[first - 2].blocks.Count == 1,
            "crop/primary flag loaded and branch/exception labels moved to first added instruction");
        Check(original[3].Calls(draw) && original[3].labels.Count == 1 && original[3].blocks.Count == 1,
            "input instructions not mutated, so fallback is safe");
        var unsupported = original.Take(4).ToArray();
        Check(CropHarvestBubblesIntegration.Transpiler(unsupported).SequenceEqual(unsupported), "unknown layout preserves original bubble code");

        var crop = new Crop();
        crop.currentPhase.Value = crop.phaseDays.Count - 1; crop.dayOfCurrentPhase.Value = 0;
        var texture = new Texture2D();
        var batch = new SpriteBatch();
        void Render(bool overlay = false, float scale = 4, byte alpha = 191) => CropHarvestBubblesIntegration.DrawCropIcon(
            batch, texture, new(100, 200), new Rectangle(0, 0, 16, 16), new Color(alpha), 0,
            new(8, 8), scale, SpriteEffects.None, .5f, crop, overlay);
        int rolls = Traits.RandomCalls;
        foreach (string? outcome in new string?[] { null, "-", "fast_growth:1" })
        {
            batch.Calls.Clear();
            if (outcome == null) crop.modData.Remove(MutationState.Key); else crop.modData[MutationState.Key] = outcome;
            Render();
            Check(batch.Calls.Count == (outcome == "fast_growth:1" ? 2 : 1), "badge only for a saved successful mutation");
            var primary = batch.Calls[0];
            Check(ReferenceEquals(primary.Texture, texture) && primary.Position == new Vector2(100, 200)
                && primary.Color.A == 191 && primary.Scale == 4 && primary.Depth == .5f, "original crop draw preserved exactly");
        }
        var badge = batch.Calls[1];
        Check(badge.Source == new Rectangle(0, 0, 9, 9) && badge.Position == new Vector2(114, 168)
            && badge.Scale == 2 && badge.Color.A == 191 && badge.Depth > .5f, "native gold sparkle in upper right with inherited opacity and higher depth");
        batch.Calls.Clear(); Render(scale: 2, alpha: 128);
        Check(batch.Calls[1].Position == new Vector2(107, 184) && batch.Calls[1].Scale == 1
            && batch.Calls[1].Color.A == 128, "half-size bubble scales badge and placement together");
        batch.Calls.Clear(); crop.programColored.Value = true; Render(); Render(overlay: true);
        Check(batch.Calls.Count == 3 && ReferenceEquals(batch.Calls[^1].Texture, ModEntry.Instance.Helper.ModContent.Texture), "one badge after both colored-crop layers");
        crop.programColored.Value = false;
        foreach (bool dead in new[] { false, true })
        {
            batch.Calls.Clear(); crop.dead.Value = dead;
            crop.fullyGrown.Value = !dead; crop.dayOfCurrentPhase.Value = 2;
            Render(); Check(batch.Calls.Count == 1, "no badge for a regrowth countdown or dead crop");
        }
        crop.dead.Value = false; crop.fullyGrown.Value = false; crop.dayOfCurrentPhase.Value = 0;
        batch.Calls.Clear(); batch.FailBadge = true; Render();
        Check(batch.Calls.Count == 1, "badge failure leaves normal crop visible and does not escape");
        Check(Traits.RandomCalls == rolls && crop.modData[MutationState.Key] == "fast_growth:1", "drawing never rolls or changes mutation state");
        Console.WriteLine("Passed optional Crop Harvest Bubbles contract, scoped IL replacement/fallback, mutation badge gating, size/opacity/position, colored layers and draw failure isolation. Uses draw/IL doubles; live rendering remains untested.");
    }
    private static class BubbleContract
    {
        private static void DrawHarvestBubble(Crop crop, SpriteBatch batch, Vector2 tile, int offset) { }
    }
}
