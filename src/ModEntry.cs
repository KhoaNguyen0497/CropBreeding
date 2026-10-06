using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

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
        Config = helper.ReadConfig<ModConfig>();
        harmony = new Harmony(Id);
        Patches.Apply(harmony);
        helper.Events.GameLoop.GameLaunched += (_, _) => Integrations.LookupAnythingIntegration.Register(harmony, Monitor);
        helper.Events.Content.AssetRequested += AssetRequested;
        helper.Events.Content.AssetsInvalidated += (_, e) =>
        {
            if (e.NamesWithoutLocale.Any(n => n.IsEquivalentTo("Data/Crops"))) Companion.Invalidate();
        };
        helper.Events.GameLoop.SaveLoaded += (_, _) => Unlock();
        helper.Events.GameLoop.DayStarted += (_, _) => Unlock();
        helper.ConsoleCommands.Add("cropbreeding_give", "Give one Breeding Machine.", (_, _) =>
        {
            if (Context.IsWorldReady) Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)" + Breeder.MachineId));
        });
        helper.ConsoleCommands.Add("cropbreeding_cleanup", "Remove breeding traits and machines before uninstalling. Save afterwards.", (_, _) => Cleanup());
        helper.ConsoleCommands.Add("cropbreeding_catalog", "List supported seed IDs and their exact harvest IDs.", (_, _) =>
        {
            if (Context.IsWorldReady)
                foreach (var pair in CropCatalog.Data.Where(p => CropCatalog.EligibleSeed(p.Key)))
                    Monitor.Log($"{pair.Key} -> {pair.Value.HarvestItemId}", LogLevel.Info);
        });
    }
    private void AssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(Id + "/BreedingMachine"))
            e.LoadFromModFile<Texture2D>("assets/breeding-machine.png", AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BigCraftableData>().Data;
                data[Breeder.MachineId] = new BigCraftableData
                {
                    Name = "Breeding Machine", DisplayName = "Breeding Machine",
                    Description = "Open to add 5 trait crops and 5 matching seeds, then choose Breed. Each input must be a matching stack. Plain seeds copy traits; one level-1 trait can merge. Breaking it loses its contents.",
                    Texture = Id + "/BreedingMachine", SpriteIndex = 0,
                    CanBePlacedIndoors = true, CanBePlacedOutdoors = true, Fragility = 0, Price = 0
                };
            });
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            e.Edit(asset => asset.AsDictionary<string, string>().Data[Breeder.MachineId] =
                $"388 50 335 5 787 1/Home/{Breeder.MachineId}/true/Farming 5/Breeding Machine");
    }
    private static void Unlock()
    {
        if (Context.IsWorldReady && Game1.player.FarmingLevel >= 5) Game1.player.craftingRecipes.TryAdd(Breeder.MachineId, 0);
    }

    private void Cleanup()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer) { Monitor.Log("Load the save as host first.", LogLevel.Warn); return; }
        Utility.ForEachItem(item =>
        {
            item.modData.Remove(Traits.Key);
            item.modData.Remove(Companion.Key);
            if (item is StardewValley.Object machine && Breeder.IsMachine(machine)) Breeder.Clear(machine);
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
                    feature.applySpeedIncreases(Game1.MasterPlayer);
                }
            foreach (var tile in location.objects.Pairs.Where(p => Breeder.IsMachine(p.Value)).Select(p => p.Key).ToArray())
                location.objects.Remove(tile);
            return true;
        });
        // Remove machine items from all inventories (including nested inventories) through the official traversal.
        Utility.ForEachItemContext((in StardewValley.Internal.ForEachItemContext context) =>
        {
            if (context.Item is StardewValley.Object machine && Breeder.IsMachine(machine)) context.RemoveItem();
            return true;
        });
        foreach (Farmer farmer in Game1.getAllFarmers()) farmer.craftingRecipes.Remove(Breeder.MachineId);
        Monitor.Log("Breeding data and machines removed. Save, quit, then remove the mod.", LogLevel.Info);
    }
}
