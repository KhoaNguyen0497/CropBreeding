using HarmonyLib;
using StardewValley;
using StardewValley.Characters;

namespace CropBreeding;

internal static class JunimoHarvestOutput
{
    private static AccessTools.FieldRef<JunimoHarvester, Item?>? lastItem;

    internal static void Initialize()
    {
        // Resolve once, never reflect/scan on an update tick or per bonus item.
        try { lastItem = AccessTools.FieldRefAccess<JunimoHarvester, Item?>("lastItemHarvested"); }
        catch (Exception ex) { ErrorHandler.Report("Access Junimo harvested item", ex); }
    }

    internal static void DeliverExtras(JunimoHarvester junimo, List<Item> extras, Item? primary)
    {
        // If this game field is unavailable, leave vanilla delivery untouched and skip our
        // Junimo extras rather than letting a bar/seed overwrite the raisin target.
        if (lastItem == null) return;
        Item? remembered;
        try { remembered = primary ?? lastItem(junimo); }
        catch (Exception ex) { ErrorHandler.Report("Read Junimo harvested item", ex); return; }
        try
        {
            foreach (Item extra in extras)
            {
                try { junimo.tryToAddItemToHut(extra); }
                catch (Exception ex) { ErrorHandler.Report("Deliver harvest bonus", ex); }
            }
        }
        finally
        {
            // Vanilla update performs its own 20% raisin roll after Crop.harvest returns.
            // Its getOne() preserves metadata/color; it also explicitly copies quality.
            // Do not call harvest again or roll any trait effects for the duplicate.
            try { lastItem(junimo) = remembered; }
            catch (Exception ex) { ErrorHandler.Report("Restore Junimo harvested item", ex); }
        }
    }
}
