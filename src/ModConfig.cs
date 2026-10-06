namespace CropBreeding;

public sealed class ModConfig
{
    public double MutationChance { get; set; } = 0.05;
    public int MaximumTraits { get; set; } = 3;
    public double FastGrowthReduction { get; set; } = 0.10;
    public double ExtraYieldChance { get; set; } = 0.20;
    public double CompanionChance { get; set; } = 0.20;
    public double QualityUpgradeChance { get; set; } = 0.05;
    public double FastRegrowthReduction { get; set; } = 0.10;
    // Pending decisions are conservative switches, not settled breeding rules.
    public bool EnableRegrowingCropMutations { get; set; } = true;
    public bool EnableSeedMakerInheritance { get; set; } = false;
}
