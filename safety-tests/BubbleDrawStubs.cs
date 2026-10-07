// Draw-call recording and IL instruction doubles; no graphics device or live detours.
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework
{
    public readonly record struct Rectangle(int X, int Y, int Width, int Height);
}
namespace Microsoft.Xna.Framework.Graphics
{
    public enum SpriteEffects { None }
    public sealed class Texture2D { public int Width = 9, Height = 9; }
    public sealed class SpriteBatch
    {
        public readonly record struct DrawCall(Texture2D Texture, Vector2 Position, Rectangle? Source,
            Color Color, float Rotation, Vector2 Origin, float Scale, SpriteEffects Effects, float Depth);
        public readonly List<DrawCall> Calls = [];
        public bool FailBadge;
        public void Draw(Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation,
            Vector2 origin, float scale, SpriteEffects effects, float depth)
        {
            if (FailBadge && ReferenceEquals(texture, CropBreeding.ModEntry.Instance.Helper.ModContent.Texture))
                throw new InvalidOperationException("injected badge draw failure");
            Calls.Add(new(texture, position, source, color, rotation, origin, scale, effects, depth));
        }
    }
}
namespace StardewValley.ItemTypeDefinitions
{
    public sealed class ParsedItemData { public Rectangle GetSourceRect() => new(0, 0, 16, 16); }
}
namespace HarmonyLib
{
    public sealed class CodeInstruction
    {
        public OpCode opcode;
        public object? operand;
        public List<Label> labels = [];
        public List<object> blocks = [];
        public CodeInstruction(OpCode op, object? value = null) { opcode = op; operand = value; }
        public CodeInstruction(CodeInstruction other)
        { opcode = other.opcode; operand = other.operand; labels = new(other.labels); blocks = new(other.blocks); }
        public bool Calls(MethodInfo method) => (opcode == OpCodes.Call || opcode == OpCodes.Callvirt) && Equals(operand, method);
    }
}
