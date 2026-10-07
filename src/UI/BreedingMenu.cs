using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using CropBreeding.Core;
using SObject = StardewValley.Object;

namespace CropBreeding.UI;

internal sealed class BreedingMenu : MenuWithInventory
{
    private readonly SObject machine;
    private readonly GameLocation location;
    private readonly StationLock mutex;
    private Item? seeds;
    private bool cleaned;
    private bool recovering;
    private int selectedTrait;
    private MenuGeometry geometry;
    private bool canBreed;
    private string[] removalTraits = [];
    private string selectedTraitLabel = "No trait selected";
    private EligibilityState? lastEligibility;
    private string? lastMessage;
    private string wrappedMessage = "";
    private readonly record struct EligibilityState(Item? Donor, int DonorCount, string? DonorTraits, string? DonorCompanion,
        Item? Seeds, int SeedCount, string? SeedTraits, string? SeedCompanion, bool Ready, bool CompanionMode,
        bool RemoveMode, int Selection, int TraitLimit, int Cost, int CatalogRevision);
    private ClickableComponent donorSlot = null!, seedSlot = null!, breedButton = null!, modeButton = null!;
    private Item? hover;
    private string message = "";

    // Snapshot references and counts for our slot/button transactions only. No cloning,
    // world scanning or per-frame inventory snapshots.
    private sealed class InputSnapshot
    {
        private readonly BreedingMenu menu;
        private readonly Item? seeds, held;
        private readonly SObject? output;
        private readonly int seedCount, heldCount, outputCount, minutes;
        private readonly bool ready;
        private readonly KeyValuePair<string, string>[] metadata;
        internal InputSnapshot(BreedingMenu menu)
        {
            this.menu = menu;
            seeds = menu.seeds; held = menu.heldItem; output = menu.machine.heldObject.Value;
            seedCount = seeds?.Stack ?? 0; heldCount = held?.Stack ?? 0; outputCount = output?.Stack ?? 0;
            ready = menu.machine.readyForHarvest.Value; minutes = menu.machine.MinutesUntilReady;
            metadata = menu.machine.modData.Pairs.ToArray();
        }
        internal void Restore()
        {
            menu.seeds = seeds; menu.heldItem = held; menu.machine.heldObject.Value = output;
            if (seeds != null) seeds.Stack = seedCount;
            if (held != null) held.Stack = heldCount;
            if (output != null) output.Stack = outputCount;
            menu.machine.readyForHarvest.Value = ready; menu.machine.MinutesUntilReady = minutes;
            menu.machine.modData.Clear();
            foreach (var pair in metadata) menu.machine.modData[pair.Key] = pair.Value;
        }
    }

    private void Failed(string action, Exception ex)
    {
        ErrorHandler.Report(action, ex);
        if (recovering) return;
        recovering = true;
        try { cleanupBeforeExit(); }
        catch (Exception cleanupError) { ErrorHandler.Report("Close failed breeding menu", cleanupError); }
        finally
        {
            ErrorHandler.Try("Release machine lock", () => { mutex?.ReleaseLock(); });
            if (ReferenceEquals(Game1.activeClickableMenu, this)) Game1.activeClickableMenu = null;
        }
    }

    internal BreedingMenu(SObject machine, GameLocation location, StationLock mutex)
        : base(okButton: false, trashCan: false, heldItemExitBehavior: ItemExitBehavior.ReturnToPlayer, allowExitWithHeldItem: true)
    {
        this.machine = machine;
        this.location = location;
        this.mutex = mutex;
        message = ModeHint;
        RefreshEligibility();
        Layout();
        if (recovering) throw new InvalidOperationException("The breeding menu could not initialize.");
    }
    private Rectangle Bounds(int x, int y, int w, int h) => new(geometry.X(x), geometry.Y(y), geometry.Size(w), geometry.Size(h));
    private void Layout()
    {
        int previousFocus = currentlySnappedComponent?.myID ?? 1000;
        geometry = new(Game1.uiViewport.Width, Game1.uiViewport.Height);
        width = geometry.Width; height = geometry.Height;
        xPositionOnScreen = geometry.Left; yPositionOnScreen = geometry.Top;
        inventory.xPositionOnScreen = geometry.X(48);
        inventory.yPositionOnScreen = geometry.Y(384);
        inventory.width = geometry.Size(768);
        inventory.height = geometry.Size(208);
        for (int i = 0; i < inventory.inventory.Count; i++)
        {
            var slot = inventory.inventory[i];
            slot.bounds = Bounds(48 + i % 12 * 64, 384 + i / 12 * 72, 64, 64);
            slot.leftNeighborID = i % 12 > 0 ? inventory.inventory[i - 1].myID : -1;
            slot.rightNeighborID = i % 12 < 11 && i + 1 < inventory.inventory.Count ? inventory.inventory[i + 1].myID : -1;
            slot.upNeighborID = i >= 12 ? inventory.inventory[i - 12].myID : 1002;
            slot.downNeighborID = i + 12 < inventory.inventory.Count ? inventory.inventory[i + 12].myID : -1;
        }
        donorSlot = new(Bounds(240, 128, 64, 64), "Donor")
            { myID = 1000, rightNeighborID = 1001, upNeighborID = 1003, downNeighborID = 1002 };
        seedSlot = new(RemovingTrait ? Bounds(440, 128, 340, 64) : Bounds(528, 128, 64, 64), "Seeds")
            { myID = 1001, leftNeighborID = 1000, upNeighborID = 1003, downNeighborID = 1002 };
        breedButton = new(Bounds(352, 224, 160, 64), "Breed")
            { myID = 1002, upNeighborID = 1000, downNeighborID = inventory.inventory[0].myID };
        modeButton = new(Bounds(448, 24, 304, 48), "Mode")
            { myID = 1003, rightNeighborID = 1004, downNeighborID = 1001 };
        initializeUpperRightCloseButton();
        if (upperRightCloseButton != null)
        {
            upperRightCloseButton.bounds = Bounds(776, 24, 48, 48);
            upperRightCloseButton.myID = 1004;
            upperRightCloseButton.leftNeighborID = 1003;
            upperRightCloseButton.downNeighborID = 1001;
        }
        lastMessage = null;
        populateClickableComponentList();
        if (Game1.options.SnappyMenus)
        {
            currentlySnappedComponent = allClickableComponents.FirstOrDefault(c => c.myID == previousFocus) ?? donorSlot;
            snapCursorToCurrentSnappedComponent();
        }
    }
    public override void snapToDefaultClickableComponent()
    {
        try
        {
            currentlySnappedComponent = donorSlot;
            snapCursorToCurrentSnappedComponent();
        }
        catch (Exception ex)
        {
            Failed("snapToDefaultClickableComponent", ex);
        }
    }
    public override void populateClickableComponentList()
    {
        try
        {
            // Vanilla's reflection-based discovery omits these private controls when rebuilding focus.
            allClickableComponents = inventory == null ? new() : new(inventory.inventory);
            foreach (var component in new[] { donorSlot, seedSlot, breedButton, modeButton })
                if (component != null) allClickableComponents.Add(component);
            if (upperRightCloseButton != null) allClickableComponents.Add(upperRightCloseButton);
        }
        catch (Exception ex)
        {
            Failed("populateClickableComponentList", ex);
        }
    }
    public override void receiveGamePadButton(Buttons button)
    {
        try
        {
            if (button == Buttons.B)
            {
                ModEntry.Instance.Helper.Input.Suppress(SButton.ControllerB);
                exitThisMenu();
                return;
            }
            base.receiveGamePadButton(button);
        }
        catch (Exception ex)
        {
            Failed("receiveGamePadButton", ex);
        }
    }
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        try
        {
            Layout();
        }
        catch (Exception ex)
        {
            Failed("gameWindowSizeChanged", ex);
        }
    }
    private bool Present => location.objects.TryGetValue(machine.TileLocation, out var current) && ReferenceEquals(current, machine);
    private bool SettingCompanion => Breeder.CompanionMode(machine);
    private bool RemovingTrait => Breeder.RemoveMode(machine);
    private string[] RemovalTraits => removalTraits;
    private string? SelectedTrait => removalTraits.Length > 0 ? removalTraits[selectedTrait % removalTraits.Length] : null;
    private string ModeHint => RemovingTrait ? "Insert 1 seed, choose a trait, then select Remove." : SettingCompanion ? "1 Companion seed + 1 chosen crop" : BreedingHint;
    private static string BreedingHint
    {
        get
        {
            int cost = Breeder.IngredientsRequired;
            return cost == 1 ? "1 donor crop + 1 matching seed = 1 bred seed"
                : $"{cost} donor crops + {cost} matching seeds = 1 bred seed";
        }
    }
    private bool CanBreed => canBreed;
    private static string? Metadata(Item? item, string key) => item != null && item.modData.TryGetValue(key, out string value) ? value : null;
    private void RefreshEligibility()
    {
        Item? donor = machine.heldObject.Value;
        var state = new EligibilityState(donor, donor?.Stack ?? 0, Metadata(donor, Traits.Key), Metadata(donor, Companion.Key),
            seeds, seeds?.Stack ?? 0, Metadata(seeds, Traits.Key), Metadata(seeds, Companion.Key), machine.readyForHarvest.Value,
            SettingCompanion, RemovingTrait, selectedTrait, ModEntry.Instance.Config.MaximumTraits, Breeder.IngredientsRequired, Companion.Revision);
        if (lastEligibility == state) return;
        removalTraits = donor != null && !state.Ready ? Traits.Read(donor.modData) : [];
        selectedTraitLabel = SelectedTrait is string trait ? TraitRules.Label(trait) : "No trait selected";
        canBreed = !state.Ready && donor != null && (state.RemoveMode ? Breeder.CanRemoveFrom(donor) && SelectedTrait != null
            : seeds != null && (state.CompanionMode ? seeds.Stack >= 1 && Breeder.CanAssign(donor, seeds)
            : seeds.Stack >= Breeder.IngredientsRequired && Breeder.CanBreed(donor, seeds, out _)));
        lastEligibility = state;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        InputSnapshot? snapshot = null;
        try
        {
            if (!Present || !mutex.IsLockHeld()) return;
            RefreshEligibility();
            if (upperRightCloseButton?.containsPoint(x, y) == true)
            {
                exitThisMenu();
                return;
            }
            if (modeButton.containsPoint(x, y) || donorSlot.containsPoint(x, y) || seedSlot.containsPoint(x, y) || breedButton.containsPoint(x, y))
                snapshot = new InputSnapshot(this);
            if (modeButton.containsPoint(x, y))
            {
                if (machine.heldObject.Value != null || seeds != null || heldItem != null)
                    message = "Empty both slots before changing mode.";
                else
                {
                    if (RemovingTrait) machine.modData.Remove(Breeder.RemoveModeKey);
                    else if (SettingCompanion)
                    {
                        machine.modData.Remove(Breeder.ModeKey);
                        machine.modData[Breeder.RemoveModeKey] = "true";
                    }
                    else machine.modData[Breeder.ModeKey] = "true";
                    selectedTrait = 0;
                    message = ModeHint;
                    Layout();
                    if (Game1.options.SnappyMenus)
                    {
                        currentlySnappedComponent = modeButton;
                        snapCursorToCurrentSnappedComponent();
                    }
                }
                return;
            }
            if (donorSlot.containsPoint(x, y))
            {
                if (heldItem == null && machine.heldObject.Value is Item item)
                {
                    heldItem = item;
                    Breeder.Clear(machine);
                    Game1.playSound("dwop");
                }
                else if (heldItem != null && machine.heldObject.Value == null && Breeder.Insert(machine, heldItem, false))
                {
                    heldItem.Stack -= SettingCompanion || RemovingTrait ? 1 : Breeder.IngredientsRequired;
                    selectedTrait = 0;
                    if (heldItem.Stack == 0) heldItem = null;
                    Game1.playSound("Ship");
                }
                else message = RemovingTrait ? "Put a seed with traits in the left slot." : SettingCompanion ? "Put a seed with Companion in the left slot." : $"Put at least {Breeder.IngredientsRequired} matching trait crops in the donor slot.";
                return;
            }
            if (seedSlot.containsPoint(x, y))
            {
                if (RemovingTrait)
                {
                    if (RemovalTraits.Length > 0) selectedTrait = (selectedTrait + 1) % RemovalTraits.Length;
                    return;
                }
                if (heldItem == null) { heldItem = seeds; seeds = null; }
                else if (SettingCompanion ? !Companion.Valid(heldItem) : !CropCatalog.EligibleSeed(heldItem.ItemId))
                    message = SettingCompanion ? "Choose an eligible companion crop." : "Put matching seeds in this slot.";
                else if (seeds == null) { seeds = heldItem; heldItem = null; }
                else if (seeds.canStackWith(heldItem))
                {
                    int count = Math.Min(heldItem.Stack, seeds.maximumStackSize() - seeds.Stack);
                    seeds.Stack += count; heldItem.Stack -= count;
                    if (heldItem.Stack == 0) heldItem = null;
                }
                else { Item swap = seeds; seeds = heldItem; heldItem = swap; }
                return;
            }
            if (breedButton.containsPoint(x, y))
            {
                if (RemovingTrait)
                {
                    if (CanBreed && SelectedTrait is string trait && Breeder.RemoveTrait(machine, Core.TraitRules.Id(trait)))
                    {
                        message = "Trait removed. Collect your seed from the left slot.";
                        Game1.playSound("coin");
                    }
                    else message = "Insert a seed with traits and choose a trait to remove.";
                    return;
                }
                if (CanBreed && Breeder.Insert(machine, seeds!, false))
                {
                    seeds!.Stack -= SettingCompanion ? 1 : Breeder.IngredientsRequired;
                    if (seeds.Stack == 0) seeds = null;
                    message = "Ready! Collect your bred seed from the left slot.";
                    Game1.playSound("coin");
                }
                else message = SettingCompanion ? "Choose a different eligible companion crop." : $"Need {Breeder.IngredientsRequired} donor crops and {Breeder.IngredientsRequired} compatible seeds. If the cost changed, retrieve and reinsert the donor crops.";
                return;
            }
            base.receiveLeftClick(x, y, playSound);
        }
        catch (Exception ex)
        {
            if (snapshot != null) ErrorHandler.Try("Restore breeding inputs", snapshot.Restore);
            Failed("Breeding input", ex);
        }
        finally
        {
            if (!cleaned) ErrorHandler.Try("Refresh breeding controls", RefreshEligibility);
        }
    }
    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        try
        {
            if (!Present || !mutex.IsLockHeld()) return;
            if (donorSlot.containsPoint(x, y) || seedSlot.containsPoint(x, y)) receiveLeftClick(x, y, playSound);
            else base.receiveRightClick(x, y, playSound);
        }
        catch (Exception ex)
        {
            Failed("receiveRightClick", ex);
        }
    }
    public override void performHoverAction(int x, int y)
    {
        try
        {
            hover = donorSlot.containsPoint(x, y) ? machine.heldObject.Value
                : seedSlot.containsPoint(x, y) ? seeds : inventory.hover(x, y, heldItem);
        }
        catch (Exception ex)
        {
            Failed("performHoverAction", ex);
        }
    }
    public override void update(GameTime time)
    {
        try
        {
            base.update(time);
            RefreshEligibility();
            if (!Present || !mutex.IsLockHeld())
            {
                if (!Present) seeds = null; // Destroying the machine destroys its staged contents.
                exitThisMenu();
            }
        }
        catch (Exception ex)
        {
            Failed("update", ex);
        }
    }
    protected override void cleanupBeforeExit()
    {
        if (cleaned) return;
        cleaned = true;
        try
        {
            Item? returnSeeds = seeds; seeds = null;
            ReturnItem(returnSeeds);
            Item? returnHeld = heldItem; heldItem = null;
            ReturnItem(returnHeld);
            ErrorHandler.Try("Base menu cleanup", () => base.cleanupBeforeExit());
        }
        finally { ErrorHandler.Try("Release machine lock", () => { mutex.ReleaseLock(); }); }
    }
    private static void ReturnItem(Item? item)
    {
        // The caller clears its slot first, so repeated cleanup cannot return it twice.
        if (item == null) return;
        try
        {
            Item? remainder = Game1.player.addItemToInventory(item);
            if (remainder != null) Game1.createItemDebris(remainder, Game1.player.Position, -1, Game1.player.currentLocation);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report("Return menu item", ex);
            ErrorHandler.Try("Recover unreturned menu item", () =>
            {
                // Vanilla decrements the input stack as it merges, or stores this exact
                // reference in a slot. Do not duplicate anything already delivered.
                if (item.Stack <= 0 || Game1.player.Items.Any(slot => ReferenceEquals(slot, item))
                    || Game1.player.currentLocation.debris.Any(debris => ReferenceEquals(debris.item, item))
                    || Game1.player.team.returnedDonations.Any(saved => ReferenceEquals(saved, item))) return;
                Game1.player.team.returnedDonations.Add(item);
                Game1.player.team.newLostAndFoundItems.Value = true;
                ModEntry.Instance.Monitor.Log("An unreturned breeding-menu item was placed in the Lost and Found.", LogLevel.Warn);
            });
        }
    }
    public override void emergencyShutDown()
    {
        try
        {
            cleanupBeforeExit(); base.emergencyShutDown();
        }
        catch (Exception ex)
        {
            Failed("emergencyShutDown", ex);
        }
    }
    public override void draw(SpriteBatch b)
    {
        try
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .65f);
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
            Text(b, "Crop Breeding", Bounds(48, 24, 360, 48), Game1.dialogueFont);
            DrawSlot(b, donorSlot, machine.readyForHarvest.Value ? "Bred seed" : RemovingTrait ? "Seed (1)" : SettingCompanion ? "Companion seed" : "Donor crops (5)", Bounds(80, 88, 344, 32));
            if (RemovingTrait)
            {
                Text(b, "Choose trait / select to cycle", Bounds(440, 88, 340, 32));
                drawTextureBox(b, seedSlot.bounds.X, seedSlot.bounds.Y, seedSlot.bounds.Width, seedSlot.bounds.Height, Color.White);
                Text(b, selectedTraitLabel, Bounds(452, 140, 316, 40));
            }
            else DrawSlot(b, seedSlot, SettingCompanion ? "Chosen crop (1)" : "Seeds (5)", Bounds(440, 88, 340, 32));
            drawTextureBox(b, modeButton.bounds.X, modeButton.bounds.Y, modeButton.bounds.Width, modeButton.bounds.Height, Color.White);
            Text(b, RemovingTrait ? "Mode: Remove Trait" : SettingCompanion ? "Mode: Set Companion" : "Mode: Breeding", Bounds(460, 28, 280, 40));
            drawTextureBox(b, breedButton.bounds.X, breedButton.bounds.Y, breedButton.bounds.Width, breedButton.bounds.Height, CanBreed ? Color.White : Color.LightGray);
            Text(b, RemovingTrait ? "Remove" : SettingCompanion ? "Set" : "Breed", Bounds(364, 236, 136, 40), color: CanBreed ? Game1.textColor : Color.Gray);
            if (lastMessage != message)
            {
                wrappedMessage = Game1.parseText(message, Game1.smallFont, 768);
                lastMessage = message;
            }
            Text(b, wrappedMessage, Bounds(48, 304, 768, 68));
            DrawInventory(b);
            if (upperRightCloseButton != null)
                b.Draw(upperRightCloseButton.texture, upperRightCloseButton.bounds, upperRightCloseButton.sourceRect, Color.White);
            DrawItems(b);
            if (hover != null && heldItem == null) drawToolTip(b, hover.getDescription(), hover.DisplayName, hover);
            drawMouse(b);
        }
        catch (Exception ex) { Failed("draw", ex); }
    }
    private void Text(SpriteBatch b, string text, Rectangle area, SpriteFont? font = null, Color? color = null)
    {
        font ??= Game1.smallFont;
        Vector2 size = font.MeasureString(text);
        float scale = Math.Min(geometry.Scale, Math.Min(area.Width / Math.Max(1f, size.X), area.Height / Math.Max(1f, size.Y)));
        b.DrawString(font, text, new Vector2(area.X, area.Y + (area.Height - size.Y * scale) / 2), color ?? Game1.textColor,
            0, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }
    private void DrawSlot(SpriteBatch b, ClickableComponent slot, string label, Rectangle labelArea)
    {
        Text(b, label, labelArea);
        int border = geometry.Size(8);
        drawTextureBox(b, slot.bounds.X - border, slot.bounds.Y - border, slot.bounds.Width + border * 2, slot.bounds.Height + border * 2, Color.White);
    }
    private void DrawInventory(SpriteBatch b)
    {
        // Vanilla InventoryMenu uses fixed 64px drawing coordinates. Keep its item handling,
        // but draw using the very same scaled rectangles as pointer/controller hit testing.
        Rectangle background = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
        Rectangle locked = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57);
        for (int i = 0; i < inventory.inventory.Count; i++)
        {
            Rectangle bounds = inventory.inventory[i].bounds;
            b.Draw(Game1.menuTexture, bounds, background, Color.White);
            if (i >= Game1.player.MaxItems) b.Draw(Game1.menuTexture, bounds, locked, Color.White * .5f);
        }
    }
    private void DrawItems(SpriteBatch b)
    {
        // Item.drawInMenu's scale argument leaves quality/stack positions at fixed 64px
        // offsets. Transform one batch of native item draws instead, including those icons
        // and modded item renderers. Never allocate render targets or a batch per item.
        bool transformed = geometry.Scale < 1;
        if (transformed)
        {
            b.End();
            try { b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(geometry.Scale)); }
            catch
            {
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                throw;
            }
        }
        try
        {
            Draw(machine.heldObject.Value, donorSlot.bounds.X, donorSlot.bounds.Y);
            if (!RemovingTrait) Draw(seeds, seedSlot.bounds.X, seedSlot.bounds.Y);
            for (int i = 0; i < inventory.inventory.Count && i < inventory.actualInventory.Count && i < Game1.player.MaxItems; i++)
                Draw(inventory.actualInventory[i], inventory.inventory[i].bounds.X, inventory.inventory[i].bounds.Y);
            Draw(heldItem, Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8);
        }
        finally
        {
            // Restore the standard Game1.DrawMenu batch even if an item renderer throws.
            if (transformed)
            {
                try { b.End(); }
                finally { b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp); }
            }
        }
        void Draw(Item? item, int x, int y) => item?.drawInMenu(b, new Vector2(x / geometry.Scale, y / geometry.Scale), 1f);
    }
}
