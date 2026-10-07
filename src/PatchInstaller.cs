using System.Reflection;
using HarmonyLib;

namespace CropBreeding;

internal static class PatchInstaller
{
    internal static void Apply(Harmony harmony, Type patches, Type type, string name, string? prefix = null, string? postfix = null,
        string? finalizer = null, string? transpiler = null, Type[]? parameters = null)
    {
        MethodInfo? target = null;
        var additions = new List<MethodInfo>();
        try
        {
            target = AccessTools.Method(type, name, parameters) ?? throw new MissingMethodException(type.FullName, name);
            HarmonyMethod? Method(string? method)
            {
                if (method == null) return null;
                MethodInfo info = AccessTools.Method(patches, method) ?? throw new MissingMethodException(patches.FullName, method);
                additions.Add(info);
                return new HarmonyMethod(info);
            }
            harmony.Patch(target, Method(prefix), Method(postfix), Method(transpiler), Method(finalizer));
        }
        catch (Exception ex)
        {
            ErrorHandler.Report($"Patch {type.Name}.{name}", ex);
            // The same vanilla method may already have other successful patches from us.
            // Roll back only this call's additions, never all patches owned by the mod.
            if (target != null)
                foreach (MethodInfo addition in additions)
                    ErrorHandler.Try("Remove incomplete patch", () => harmony.Unpatch(target, addition));
        }
    }
}
