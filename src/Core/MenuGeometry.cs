namespace CropBreeding.Core;

// All drawing, pointer hitboxes and controller targets share this transform.
internal readonly record struct MenuGeometry(int ViewWidth, int ViewHeight)
{
    internal float Scale => Math.Min(1f, Math.Min(Math.Max(1, ViewWidth - 24) / 864f, Math.Max(1, ViewHeight - 24) / 640f));
    internal int Size(int value) => Math.Max(1, (int)Math.Round(value * Scale));
    internal int Width => Size(864);
    internal int Height => Size(640);
    internal int Left => (ViewWidth - Width) / 2;
    internal int Top => (ViewHeight - Height) / 2;
    internal int X(int value) => Left + (int)Math.Round(value * Scale);
    internal int Y(int value) => Top + (int)Math.Round(value * Scale);
}
