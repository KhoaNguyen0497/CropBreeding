// Minimal test doubles for the linked production code. These do not simulate gameplay.
global using HoeDirtAlias = StardewValley.HoeDirt;
using CropBreeding.Integrations;

namespace Microsoft.Xna.Framework
{
    public struct Color { public static Color OrangeRed => new(); }
}
namespace StardewModdingAPI
{
    public interface IManifest { }
    public enum LogLevel { Error }
    public static class Context { public static bool IsWorldReady = true; }
}
namespace StardewValley
{
    public sealed class Net<T>(T value) { public T Value = value; }
    public sealed class Metadata : Dictionary<string, string> { public IEnumerable<KeyValuePair<string, string>> Pairs => this; }
    public sealed class Crop
    {
        public List<int> phaseDays = [1, 2, 3, 99999];
        public Metadata modData = new();
        public Net<int> currentPhase = new(2), dayOfCurrentPhase = new(1), phaseToShow = new(-1);
        public Net<bool> fullyGrown = new(false), raisedSeeds = new(true);
        public Net<string> indexOfHarvest = new("24");
        public HoeDirt? Dirt;
        public int DrawUpdates;
        public void updateDrawMath(int tile) => DrawUpdates++;
    }
    public sealed class HoeDirt
    {
        public Net<int> state = new(1), nearWaterForPaddy = new(0);
        public int Tile;
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
    public static class Game1 { public static Chat? chatBox = new(); }
}
namespace CropBreeding
{
    public sealed class MonitorStub
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
        public static ModEntry Instance = new();
        public ModConfig Config = new();
        public MonitorStub Monitor = new();
        public HelperStub Helper = new();
        public Manifest ModManifest = new();
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
