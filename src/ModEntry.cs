using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;

namespace CropBreeding;

public sealed class ModEntry : Mod
{
    internal const string Id = "KhoaNguyen0497.CropBreeding";
    internal static ModEntry Instance = null!;
    internal ModConfig Config = new();
    private Harmony harmony = null!;

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        ErrorHandler.Try("Load settings", () =>
        {
            var loaded = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            loaded.Normalize();
            Config = loaded;
        });
        harmony = new Harmony(Id);
        Patches.Apply(harmony);
        ErrorHandler.Try("Register research input rule", () =>
            GameStateQuery.Register(ResearchMachine.InputQuery, (_, context) => ResearchMachine.CanAccept(context.InputItem)));
        helper.Events.Input.ButtonPressed += (_, e) => ErrorHandler.Try("Controller breeding menu input", () =>
        {
            if (e.Button == SButton.ControllerX && Game1.activeClickableMenu is UI.BreedingMenu station)
            {
                // Stop vanilla from also splitting/picking up an item with this X press.
                helper.Input.Suppress(e.Button);
                station.QuickInsertSelected();
            }
            if (e.Button == SButton.ControllerB && Game1.activeClickableMenu is UI.BreedingMenu menu)
            {
                // Consume B before vanilla can also interpret the same press as opening inventory.
                helper.Input.Suppress(e.Button);
                menu.exitThisMenu();
            }
        });
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            UI.TraitBadge.Load();
            ErrorHandler.Try("Register inventory trait badge", () => UI.TraitBadge.Register(harmony));
            ErrorHandler.Try("Register Better Junimos planting", () => Integrations.BetterJunimosIntegration.Register(harmony));
            ErrorHandler.Try("Register Better Junimos fertilizing", () => Integrations.BetterJunimosFertilizerIntegration.Register(harmony));
            ErrorHandler.Try("Register Crop Harvest Bubbles", () => Integrations.CropHarvestBubblesIntegration.Register(harmony));
            ErrorHandler.Try("Register Lookup Anything", () => Integrations.LookupAnythingIntegration.Register(harmony, Monitor));
            ErrorHandler.Try("Register config menu", Integrations.GenericModConfigMenuIntegration.Register);
        };
        helper.Events.Content.AssetRequested += (sender, e) => ErrorHandler.Try("Load breeding assets", () => AssetRequested(sender, e));
        helper.Events.Content.AssetsInvalidated += (_, e) => ErrorHandler.Try("Invalidate crop cache", () =>
        {
            if (e.NamesWithoutLocale.Any(n => n.IsEquivalentTo("Data/Crops"))) Companion.Invalidate();
        });
        helper.Events.GameLoop.SaveLoaded += (_, _) => ErrorHandler.Try("Unlock recipe", Unlock);
        helper.Events.GameLoop.DayStarted += (_, _) => ErrorHandler.Try("Unlock recipe", Unlock);
        helper.ConsoleCommands.Add("cropbreeding_give", "Give one Breeding Machine.", (_, _) => ErrorHandler.Try("Give machine", () =>
        {
            if (Context.IsWorldReady) Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)" + Breeder.MachineId));
        }));
        helper.ConsoleCommands.Add("cropbreeding_cleanup", "Remove breeding traits and machines before uninstalling. Save afterwards.", (_, _) => ErrorHandler.Try("Cleanup", Cleanup));
        helper.ConsoleCommands.Add("cropbreeding_give_research", "Give one Research Machine.", (_, _) => ErrorHandler.Try("Give research machine", () =>
        {
            if (Context.IsWorldReady) Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)" + ResearchMachine.MachineId));
        }));
        helper.ConsoleCommands.Add("cropbreeding_catalog", "List supported seed IDs and their exact harvest IDs.", (_, _) => ErrorHandler.Try("List crop catalog", () =>
        {
            if (Context.IsWorldReady)
                foreach (var pair in CropCatalog.Data.Where(p => CropCatalog.EligibleSeed(p.Key)))
                    Monitor.Log($"{pair.Key} -> {pair.Value.HarvestItemId}", LogLevel.Info);
        }));
        helper.ConsoleCommands.Add("cropbreeding_research_check", "Select a Researcher seed and face the Research Machine. Log input, machine and callback checks without processing anything.",
            (_, _) => ErrorHandler.Try("Research diagnostics", ResearchDiagnostics.Run));
    }
    private void AssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(Id + "/BreedingMachine")
            || e.NameWithoutLocale.IsEquivalentTo(Id + "/ResearchMachine"))
            e.LoadFrom(() =>
            {
                try
                {
                    string file = e.NameWithoutLocale.IsEquivalentTo(Id + "/ResearchMachine")
                        ? "assets/research-machine.png" : "assets/breeding-machine.png";
                    return Helper.ModContent.Load<Texture2D>(file);
                }
                catch (Exception ex)
                {
                    ErrorHandler.Report("Load machine sprite", ex);
                    return Game1.bigCraftableSpriteSheet;
                }
            }, AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            e.Edit(asset => ErrorHandler.Try("Add breeding machine data", () =>
            {
                var data = asset.AsDictionary<string, BigCraftableData>().Data;
                data[Breeder.MachineId] = new BigCraftableData
                {
                    Name = "Breeding Machine", DisplayName = "Breeding Machine",
                    Description = "Breed trait crops with matching seeds. Equal input amounts follow the Breeding cost setting. Combine traits at any level; shared levels add up to their maximum. Plain seeds copy traits. Plain crops and outputs above the trait cap are rejected. Breaking it loses its contents.",
                    Texture = Id + "/BreedingMachine", SpriteIndex = 0,
                    CanBePlacedIndoors = true, CanBePlacedOutdoors = true, Fragility = 0, Price = 0
                };
                data[ResearchMachine.MachineId] = new BigCraftableData
                {
                    Name = "Research Machine", DisplayName = "Research Machine",
                    Description = "Consumes one Researcher seed. Replaces Researcher with two successful trait rolls in 2 in-game hours. Breaking it loses its contents.",
                    Texture = Id + "/ResearchMachine", SpriteIndex = 0,
                    CanBePlacedIndoors = true, CanBePlacedOutdoors = true, Fragility = 0, Price = 0
                };
            }));
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            e.Edit(asset => ErrorHandler.Try("Add research machine rules", () =>
                asset.AsDictionary<string, MachineData>().Data["(BC)" + ResearchMachine.MachineId] = ResearchMachine.CreateData()));
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            e.Edit(asset => ErrorHandler.Try("Add breeding recipes", () =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                data[Breeder.MachineId] = $"388 50 335 5 787 1/Home/{Breeder.MachineId}/true/Farming 5/Breeding Machine";
                data[ResearchMachine.MachineId] = $"388 50 335 5 787 1/Home/{ResearchMachine.MachineId}/true/Farming 5/Research Machine";
            }));
    }
    private static void Unlock()
    {
        if (Context.IsWorldReady && Game1.player.FarmingLevel >= 5)
        {
            Game1.player.craftingRecipes.TryAdd(Breeder.MachineId, 0);
            Game1.player.craftingRecipes.TryAdd(ResearchMachine.MachineId, 0);
        }
    }

    private void Cleanup()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer) { Monitor.Log("Load the save as host first.", LogLevel.Warn); return; }
        Utility.ForEachItem(item =>
        {
            item.modData.Remove(Traits.Key);
            item.modData.Remove(Companion.Key);
            if (item is StardewValley.Object machine && (Breeder.IsMachine(machine) || ResearchMachine.IsMachine(machine))) Breeder.Clear(machine);
            return true;
        });
        Utility.ForEachLocation(location =>
        {
            foreach (var feature in location.terrainFeatures.Values.OfType<HoeDirtAlias>())
                if (feature.crop is Crop crop)
                {
                    crop.modData.Remove(Traits.Key);
                    crop.modData.Remove(Companion.Key);
                    crop.modData.Remove(Traits.EligibilityKey);
                    crop.modData.Remove(MutationState.Key);
                    feature.applySpeedIncreases(Game1.MasterPlayer);
                }
            foreach (var tile in location.objects.Pairs.Where(p => Breeder.IsMachine(p.Value) || ResearchMachine.IsMachine(p.Value)).Select(p => p.Key).ToArray())
                location.objects.Remove(tile);
            return true;
        });
        // Remove machine items from all inventories (including nested inventories) through the official traversal.
        Utility.ForEachItemContext((in StardewValley.Internal.ForEachItemContext context) =>
        {
            if (context.Item is StardewValley.Object machine && (Breeder.IsMachine(machine) || ResearchMachine.IsMachine(machine))) context.RemoveItem();
            return true;
        });
        foreach (Farmer farmer in Game1.getAllFarmers())
        {
            farmer.craftingRecipes.Remove(Breeder.MachineId);
            farmer.craftingRecipes.Remove(ResearchMachine.MachineId);
        }
        Monitor.Log("Breeding data and machines removed. Save, quit, then remove the mod.", LogLevel.Info);
    }
}
