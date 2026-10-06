using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using SObject = StardewValley.Object;

namespace CropBreeding.UI;

internal sealed class BreedingMenu : MenuWithInventory
{
    private readonly SObject machine;
    private readonly GameLocation location;
    private readonly NetMutex mutex;
    private Item? seeds;
    private bool cleaned;
    private bool recovering;
    private int selectedTrait;
    private ClickableComponent donorSlot = null!, seedSlot = null!, breedButton = null!, modeButton = null!;
    private Item? hover;
    private string message = "5 donor crops + 5 matching seeds = 1 bred seed";

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
            ErrorHandler.Try("Release machine lock", () => { if (mutex != null && mutex.IsLockHeld()) mutex.ReleaseLock(); });
            if (ReferenceEquals(Game1.activeClickableMenu, this)) Game1.activeClickableMenu = null;
        }
    }

    internal BreedingMenu(SObject machine, GameLocation location, NetMutex mutex)
        : base(okButton: false, trashCan: false, heldItemExitBehavior: ItemExitBehavior.ReturnToPlayer, allowExitWithHeldItem: true)
    {
        this.machine = machine;
        this.location = location;
        this.mutex = mutex;
        message = ModeHint;
        Layout();
        if (recovering) throw new InvalidOperationException("The breeding menu could not initialize.");
    }
    private void Layout()
    {
        width = 864; height = 640;
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        inventory.movePosition(xPositionOnScreen + 48 - inventory.xPositionOnScreen,
            yPositionOnScreen + 352 - inventory.yPositionOnScreen);
        donorSlot = new(new Rectangle(xPositionOnScreen + 240, yPositionOnScreen + 120, 64, 64), "Donor")
            { myID = 1000, rightNeighborID = 1001, downNeighborID = 1002 };
        seedSlot = new(new Rectangle(xPositionOnScreen + 528, yPositionOnScreen + 120, 64, 64), "Seeds")
            { myID = 1001, leftNeighborID = 1000, downNeighborID = 1002 };
        if (RemovingTrait) seedSlot.bounds = new Rectangle(xPositionOnScreen + 440, yPositionOnScreen + 120, 340, 64);
        breedButton = new(new Rectangle(xPositionOnScreen + 352, yPositionOnScreen + 232, 160, 64), "Breed")
            { myID = 1002, upNeighborID = 1000, downNeighborID = inventory.inventory[0].myID };
        modeButton = new(new Rectangle(xPositionOnScreen + 520, yPositionOnScreen + 22, 276, 48), "Mode")
            { myID = 1003, downNeighborID = 1001 };
        donorSlot.upNeighborID = seedSlot.upNeighborID = 1003;
        foreach (var slot in inventory.inventory.Take(12)) slot.upNeighborID = 1002;
        initializeUpperRightCloseButton();
        if (upperRightCloseButton != null)
        {
            upperRightCloseButton.myID = 1004;
            upperRightCloseButton.leftNeighborID = 1003;
            upperRightCloseButton.downNeighborID = 1001;
            modeButton.rightNeighborID = 1004;
        }
        populateClickableComponentList();
        if (Game1.options.SnappyMenus) snapToDefaultClickableComponent();
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
    private string[] RemovalTraits => machine.heldObject.Value is Item item && !machine.readyForHarvest.Value ? Traits.Read(item.modData) : [];
    private string? SelectedTrait => RemovalTraits is { Length: > 0 } traits ? traits[selectedTrait % traits.Length] : null;
    private string ModeHint => RemovingTrait ? "Insert 1 seed, choose a trait, then select Remove." : SettingCompanion ? "1 Companion seed + 1 chosen crop" : "5 donor crops + 5 matching seeds = 1 bred seed";
    private bool CanBreed => !machine.readyForHarvest.Value && machine.heldObject.Value is Item donor
        && (RemovingTrait ? Breeder.CanRemoveFrom(donor) && SelectedTrait != null
            : seeds != null && (SettingCompanion ? seeds.Stack >= 1 && Breeder.CanAssign(donor, seeds)
            : seeds.Stack >= Breeder.SeedsRequired && Breeder.CanBreed(donor, seeds, out _)));

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        InputSnapshot? snapshot = null;
        try
        {
            if (!Present || !mutex.IsLockHeld()) return;
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
                    heldItem.Stack -= SettingCompanion || RemovingTrait ? 1 : Breeder.DonorsRequired;
                    selectedTrait = 0;
                    if (heldItem.Stack == 0) heldItem = null;
                    Game1.playSound("Ship");
                }
                else message = RemovingTrait ? "Put a seed with traits in the left slot." : SettingCompanion ? "Put a seed with Companion in the left slot." : "Put a stack of at least 5 matching trait crops in the donor slot.";
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
                    seeds!.Stack -= SettingCompanion ? 1 : Breeder.SeedsRequired;
                    if (seeds.Stack == 0) seeds = null;
                    message = "Ready! Collect your bred seed from the left slot.";
                    Game1.playSound("coin");
                }
                else message = SettingCompanion ? "Choose a different eligible companion crop." : "Need 5 matching donor crops and 5 compatible seeds.";
                return;
            }
            base.receiveLeftClick(x, y, playSound);
        }
        catch (Exception ex)
        {
            if (snapshot != null) ErrorHandler.Try("Restore breeding inputs", snapshot.Restore);
            Failed("Breeding input", ex);
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
        finally { ErrorHandler.Try("Release machine lock", () => { if (mutex.IsLockHeld()) mutex.ReleaseLock(); }); }
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
            b.DrawString(Game1.dialogueFont, "Crop Breeding", new Vector2(xPositionOnScreen + 48, yPositionOnScreen + 28), Game1.textColor);
            DrawSlot(b, donorSlot, machine.heldObject.Value, machine.readyForHarvest.Value ? "Bred seed" : RemovingTrait ? "Seed (1)" : SettingCompanion ? "Companion seed" : "Donor crops (5)");
            if (RemovingTrait)
            {
                b.DrawString(Game1.smallFont, "Choose trait (click to cycle)", new Vector2(seedSlot.bounds.X, seedSlot.bounds.Y - 40), Game1.textColor);
                drawTextureBox(b, seedSlot.bounds.X, seedSlot.bounds.Y, seedSlot.bounds.Width, seedSlot.bounds.Height, Color.White);
                b.DrawString(Game1.smallFont, SelectedTrait is string trait ? Core.TraitRules.Label(trait) : "No trait selected",
                    new Vector2(seedSlot.bounds.X + 12, seedSlot.bounds.Y + 16), Game1.textColor);
            }
            else DrawSlot(b, seedSlot, seeds, SettingCompanion ? "Chosen crop (1)" : "Seeds (5)");
            drawTextureBox(b, modeButton.bounds.X, modeButton.bounds.Y, modeButton.bounds.Width, modeButton.bounds.Height, Color.White);
            b.DrawString(Game1.smallFont, RemovingTrait ? "Mode: Remove Trait" : SettingCompanion ? "Mode: Set Companion" : "Mode: Breeding", new Vector2(modeButton.bounds.X + 12, modeButton.bounds.Y + 8), Game1.textColor);
            drawTextureBox(b, breedButton.bounds.X, breedButton.bounds.Y, breedButton.bounds.Width, breedButton.bounds.Height, CanBreed ? Color.White : Color.LightGray);
            b.DrawString(Game1.smallFont, RemovingTrait ? "Remove" : SettingCompanion ? "Set" : "Breed", new Vector2(breedButton.bounds.X + 40, breedButton.bounds.Y + 16), CanBreed ? Game1.textColor : Color.Gray);
            b.DrawString(Game1.smallFont, message, new Vector2(xPositionOnScreen + 48, yPositionOnScreen + 312), Game1.textColor);
            inventory.draw(b);
            upperRightCloseButton?.draw(b);
            if (hover != null && heldItem == null) drawToolTip(b, hover.getDescription(), hover.DisplayName, hover);
            heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
            drawMouse(b);
        }
        catch (Exception ex)
        {
            Failed("draw", ex);
        }
    }
    private static void DrawSlot(SpriteBatch b, ClickableComponent slot, Item? item, string label)
    {
        b.DrawString(Game1.smallFont, label, new Vector2(slot.bounds.X - 16, slot.bounds.Y - 40), Game1.textColor);
        drawTextureBox(b, slot.bounds.X - 8, slot.bounds.Y - 8, 80, 80, Color.White);
        item?.drawInMenu(b, new Vector2(slot.bounds.X, slot.bounds.Y), 1f);
    }
}
