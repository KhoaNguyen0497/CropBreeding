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
    private MenuInventoryLayout backpack;
    private int inventoryPage, observedMaxItems;
    private const int InventoryId = 10000;
    private static readonly Color Ink = new(86, 47, 24);
    private static readonly Color Muted = new(132, 98, 60);
    private static readonly Color Green = new(55, 112, 65);
    private string donorName = "Empty", seedName = "Empty";
    private string donorTraits = "", seedTraits = "";
    private string donorLabel = "", seedLabel = "";
    private string costHint = "", currentModeHint = "", pageLabel = "";
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
    private ClickableComponent companionButton = null!, removalButton = null!;
    private ClickableComponent previousPage = null!, nextPage = null!;
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
    private void Layout(int? focus = null)
    {
        int previousFocus = focus ?? currentlySnappedComponent?.myID ?? 1000;
        observedMaxItems = Game1.player.MaxItems;
        backpack = new(Math.Max(observedMaxItems, inventory.inventory.Count), inventoryPage);
        inventoryPage = backpack.PageIndex;
        pageLabel = $"{inventoryPage + 1}/{backpack.PageCount}";
        geometry = new(Game1.uiViewport.Width, Game1.uiViewport.Height, backpack.VisibleRows);
        width = geometry.Width; height = geometry.Height;
        xPositionOnScreen = geometry.Left; yPositionOnScreen = geometry.Top;
        // Some backpack mods expand InventoryMenu; others only expand the farmer's capacity.
        // Keep the original inventory and its handlers, and add missing hitboxes if necessary.
        while (inventory.inventory.Count < backpack.Capacity)
            inventory.inventory.Add(new ClickableComponent(Rectangle.Empty, inventory.inventory.Count.ToString()));
        inventory.capacity = backpack.Capacity;
        inventory.rows = backpack.Capacity / MenuInventoryLayout.Columns;
        inventory.xPositionOnScreen = geometry.X(MenuGeometry.InventoryLeft);
        inventory.yPositionOnScreen = geometry.Y(MenuGeometry.InventoryTop);
        inventory.width = geometry.Size(856);
        inventory.height = geometry.Size(backpack.VisibleRows * MenuGeometry.SlotPitch);
        for (int i = 0; i < inventory.inventory.Count; i++)
        {
            var slot = inventory.inventory[i];
            int cell = i - backpack.Start;
            slot.name = i.ToString(); // Vanilla inventory clicks use this real item index.
            slot.myID = InventoryId + i;
            slot.visible = backpack.IsVisible(i);
            slot.bounds = slot.visible ? Bounds(MenuGeometry.InventoryLeft + cell % 12 * MenuGeometry.SlotPitch,
                MenuGeometry.InventoryTop + cell / 12 * MenuGeometry.SlotPitch, 64, 64) : Rectangle.Empty;
            slot.fullyImmutable = true;
            slot.leftNeighborID = cell % 12 > 0 ? InventoryId + i - 1 : -1;
            slot.rightNeighborID = cell % 12 < 11 && i + 1 < backpack.End ? InventoryId + i + 1 : -1;
            slot.upNeighborID = cell >= 12 ? InventoryId + i - 12
                : cell < 4 ? 1000 : cell < 8 ? 1001 : backpack.PageCount > 1 ? 1007 : 1002;
            slot.downNeighborID = i + 12 < backpack.End ? InventoryId + i + 12 : -1;
        }
        donorSlot = new(Bounds(48, 196, 64, 64), "Donor")
            { myID = 1000, rightNeighborID = 1001, upNeighborID = 1003, downNeighborID = InventoryId + backpack.Start, fullyImmutable = true };
        seedSlot = new(RemovingTrait ? Bounds(384, 192, 276, 68) : Bounds(384, 196, 64, 64), "Seeds")
            { myID = 1001, leftNeighborID = 1000, rightNeighborID = 1002, upNeighborID = 1005, downNeighborID = InventoryId + backpack.Start + 4, fullyImmutable = true };
        breedButton = new(Bounds(720, 204, 240, 48), "Breed")
            { myID = 1002, leftNeighborID = 1001, upNeighborID = 1006, downNeighborID = backpack.PageCount > 1 ? 1007 : InventoryId + backpack.Start + 9, fullyImmutable = true };
        modeButton = new(Bounds(28, 80, 316, 44), "Breeding")
            { myID = 1003, rightNeighborID = 1005, upNeighborID = 1004, downNeighborID = 1000, fullyImmutable = true };
        companionButton = new(Bounds(364, 80, 316, 44), "Set Companion")
            { myID = 1005, leftNeighborID = 1003, rightNeighborID = 1006, upNeighborID = 1004, downNeighborID = 1001, fullyImmutable = true };
        removalButton = new(Bounds(700, 80, 280, 44), "Remove Trait")
            { myID = 1006, leftNeighborID = 1005, rightNeighborID = 1004, upNeighborID = 1004, downNeighborID = 1002, fullyImmutable = true };
        previousPage = new(Bounds(800, 340, 44, 32), "Previous page")
            { myID = 1007, rightNeighborID = 1008, upNeighborID = 1002, downNeighborID = InventoryId + backpack.Start + 10, visible = backpack.PageCount > 1, fullyImmutable = true };
        nextPage = new(Bounds(920, 340, 44, 32), "Next page")
            { myID = 1008, leftNeighborID = 1007, upNeighborID = 1002, downNeighborID = InventoryId + backpack.Start + 11, visible = backpack.PageCount > 1, fullyImmutable = true };
        initializeUpperRightCloseButton();
        if (upperRightCloseButton != null)
        {
            upperRightCloseButton.bounds = Bounds(936, 20, 44, 44);
            upperRightCloseButton.myID = 1004;
            upperRightCloseButton.leftNeighborID = 1006;
            upperRightCloseButton.downNeighborID = 1006;
            upperRightCloseButton.fullyImmutable = true;
        }
        lastMessage = null;
        hover = null;
        populateClickableComponentList();
        if (Game1.options.SnappyMenus)
        {
            currentlySnappedComponent = allClickableComponents.FirstOrDefault(c => c.myID == previousFocus) ?? donorSlot;
            snapCursorToCurrentSnappedComponent();
        }
    }
    private void ChangePage(int direction)
    {
        int page = Math.Clamp(inventoryPage + direction, 0, backpack.PageCount - 1);
        if (page == inventoryPage) return;
        int focus = currentlySnappedComponent?.myID ?? 1000;
        if (focus >= InventoryId && backpack.IsVisible(focus - InventoryId))
        {
            int cell = focus - InventoryId - backpack.Start;
            focus = InventoryId + Math.Min(page * MenuInventoryLayout.PageSize + cell, backpack.Capacity - 1);
        }
        inventoryPage = page;
        Layout(focus);
        Game1.playSound("shiny4");
    }
    public override void receiveScrollWheelAction(int direction)
    {
        try
        {
            if (Bounds(28, 332, 952, geometry.InventoryBottom - 332).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
                ChangePage(direction > 0 ? -1 : 1);
        }
        catch (Exception ex) { Failed("Inventory page", ex); }
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
            allClickableComponents = inventory == null ? new() : new(inventory.inventory.Where(slot => slot.visible));
            foreach (var component in new[] { donorSlot, seedSlot, breedButton, modeButton, companionButton, removalButton, previousPage, nextPage })
                if (component?.visible == true) allClickableComponents.Add(component);
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
            if (button == Buttons.X) return; // Handled once by the suppressed SMAPI input event.
            if (button == Buttons.B)
            {
                ModEntry.Instance.Helper.Input.Suppress(SButton.ControllerB);
                exitThisMenu();
                return;
            }
            if (button is Buttons.LeftShoulder or Buttons.RightShoulder)
            {
                ChangePage(button == Buttons.LeftShoulder ? -1 : 1);
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
    internal void QuickInsertSelected()
    {
        InputSnapshot? snapshot = null;
        Item? source = null;
        int index = -1, count = 0;
        try
        {
            if (!Present || !mutex.IsLockHeld() || cleaned) return;
            if (heldItem != null) { message = "Put down your held item first."; return; }
            index = Game1.options.SnappyMenus ? (currentlySnappedComponent?.myID ?? -1) - InventoryId
                : inventory.inventory.FindIndex(slot => slot.visible && slot.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()));
            if (!backpack.IsVisible(index) || index >= inventory.actualInventory.Count || index >= Game1.player.MaxItems) return;
            source = inventory.actualInventory[index];
            if (source == null) return;
            count = source.Stack;
            snapshot = new InputSnapshot(this);
            if (!StationStacks.Insert(machine, ref seeds, source))
            {
                message = "That item cannot be added to the current inputs.";
                return;
            }
            if (source.Stack == 0) inventory.actualInventory[index] = null;
            selectedTrait = 0;
            message = ModeHint;
            // Inventory transfer is committed before optional sound/label updates.
            snapshot = null;
            ErrorHandler.Try("Quick-insert sound", () => Game1.playSound("Ship"));
        }
        catch (Exception ex)
        {
            if (snapshot != null)
            {
                ErrorHandler.Try("Restore quick-insert slots", snapshot.Restore);
                if (source != null) { source.Stack = count; inventory.actualInventory[index] = source; }
            }
            Failed("Quick-insert inventory stack", ex);
        }
        finally { if (!cleaned) ErrorHandler.Try("Refresh breeding controls", RefreshEligibility); }
    }
    private bool SettingCompanion => Breeder.CompanionMode(machine);
    private bool RemovingTrait => Breeder.RemoveMode(machine);
    private string[] RemovalTraits => removalTraits;
    private string? SelectedTrait => removalTraits.Length > 0 ? removalTraits[selectedTrait % removalTraits.Length] : null;
    private string ModeHint => RemovingTrait ? "Insert 1 seed, choose a trait, then select Remove." : SettingCompanion ? "1 Companion seed + 1 chosen crop" : BreedingHint;
    private static string BreedingHint
    {
        get
        {
            int cost = Breeder.CropsRequired;
            return cost == 1 ? "1 donor crop + 1 matching seed = 1 bred seed"
                : $"{cost} donor crops + 1 matching seed = 1 bred seed";
        }
    }
    private bool CanBreed => canBreed;
    private static string? Metadata(Item? item, string key) => item != null && item.modData.TryGetValue(key, out string value) ? value : null;
    private void RefreshEligibility()
    {
        Item? donor = machine.heldObject.Value;
        var state = new EligibilityState(donor, donor?.Stack ?? 0, Metadata(donor, Traits.Key), Metadata(donor, Companion.Key),
            seeds, seeds?.Stack ?? 0, Metadata(seeds, Traits.Key), Metadata(seeds, Companion.Key), machine.readyForHarvest.Value,
            SettingCompanion, RemovingTrait, selectedTrait, ModEntry.Instance.Config.MaximumTraits, Breeder.CropsRequired, Companion.Revision);
        if (lastEligibility == state) return;
        removalTraits = donor != null && !state.Ready ? Traits.Read(donor.modData) : [];
        selectedTraitLabel = SelectedTrait is string trait ? TraitRules.Label(trait) : "No trait selected";
        canBreed = StationStacks.CanProcess(machine, seeds) && (!state.RemoveMode || SelectedTrait != null);
        donorName = donor?.DisplayName ?? "Empty";
        seedName = seeds?.DisplayName ?? "Empty";
        donorTraits = Summary(donor);
        seedTraits = Summary(seeds);
        donorLabel = state.Ready ? "Collect seed" : state.RemoveMode ? "Seed (1)"
            : state.CompanionMode ? "Companion seed (1)" : $"Donor crops ({state.Cost})";
        seedLabel = state.RemoveMode ? "Trait to remove" : state.CompanionMode ? "Chosen crop (1)" : "Matching seed (1)";
        currentModeHint = ModeHint;
        costHint = state.RemoveMode ? "Remove one trait for free" : state.CompanionMode ? "1 seed + 1 crop" : $"1 seed + {state.Cost} crop{(state.Cost == 1 ? "" : "s")} = 1 seed";
        if (lastEligibility is EligibilityState previous && previous.Cost != state.Cost)
            message = ModeHint;
        lastEligibility = state;
    }

    private static string Summary(Item? item)
    {
        if (item == null) return "Select from inventory";
        string[] traits = Traits.Read(item.modData);
        return traits.Length == 0 ? "No traits" : traits.Length == 1 ? TraitRules.Label(traits[0]) : $"{traits.Length} traits";
    }

    private void ChangeMode(int mode)
    {
        int current = RemovingTrait ? 2 : SettingCompanion ? 1 : 0;
        if (mode == current) return;
        if (machine.heldObject.Value != null || seeds != null || heldItem != null)
        {
            message = "Empty both slots and put down your held item to change mode.";
            return;
        }
        machine.modData.Remove(Breeder.ModeKey);
        machine.modData.Remove(Breeder.RemoveModeKey);
        if (mode == 1) machine.modData[Breeder.ModeKey] = "true";
        if (mode == 2) machine.modData[Breeder.RemoveModeKey] = "true";
        selectedTrait = 0;
        message = ModeHint;
        Layout(mode == 0 ? 1003 : mode == 1 ? 1005 : 1006);
        Game1.playSound("smallSelect");
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        InputSnapshot? snapshot = null;
        Item? surplus = null;
        try
        {
            if (!Present || !mutex.IsLockHeld()) return;
            RefreshEligibility();
            if (upperRightCloseButton?.containsPoint(x, y) == true)
            {
                exitThisMenu();
                return;
            }
            if (previousPage.visible && previousPage.containsPoint(x, y)) { ChangePage(-1); return; }
            if (nextPage.visible && nextPage.containsPoint(x, y)) { ChangePage(1); return; }
            bool modeClick = modeButton.containsPoint(x, y) || companionButton.containsPoint(x, y) || removalButton.containsPoint(x, y);
            if (modeClick || donorSlot.containsPoint(x, y) || seedSlot.containsPoint(x, y) || breedButton.containsPoint(x, y))
                snapshot = new InputSnapshot(this);
            if (modeClick)
            {
                ChangeMode(modeButton.containsPoint(x, y) ? 0 : companionButton.containsPoint(x, y) ? 1 : 2);
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
                    heldItem.Stack -= SettingCompanion || RemovingTrait ? 1 : Breeder.CropsRequired;
                    selectedTrait = 0;
                    if (heldItem.Stack == 0) heldItem = null;
                    Game1.playSound("Ship");
                }
                else message = RemovingTrait ? "Put a seed with traits in the left slot." : SettingCompanion ? "Put a seed with Companion in the left slot." : $"Put at least {Breeder.CropsRequired} matching trait crops in the donor slot.";
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
                    if (CanBreed && SelectedTrait is string trait && StationStacks.Process(machine, null, Core.TraitRules.Id(trait), out surplus))
                    {
                        snapshot = null;
                        message = "Trait removed. Collect your seed from the left slot.";
                        Game1.playSound("coin");
                    }
                    else message = "Insert a seed with traits and choose a trait to remove.";
                    return;
                }
                if (CanBreed && StationStacks.Process(machine, seeds, null, out surplus))
                {
                    seeds!.Stack -= Breeder.SeedsRequired;
                    if (seeds.Stack == 0) seeds = null;
                    snapshot = null;
                    message = "Ready! Collect your bred seed from the left slot.";
                    Game1.playSound("coin");
                }
                else message = SettingCompanion ? "Choose a different eligible crop; a seed cannot use its own crop as Companion." : $"Need {Breeder.CropsRequired} donor crops and 1 compatible seed.";
                return;
            }
            base.receiveLeftClick(x, y, playSound);
        }
        catch (Exception ex)
        {
            if (snapshot != null)
            {
                surplus = null; // Rolled-back input already contains these items.
                ErrorHandler.Try("Restore breeding inputs", snapshot.Restore);
            }
            Failed("Breeding input", ex);
        }
        finally
        {
            ReturnItem(surplus);
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
            if (observedMaxItems != Game1.player.MaxItems || inventory.inventory.Count != backpack.Capacity)
                Layout();
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
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .5f);
            Panel(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
            Text(b, "Crop Breeding", Bounds(32, 22, 420, 42), Game1.dialogueFont);
            Text(b, costHint, Bounds(460, 28, 452, 32), color: Muted, textScale: .82f);
            Button(b, modeButton, "Breeding", selected: !SettingCompanion && !RemovingTrait);
            Button(b, companionButton, "Set Companion", selected: SettingCompanion);
            Button(b, removalButton, "Remove Trait", selected: RemovingTrait);

            Panel(b, Bounds(28, 144, 316, 136), .6f);
            Panel(b, Bounds(364, 144, 316, 136), .6f);
            Panel(b, Bounds(700, 144, 280, 136), .6f);
            Text(b, donorLabel, Bounds(48, 156, 276, 30), color: machine.readyForHarvest.Value ? Green : Muted, textScale: .82f);
            Slot(b, donorSlot);
            Text(b, donorName, Bounds(128, 196, 196, 26), textScale: .86f);
            Text(b, donorTraits, Bounds(128, 226, 196, 36), color: Muted, textScale: .72f);
            Text(b, seedLabel, Bounds(384, 156, 276, 30), color: Muted, textScale: .82f);
            if (RemovingTrait)
                Button(b, seedSlot, selectedTraitLabel, enabled: RemovalTraits.Length > 0);
            else
            {
                Slot(b, seedSlot);
                Text(b, seedName, Bounds(464, 196, 196, 26), textScale: .86f);
                Text(b, seedTraits, Bounds(464, 226, 196, 36), color: Muted, textScale: .72f);
            }
            Text(b, machine.readyForHarvest.Value ? "Seed ready" : CanBreed ? "Ready" : "Add ingredients",
                Bounds(720, 156, 240, 30), color: CanBreed || machine.readyForHarvest.Value ? Green : Muted, textScale: .85f);
            Button(b, breedButton, RemovingTrait ? "Remove trait" : SettingCompanion ? "Set companion" : "Breed", enabled: CanBreed);
            string status = message == currentModeHint ? RemovingTrait ? "Select the trait box to cycle through traits."
                : "Select an inventory item, then place it in an input slot." : message;
            if (lastMessage != status)
            {
                wrappedMessage = Game1.parseText(status, Game1.smallFont, 1180);
                lastMessage = status;
            }
            Text(b, wrappedMessage, Bounds(40, 288, 928, 36), color: Muted, textScale: .78f);

            Panel(b, Bounds(28, 332, 952, geometry.InventoryBottom - 332), .6f);
            Text(b, "Inventory", Bounds(48, 340, 360, 32), textScale: .9f);
            if (backpack.PageCount > 1)
            {
                Button(b, previousPage, "<", enabled: inventoryPage > 0);
                Text(b, pageLabel, Bounds(848, 340, 68, 32), centered: true, textScale: .8f);
                Button(b, nextPage, ">", enabled: inventoryPage + 1 < backpack.PageCount);
            }
            DrawInventory(b);
            b.Draw(Game1.staminaRect, Bounds(32, geometry.InventoryBottom + 16, 944, 2), new Color(151, 83, 30));
            string controls = Game1.options.gamepadControls
                ? backpack.PageCount > 1 ? "D-pad: select  |  A: pick / place  |  X: quick insert  |  LB/RB: page  |  B: close"
                    : "D-pad: select  |  A: pick / place  |  X: quick insert  |  B: close"
                : backpack.PageCount > 1 ? "Left-click: pick / place  |  Right-click: split stack  |  Scroll: page  |  Esc: close"
                    : "Left-click: pick / place  |  Right-click: split stack  |  Esc: close";
            Text(b, controls, Bounds(32, geometry.InventoryBottom + 28, 944, 24), color: Muted, textScale: .72f);
            if (upperRightCloseButton != null)
                b.Draw(upperRightCloseButton.texture, upperRightCloseButton.bounds, upperRightCloseButton.sourceRect, Color.White);
            DrawItems(b);
            if (hover != null && heldItem == null) drawToolTip(b, hover.getDescription(), hover.DisplayName, hover);
            drawMouse(b);
        }
        catch (Exception ex) { Failed("draw", ex); }
    }
    private void Panel(SpriteBatch b, Rectangle area, float borderScale = 1f, Color? tint = null) =>
        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), area.X, area.Y, area.Width, area.Height,
            tint ?? Color.White, borderScale * geometry.Scale, false);

    private void Button(SpriteBatch b, ClickableComponent button, string label, bool selected = false, bool enabled = true)
    {
        Rectangle area = button.bounds;
        bool focused = Game1.options.SnappyMenus && currentlySnappedComponent == button
            || button.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY());
        drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), area.X, area.Y, area.Width, area.Height,
            !enabled ? Color.LightGray : selected || focused ? Color.Wheat : Color.White, 3 * geometry.Scale, false);
        int inset = geometry.Size(12);
        Text(b, label, new Rectangle(area.X + inset, area.Y, area.Width - 2 * inset, area.Height),
            color: enabled ? Ink : Muted, centered: true, textScale: .85f);
    }
    private void Text(SpriteBatch b, string text, Rectangle area, SpriteFont? font = null, Color? color = null,
        bool centered = false, float textScale = 1f)
    {
        font ??= Game1.smallFont;
        Vector2 size = font.MeasureString(text);
        float scale = Math.Min(geometry.Scale * textScale, Math.Min(area.Width / Math.Max(1f, size.X), area.Height / Math.Max(1f, size.Y)));
        b.DrawString(font, text, new Vector2(area.X + (centered ? (area.Width - size.X * scale) / 2 : 0), area.Y + (area.Height - size.Y * scale) / 2), color ?? Ink,
            0, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }
    private void Slot(SpriteBatch b, ClickableComponent slot)
    {
        bool focused = Game1.options.SnappyMenus && currentlySnappedComponent == slot
            || slot.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY());
        int inset = geometry.Size(4);
        Panel(b, new Rectangle(slot.bounds.X - inset, slot.bounds.Y - inset, slot.bounds.Width + 2 * inset, slot.bounds.Height + 2 * inset),
            .4f, focused ? new Color(255, 247, 190) : Color.White);
    }
    private void DrawInventory(SpriteBatch b)
    {
        // Vanilla InventoryMenu uses fixed 64px drawing coordinates. Keep its item handling,
        // but draw using the very same scaled rectangles as pointer/controller hit testing.
        Rectangle background = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
        Rectangle locked = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57);
        for (int i = backpack.Start; i < backpack.End; i++)
        {
            Rectangle bounds = inventory.inventory[i].bounds;
            bool focused = Game1.options.SnappyMenus && currentlySnappedComponent == inventory.inventory[i]
                || bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());
            b.Draw(Game1.menuTexture, bounds, background, focused ? Color.Wheat : Color.White);
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
            for (int i = backpack.Start; i < backpack.End && i < inventory.actualInventory.Count && i < Game1.player.MaxItems; i++)
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
