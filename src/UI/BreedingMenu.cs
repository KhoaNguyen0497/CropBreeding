using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    private ClickableComponent donorSlot = null!, seedSlot = null!, breedButton = null!, modeButton = null!;
    private Item? hover;
    private string message = "5 donor crops + 5 matching seeds = 1 bred seed";

    internal BreedingMenu(SObject machine, GameLocation location, NetMutex mutex)
        : base(okButton: false, trashCan: false, heldItemExitBehavior: ItemExitBehavior.ReturnToPlayer, allowExitWithHeldItem: true)
    {
        this.machine = machine;
        this.location = location;
        this.mutex = mutex;
        message = ModeHint;
        Layout();
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
        breedButton = new(new Rectangle(xPositionOnScreen + 352, yPositionOnScreen + 232, 160, 64), "Breed")
            { myID = 1002, upNeighborID = 1000, downNeighborID = inventory.inventory[0].myID };
        modeButton = new(new Rectangle(xPositionOnScreen + 520, yPositionOnScreen + 22, 276, 48), "Mode")
            { myID = 1003, downNeighborID = 1001 };
        donorSlot.upNeighborID = seedSlot.upNeighborID = 1003;
        allClickableComponents = new(inventory.inventory) { donorSlot, seedSlot, breedButton, modeButton };
        foreach (var slot in inventory.inventory.Take(12)) slot.upNeighborID = 1002;
        initializeUpperRightCloseButton();
        if (upperRightCloseButton != null) allClickableComponents.Add(upperRightCloseButton);
        if (Game1.options.SnappyMenus) snapToDefaultClickableComponent();
    }
    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = donorSlot;
        snapCursorToCurrentSnappedComponent();
    }
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();
    private bool Present => location.objects.TryGetValue(machine.TileLocation, out var current) && ReferenceEquals(current, machine);
    private bool SettingCompanion => Breeder.CompanionMode(machine);
    private string ModeHint => SettingCompanion ? "1 Companion seed + 1 chosen crop" : "5 donor crops + 5 matching seeds = 1 bred seed";
    private bool CanBreed => !machine.readyForHarvest.Value && machine.heldObject.Value is Item donor
        && seeds != null && (SettingCompanion ? seeds.Stack >= 1 && Breeder.CanAssign(donor, seeds)
            : seeds.Stack >= Breeder.SeedsRequired && Breeder.CanBreed(donor, seeds, out _));

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (!Present || !mutex.IsLockHeld()) return;
        if (modeButton.containsPoint(x, y))
        {
            if (machine.heldObject.Value != null || seeds != null || heldItem != null)
                message = "Empty both slots before changing mode.";
            else
            {
                if (SettingCompanion) machine.modData.Remove(Breeder.ModeKey);
                else machine.modData[Breeder.ModeKey] = "true";
                message = ModeHint;
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
                heldItem.Stack -= SettingCompanion ? 1 : Breeder.DonorsRequired;
                if (heldItem.Stack == 0) heldItem = null;
                Game1.playSound("Ship");
            }
            else if (!SettingCompanion && !machine.readyForHarvest.Value && heldItem != null
                && machine.heldObject.Value is Item pending && pending.Stack < Breeder.DonorsRequired
                && pending.canStackWith(heldItem))
            {
                // Allow old saves with a single staged donor to be topped up without losing it.
                int count = Math.Min(heldItem.Stack, Breeder.DonorsRequired - pending.Stack);
                pending.Stack += count;
                heldItem.Stack -= count;
                if (heldItem.Stack == 0) heldItem = null;
            }
            else message = SettingCompanion ? "Put a seed with Companion in the left slot." : "Put a stack of at least 5 matching trait crops in the donor slot.";
            return;
        }
        if (seedSlot.containsPoint(x, y))
        {
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
    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (!Present || !mutex.IsLockHeld()) return;
        if (donorSlot.containsPoint(x, y) || seedSlot.containsPoint(x, y)) receiveLeftClick(x, y, playSound);
        else base.receiveRightClick(x, y, playSound);
    }
    public override void performHoverAction(int x, int y)
    {
        hover = donorSlot.containsPoint(x, y) ? machine.heldObject.Value
            : seedSlot.containsPoint(x, y) ? seeds : inventory.hover(x, y, heldItem);
    }
    public override void update(GameTime time)
    {
        base.update(time);
        if (!Present || !mutex.IsLockHeld())
        {
            if (!Present) seeds = null; // Destroying the machine destroys its staged contents.
            exitThisMenu();
        }
    }
    protected override void cleanupBeforeExit()
    {
        if (!cleaned)
        {
            cleaned = true;
            if (seeds != null)
            {
                Item? remainder = Game1.player.addItemToInventory(seeds);
                if (remainder != null) Game1.createItemDebris(remainder, Game1.player.Position, -1, Game1.player.currentLocation);
                seeds = null;
            }
            if (mutex.IsLockHeld()) mutex.ReleaseLock();
        }
        base.cleanupBeforeExit();
    }
    public override void emergencyShutDown() { cleanupBeforeExit(); base.emergencyShutDown(); }
    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .65f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        b.DrawString(Game1.dialogueFont, "Crop Breeding", new Vector2(xPositionOnScreen + 48, yPositionOnScreen + 28), Game1.textColor);
        DrawSlot(b, donorSlot, machine.heldObject.Value, machine.readyForHarvest.Value ? "Bred seed" : SettingCompanion ? "Companion seed" : "Donor crops (5)");
        DrawSlot(b, seedSlot, seeds, SettingCompanion ? "Chosen crop (1)" : "Seeds (5)");
        drawTextureBox(b, modeButton.bounds.X, modeButton.bounds.Y, modeButton.bounds.Width, modeButton.bounds.Height, Color.White);
        b.DrawString(Game1.smallFont, SettingCompanion ? "Mode: Set Companion" : "Mode: Breeding", new Vector2(modeButton.bounds.X + 12, modeButton.bounds.Y + 8), Game1.textColor);
        drawTextureBox(b, breedButton.bounds.X, breedButton.bounds.Y, breedButton.bounds.Width, breedButton.bounds.Height, CanBreed ? Color.White : Color.LightGray);
        b.DrawString(Game1.smallFont, SettingCompanion ? "Set" : "Breed", new Vector2(breedButton.bounds.X + 40, breedButton.bounds.Y + 16), CanBreed ? Game1.textColor : Color.Gray);
        b.DrawString(Game1.smallFont, message, new Vector2(xPositionOnScreen + 48, yPositionOnScreen + 312), Game1.textColor);
        inventory.draw(b);
        upperRightCloseButton?.draw(b);
        if (hover != null && heldItem == null) drawToolTip(b, hover.getDescription(), hover.DisplayName, hover);
        heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
        drawMouse(b);
    }
    private static void DrawSlot(SpriteBatch b, ClickableComponent slot, Item? item, string label)
    {
        b.DrawString(Game1.smallFont, label, new Vector2(slot.bounds.X - 16, slot.bounds.Y - 40), Game1.textColor);
        drawTextureBox(b, slot.bounds.X - 8, slot.bounds.Y - 8, 80, 80, Color.White);
        item?.drawInMenu(b, new Vector2(slot.bounds.X, slot.bounds.Y), 1f);
    }
}
