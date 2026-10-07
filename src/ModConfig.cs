namespace CropBreeding;

public sealed class ModConfig
{
    public double MutationChance { get; set; } = 0.05;
    public int MaximumTraits { get; set; } = 3;
    public int BreedingCost { get; set; } = 3;
    public bool EnableRegrowingCropMutations { get; set; } = true;
    public bool ShowErrorsInChat { get; set; } = true;

    public void Normalize()
    {
        MaximumTraits = Math.Clamp(MaximumTraits, 0, Core.TraitRules.Known.Length);
        BreedingCost = Math.Clamp(BreedingCost, 1, 10);
        MutationChance = double.IsFinite(MutationChance) ? Math.Clamp(MutationChance, 0, 1) : .05;
    }
}
