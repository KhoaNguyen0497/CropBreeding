namespace CropBreeding.Core;

// All drawing, pointer hitboxes and controller targets share this transform.
internal readonly record struct MenuGeometry(int ViewWidth, int ViewHeight, int InventoryRows = 3)
{
    internal const int InventoryTop = 384;
    internal const int InventoryLeft = 76;
    internal const int SlotPitch = 72;
    internal int InventoryBottom => InventoryTop + Math.Clamp(InventoryRows, 3, 4) * SlotPitch;
    internal int LogicalHeight => InventoryBottom + 64;
    internal float Scale => Math.Min(1f, Math.Min(Math.Max(1, ViewWidth - 24) / 1008f, Math.Max(1, ViewHeight - 24) / (float)LogicalHeight));
    internal int Size(int value) => Math.Max(1, (int)Math.Round(value * Scale));
    internal int Width => Size(1008);
    internal int Height => Size(LogicalHeight);
    internal int Left => (ViewWidth - Width) / 2;
    internal int Top => (ViewHeight - Height) / 2;
    internal int X(int value) => Left + (int)Math.Round(value * Scale);
    internal int Y(int value) => Top + (int)Math.Round(value * Scale);
}

// Page indices always refer to the real backpack; no copied or reordered items.
internal readonly record struct MenuInventoryLayout(int SlotCount, int Page)
{
    internal const int Columns = 12;
    internal const int PageSize = Columns * 4;
    internal int Capacity => Math.Max(36, (SlotCount + Columns - 1) / Columns * Columns);
    internal int VisibleRows => Math.Clamp(Capacity / Columns, 3, 4);
    internal int PageCount => (Capacity + PageSize - 1) / PageSize;
    internal int PageIndex => Math.Clamp(Page, 0, PageCount - 1);
    internal int Start => PageIndex * PageSize;
    internal int End => Math.Min(Capacity, Start + PageSize);
    internal bool IsVisible(int index) => index >= Start && index < End;
}
