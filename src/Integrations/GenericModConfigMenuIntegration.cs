using StardewModdingAPI;

namespace CropBreeding.Integrations;

// Minimal public API contract from spacechase0's Generic Mod Config Menu.
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void Unregister(IManifest mod);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null,
        int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
}

internal static class GenericModConfigMenuIntegration
{
    internal static void Register()
    {
        var mod = ModEntry.Instance;
        var api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api == null) return;
        try
        {
            api.Register(mod.ModManifest, () => mod.Config = new ModConfig(),
                () => ErrorHandler.Try("Save settings", () => { mod.Config.Normalize(); mod.Helper.WriteConfig(mod.Config); }));
            api.AddSectionTitle(mod.ModManifest, () => "Breeding and mutations");
            api.AddNumberOption(mod.ModManifest, () => mod.Config.MaximumTraits, value => mod.Config.MaximumTraits = Math.Clamp(value, 0, Core.TraitRules.Known.Length),
                () => "Maximum traits", () => "Unique traits per seed or crop. Zero prevents adding new traits. Lowering this does not remove existing traits.", 0, Core.TraitRules.Known.Length, 1, fieldId: nameof(ModConfig.MaximumTraits));
            Percent(nameof(ModConfig.MutationChance), "Mutation chance", "Chance per plant harvest before Researcher.", c => c.MutationChance, (c, v) => c.MutationChance = v);
            api.AddBoolOption(mod.ModManifest, () => mod.Config.EnableRegrowingCropMutations, value => mod.Config.EnableRegrowingCropMutations = value,
                () => "Mutations on regrowing crops", fieldId: nameof(ModConfig.EnableRegrowingCropMutations));
            api.AddSectionTitle(mod.ModManifest, () => "Trait effects per level");
            Percent(nameof(ModConfig.GrowthReductionPerLevel), "Fast Growth reduction", "Applies to initial growth and natural regrowth after other modifiers. Existing planted growth is updated at its next normal recalculation, not scanned when saving settings.", c => c.GrowthReductionPerLevel, (c, v) => c.GrowthReductionPerLevel = v);
            Percent(nameof(ModConfig.ExtraYieldPerLevel), "High Yield bonus", "Proportional extra produce, with fractional remainder chance. Total bonus capped at 100%.", c => c.ExtraYieldPerLevel, (c, v) => c.ExtraYieldPerLevel = v);
            Percent(nameof(ModConfig.CompanionChance), "Companion chance", "Chance for one companion crop per harvest.", c => c.CompanionChance, (c, v) => c.CompanionChance = v);
            Percent(nameof(ModConfig.QualityUpgradeChance), "High Quality chance", "Chance per item to upgrade one quality tier.", c => c.QualityUpgradeChance, (c, v) => c.QualityUpgradeChance = v);
            Percent(nameof(ModConfig.SeedSaverChance), "Seed Saver chance", "Chance to return one seed with the parent's traits.", c => c.SeedSaverChance, (c, v) => c.SeedSaverChance = v);
            Percent(nameof(ModConfig.RootedChance), "Rooted chance", "Chance to restart a single-harvest crop.", c => c.RootedChance, (c, v) => c.RootedChance = v);
            Percent(nameof(ModConfig.ResearcherMutationBonus), "Researcher mutation bonus", "Percentage points added to mutation chance.", c => c.ResearcherMutationBonus, (c, v) => c.ResearcherMutationBonus = v);
            Percent(nameof(ModConfig.ResearcherGrowthPenalty), "Researcher growth penalty", "Extra initial growth and regrowth time per level.", c => c.ResearcherGrowthPenalty, (c, v) => c.ResearcherGrowthPenalty = v);
            api.AddSectionTitle(mod.ModManifest, () => "Error reporting");
            api.AddBoolOption(mod.ModManifest, () => mod.Config.ShowErrorsInChat, value => mod.Config.ShowErrorsInChat = value,
                () => "Show errors in chat", () => "Show local chat notices as well as full SMAPI errors. Repeated chat notices are limited to once per action every 10 seconds.", fieldId: nameof(ModConfig.ShowErrorsInChat));

            void Percent(string id, string name, string help, Func<ModConfig, double> get, Action<ModConfig, double> set)
                => api.AddNumberOption(mod.ModManifest, () => (int)Math.Round(get(mod.Config) * 100),
                    value => set(mod.Config, Math.Clamp(value, 0, 100) / 100.0), () => name, () => help,
                    0, 100, 1, value => $"{value}%", id);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report("Register config menu", ex);
            ErrorHandler.Try("Remove incomplete config menu", () => api.Unregister(mod.ModManifest));
        }
    }
}
