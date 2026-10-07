// Minimal test doubles for the linked production code. These do not simulate gameplay.
global using HoeDirtAlias = StardewValley.HoeDirt;
using CropBreeding.Integrations;
using StardewValley;

namespace Microsoft.Xna.Framework
{
    public struct Color { public static Color OrangeRed => new(); }
    public readonly record struct Vector2(float X, float Y)
    {
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    }
}
namespace StardewModdingAPI
{
    public interface IManifest { }
    public enum LogLevel { Error, Warn }
    public interface IMonitor { void Log(string message, LogLevel level); }
    public static class Context { public static bool IsWorldReady = true; }
}
namespace StardewValley
{
    public sealed class Net<T>(T value) { public T Value = value; }
    public sealed class Metadata : Dictionary<string, string> { public IEnumerable<KeyValuePair<string, string>> Pairs => this; }
    public class Item
    {
        public Metadata modData = new();
        public string ItemId = "24";
        public int Stack = 1, Quality;
        public virtual Item getOne()
        {
            var copy = new Item { ItemId = ItemId, Quality = Quality };
            foreach (var pair in modData) copy.modData[pair.Key] = pair.Value;
            return copy;
        }
    }
    public sealed class Object : Item { public Microsoft.Xna.Framework.Vector2 TileLocation; }
    public static class ItemRegistry
    {
        public static Func<string, int, int, Item>? Factory;
        public static Item Create(string id, int stack, int quality) => Factory?.Invoke(id, stack, quality)
            ?? new Item { ItemId = id.Replace("(O)", ""), Stack = stack, Quality = quality };
    }
    public sealed class GameLocation
    {
        public string NameOrUniqueName = "Farm";
        public Dictionary<Microsoft.Xna.Framework.Vector2, object> terrainFeatures = new();
    }
    public sealed class Farmer { public FarmerTeam team = new(); }
    public sealed class FarmerTeam
    {
        public Dictionary<string, Network.NetMutex> globalInventoryMutexes = new();
        public Network.NetMutex GetOrCreateGlobalInventoryMutex(string key)
        {
            if (!globalInventoryMutexes.TryGetValue(key, out var mutex)) globalInventoryMutexes[key] = mutex = new();
            return mutex;
        }
    }
    public sealed class Crop
    {
        public List<int> phaseDays = [1, 2, 3, 99999];
        public Metadata modData = new();
        public Net<int> currentPhase = new(2), dayOfCurrentPhase = new(1), phaseToShow = new(-1);
        public Net<bool> fullyGrown = new(false), raisedSeeds = new(true);
        public Net<bool> dead = new(false);
        public Net<string> indexOfHarvest = new("24"), netSeedIndex = new("472");
        public GameLocation currentLocation = new();
        public CropData? Data = new();
        public bool Eligible = true;
        public CropData? GetData() => Data;
        public HoeDirt? Dirt;
        public int DrawUpdates;
        public void updateDrawMath(Microsoft.Xna.Framework.Vector2 tile) => DrawUpdates++;
        public void ResetPhaseDays() { phaseDays = Data!.DaysInPhase.Append(99999).ToList(); }
    }
    public sealed class HoeDirt
    {
        public Crop? crop;
        public Net<int> state = new(1), nearWaterForPaddy = new(0);
        public Microsoft.Xna.Framework.Vector2 Tile;
        public void applySpeedIncreases(Farmer who) { }
        public bool hasPaddyCrop() => false;
        public bool paddyWaterCheck() => false;
        public void updateNeighbors() { }
    }
    public sealed class Chat
    {
        public List<string> Messages = [];
        public bool Throw;
        public void addMessage(string message, Microsoft.Xna.Framework.Color color)
        {
            if (Throw) throw new Exception("chat unavailable");
            Messages.Add(message);
        }
    }
    public sealed class CropData
    {
        public int RegrowDays = -1;
        public List<int> DaysInPhase = [5, 5, 5, 5, 5];
        public string HarvestItemId = "24";
        public bool IsRaised;
    }
    public static class Game1
    {
        public static Chat? chatBox = new();
        public static Farmer player = new();
        public static bool IsMasterGame = true;
        public static Dictionary<string, object> objectData = new();
    }
}
namespace CropBreeding
{
    public sealed class MonitorStub : StardewModdingAPI.IMonitor
    {
        public List<string> Messages = [];
        public bool Throw;
        public void Log(string message, StardewModdingAPI.LogLevel level)
        {
            if (Throw) throw new Exception("logger unavailable");
            Messages.Add(message);
        }
    }
    public sealed class Manifest : StardewModdingAPI.IManifest { }
    public sealed class Registry
    {
        public object? Api;
        public T? GetApi<T>(string id) where T : class => Api as T;
    }
    public sealed class HelperStub
    {
        public Registry ModRegistry = new();
        public EventsStub Events = new();
        public int Saves;
        public bool Throw;
        public void WriteConfig(ModConfig config)
        {
            if (Throw) throw new Exception("disk unavailable");
            Saves++;
        }
    }
    public sealed class ModEntry
    {
        public const string Id = "CropBreeding.Tests";
        public static ModEntry Instance = new();
        public ModConfig Config = new();
        public MonitorStub Monitor = new();
        public HelperStub Helper = new();
        public Manifest ModManifest = new();
    }
    internal static class Traits
    {
        internal const string Key = "Traits";
        internal static Func<Random> RandomFactory = () => new Random(0);
        internal static int RandomCalls;
        internal static Func<double, Random>? SaltRandomFactory;
        internal static Random RandomFor(Crop crop, double salt) { RandomCalls++; return SaltRandomFactory?.Invoke(salt) ?? RandomFactory(); }
        internal static string[] Read(Metadata data) => Core.TraitRules.Parse(data.GetValueOrDefault(Key));
        internal static bool Eligible(Crop crop, HoeDirt soil) => crop.Eligible;
        internal static void Write(Metadata data, IEnumerable<string> values) => data[Key] = Core.TraitRules.Encode(values);
        internal static int Level(Metadata data, string id) => Core.TraitRules.Level(Read(data), id);
        internal static double GrowthPenalty(Metadata data) => 0;
    }
    internal static class Companion
    {
        internal const string Key = "Companion";
        internal static bool ThrowBaseDays;
        internal static void Write(Metadata data, string? id) { if (id == null) data.Remove(Key); else data[Key] = id; }
        internal static int BaseDays(Metadata data) => ThrowBaseDays ? throw new Exception("injected Companion error") : 6;
        internal static void RemoveGrowthDelay(HoeDirt soil) { }
        internal static string? Read(Metadata data) => data.GetValueOrDefault("Companion");
        internal static string Label(Metadata data) => Read(data) ?? "unassigned";
    }
    public sealed class ConfigApi : IGenericModConfigMenuApi
    {
        public Dictionary<string, (Func<int> Get, Action<int> Set)> Numbers = new();
        public Dictionary<string, (Func<bool> Get, Action<bool> Set)> Booleans = new();
        public Action Reset = null!, Save = null!;
        public bool Unregistered, FailOptions;
        public void Register(StardewModdingAPI.IManifest mod, Action reset, Action save, bool titleScreenOnly = false) { Reset = reset; Save = save; }
        public void Unregister(StardewModdingAPI.IManifest mod) => Unregistered = true;
        public void AddSectionTitle(StardewModdingAPI.IManifest mod, Func<string> text, Func<string>? tooltip = null) { }
        public void AddBoolOption(StardewModdingAPI.IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null)
            => Booleans.Add(fieldId!, (getValue, setValue));
        public void AddNumberOption(StardewModdingAPI.IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null,
            int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null)
        {
            if (FailOptions) throw new Exception("API registration failure");
            Numbers.Add(fieldId!, (getValue, setValue));
        }
    }
}

namespace StardewValley.TerrainFeatures
{
    public sealed class Tree
    {
        public const int treeStage = 5;
        public StardewValley.Net<bool> stump = new(false);
        public StardewValley.Net<float> health = new(10);
        public StardewValley.Net<int> growthStage = new(0);
    }
}
namespace StardewValley.Characters
{
    public sealed class JunimoHarvester
    {
        public Item? LastItem;
        public readonly List<Item> Delivered = [];
        public string? FailItem;
        public void tryToAddItemToHut(Item item)
        {
            LastItem = item;
            if (item.ItemId == FailItem) throw new Exception("injected hut delivery failure");
            Delivered.Add(item);
        }
    }
}
namespace StardewValley.Network
{
    public sealed class NetMutex
    {
        public bool Held, OtherOwner;
        public bool IsLocked() => Held || OtherOwner;
        public bool IsLockHeld() => Held;
        public void RequestLock(Action acquired, Action failed)
        {
            if (OtherOwner) failed(); else { Held = true; acquired(); }
        }
        public void ReleaseLock() => Held = false;
    }
}
namespace StardewModdingAPI.Events { public sealed class UpdateTickedEventArgs : EventArgs { } }
namespace CropBreeding
{
    internal static class CropCatalog { internal static string Raw(string id) => id.Replace("(O)", ""); }
    public sealed class EventsStub { public GameLoopStub GameLoop = new(); }
    public sealed class GameLoopStub
    {
        public event EventHandler<StardewModdingAPI.Events.UpdateTickedEventArgs>? UpdateTicked;
        public int Subscribers => UpdateTicked?.GetInvocationList().Length ?? 0;
        public void Tick() => UpdateTicked?.Invoke(this, new());
    }
}
