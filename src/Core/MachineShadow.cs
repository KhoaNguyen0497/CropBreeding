namespace CropBreeding.Core;

internal static class MachineShadow
{
    // Match Fertilizer Makers' compact Seed Maker-colour ground footprint.
    // Run once when the research texture loads; never add a per-frame draw patch.
    internal static void Apply<T>(T[] pixels, int width, int height, T shadow, Func<T, bool> isTransparent)
    {
        if (width != 32 || height != 32 || pixels.Length != width * height)
            throw new ArgumentException("Expected the two-frame 32x32 research machine atlas.");
        for (int frame = 0; frame < 2; frame++)
            for (int y = 29; y <= 31; y++)
            {
                int inset = y == 31 ? 4 : 2;
                for (int x = inset; x < 16 - inset; x++)
                {
                    int index = y * width + frame * 16 + x;
                    if (isTransparent(pixels[index])) pixels[index] = shadow;
                }
            }
    }
}
