using SObject = StardewValley.Object;

namespace CropBreeding;

internal static partial class Patches
{
    private static bool DropPrefix(SObject __instance, ref bool __result)
    {
        bool original = __result;
        try
        {
            if (!Breeder.IsMachine(__instance)) return true;
            // Both probes and actual item drops reject input. Vanilla then calls checkForAction
            // to open the menu. Returning true here would make it remove one active item.
            __result = false;
            return false;
        }
        catch (Exception ex)
        {
            __result = original;
            ErrorHandler.Report("DropPrefix", ex);
            return true;
        }
    }
}
