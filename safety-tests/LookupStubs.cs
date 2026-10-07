// These fakes validate our runtime contract/field construction, not live Harmony detouring.
using System.Reflection;
using StardewValley;

namespace HarmonyLib
{
    public enum HarmonyPatchType { All }
    public sealed class HarmonyMethod(MethodInfo method) { public MethodInfo Method = method; }
    public sealed class Harmony
    {
        public string Id = "test";
        public MethodInfo? Postfix;
        public bool ThrowOnPatch, Unpatched;
        public readonly HashSet<MethodInfo> Installed = [];
        public void Patch(MethodBase target, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null)
        {
            foreach (var addition in new[] { prefix, postfix, transpiler, finalizer }) if (addition != null) Installed.Add(addition.Method);
            if (ThrowOnPatch) throw new Exception("injected patch error");
            Postfix = postfix?.Method;
        }
        public void Unpatch(MethodBase target, HarmonyPatchType type, string id) { Unpatched = true; Installed.Clear(); }
        public void Unpatch(MethodBase target, MethodInfo method) { Unpatched = true; Installed.Remove(method); }
    }
    public static class AccessTools
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public static MethodInfo Method(Type type, string name, Type[]? parameters = null)
            => (parameters == null ? type.GetMethod(name, Flags) : type.GetMethod(name, Flags, null, parameters, null))!;
        public static FieldInfo Field(Type type, string name) => type.GetField(name, Flags)!;
        public static ConstructorInfo Constructor(Type type, Type[] parameters) => type.GetConstructor(Flags, null, parameters, null)!;
    }
}
namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    public interface ICustomField { string Label { get; } string Value { get; } }
    public sealed class GenericField(string label, string? value, bool? hasValue = null) : ICustomField
    {
        public static bool Throw;
        public string Label { get; } = Throw ? throw new Exception("injected field error") : label;
        public string Value { get; } = value ?? "";
        public bool HasValue { get; } = hasValue ?? !string.IsNullOrEmpty(value);
    }
}
namespace CropBreeding
{
    internal sealed class LookupSubject(Item item, Crop? crop = null)
    {
        private readonly Item Target = item;
        private readonly Crop? FromCrop = crop;
        public IEnumerable<Pathoschild.Stardew.LookupAnything.Framework.Fields.ICustomField> GetData() => [];
    }
}
