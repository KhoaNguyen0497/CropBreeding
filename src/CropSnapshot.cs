using StardewValley;

namespace CropBreeding;

// Used only around actions which mutate a crop, never in per-frame/stacking checks.
internal sealed class CropSnapshot
{
    private readonly Crop crop;
    private readonly int[] phases;
    private readonly KeyValuePair<string, string>[] metadata;
    private readonly int phase, day, shown;
    private readonly bool grown, raised;
    private readonly string harvest;
    private readonly HoeDirtAlias? soil;
    private readonly int water, nearWater;

    internal CropSnapshot(Crop crop)
    {
        this.crop = crop;
        phases = crop.phaseDays.ToArray();
        metadata = crop.modData.Pairs.ToArray();
        phase = crop.currentPhase.Value; day = crop.dayOfCurrentPhase.Value; shown = crop.phaseToShow.Value;
        grown = crop.fullyGrown.Value; raised = crop.raisedSeeds.Value; harvest = crop.indexOfHarvest.Value;
        soil = crop.Dirt as HoeDirtAlias;
        water = soil?.state.Value ?? 0; nearWater = soil?.nearWaterForPaddy.Value ?? -1;
    }

    internal void Restore()
    {
        crop.phaseDays.Clear();
        foreach (int days in phases) crop.phaseDays.Add(days);
        crop.modData.Clear();
        foreach (var pair in metadata) crop.modData[pair.Key] = pair.Value;
        crop.currentPhase.Value = phase; crop.dayOfCurrentPhase.Value = day; crop.phaseToShow.Value = shown;
        crop.fullyGrown.Value = grown; crop.raisedSeeds.Value = raised; crop.indexOfHarvest.Value = harvest;
        if (soil != null) { soil.state.Value = water; soil.nearWaterForPaddy.Value = nearWater; crop.updateDrawMath(soil.Tile); }
    }
}
