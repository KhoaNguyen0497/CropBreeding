namespace CropBreeding;

public sealed class ModConfig
{
    public double MutationChance { get; set; } = 0.05;
    public int MaximumTraits { get; set; } = 3;
    public double GrowthReductionPerLevel { get; set; } = 0.05;
    public double ExtraYieldPerLevel { get; set; } = 0.20;
    public double CompanionChance { get; set; } = 0.20;
    public double QualityUpgradeChance { get; set; } = 0.05;
    public double SeedSaverChance { get; set; } = 0.10;
    public double RootedChance { get; set; } = 0.10;
    public double ResearcherMutationBonus { get; set; } = 0.05;
    public double ResearcherGrowthPenalty { get; set; } = 0.10;
    public bool EnableRegrowingCropMutations { get; set; } = true;
    public bool ShowErrorsInChat { get; set; } = false;

    public void Normalize()
    {
        MaximumTraits = Math.Clamp(MaximumTraits, 0, Core.TraitRules.Known.Length);
        MutationChance = Rate(MutationChance, .05);
        GrowthReductionPerLevel = Rate(GrowthReductionPerLevel, .05);
        ExtraYieldPerLevel = Rate(ExtraYieldPerLevel, .20);
        CompanionChance = Rate(CompanionChance, .20);
        QualityUpgradeChance = Rate(QualityUpgradeChance, .05);
        SeedSaverChance = Rate(SeedSaverChance, .10);
        RootedChance = Rate(RootedChance, .10);
        ResearcherMutationBonus = Rate(ResearcherMutationBonus, .05);
        ResearcherGrowthPenalty = Rate(ResearcherGrowthPenalty, .10);
    }
    private static double Rate(double value, double fallback) => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : fallback;
}
