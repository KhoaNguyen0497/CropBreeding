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
            api.AddSectionTitle(mod.ModManifest, () => "Breeding and mutations", () => "No restart needed. Mutation changes affect the next readiness roll; a ready plant keeps its saved result. Breeding uses current settings.");
            api.AddNumberOption(mod.ModManifest, () => mod.Config.MaximumTraits, value => mod.Config.MaximumTraits = Math.Clamp(value, 0, Core.TraitRules.Known.Length),
                () => "Maximum traits", () => "Unique traits per seed or crop. Zero prevents adding new traits. Lowering this does not remove existing traits.", 0, Core.TraitRules.Known.Length, 1, fieldId: nameof(ModConfig.MaximumTraits));
            Percent(nameof(ModConfig.MutationChance), "Mutation chance", "Rolled once when each harvest becomes ready, before Researcher. Already stored outcomes are unchanged by settings edits.", c => c.MutationChance, (c, v) => c.MutationChance = v);
            api.AddBoolOption(mod.ModManifest, () => mod.Config.EnableRegrowingCropMutations, value => mod.Config.EnableRegrowingCropMutations = value,
                () => "Mutations on regrowing crops", () => "Affects future readiness rolls, not outcomes already stored on ready plants.", fieldId: nameof(ModConfig.EnableRegrowingCropMutations));
            api.AddSectionTitle(mod.ModManifest, () => "Trait effects per level", () => "Harvest bonuses use current settings. Growth settings don't rewrite planted phases or a running regrowth countdown; see each option's timing.");
            Percent(nameof(ModConfig.GrowthReductionPerLevel), "Fast Growth reduction", "Applied after other modifiers. Initial growth changes on planting, Rooted restart, or a normal growth recalculation. Regrowth changes after the next harvest; the current countdown stays unchanged.", c => c.GrowthReductionPerLevel, (c, v) => c.GrowthReductionPerLevel = v);
            Percent(nameof(ModConfig.ExtraYieldPerLevel), "High Yield bonus", "Proportional extra produce, with fractional remainder chance. Total bonus capped at 100%. Uses the current value at the next harvest, including already-ready crops.", c => c.ExtraYieldPerLevel, (c, v) => c.ExtraYieldPerLevel = v);
            Percent(nameof(ModConfig.CompanionChance), "Companion chance", "Chance for one companion crop. Uses the current value at the next harvest, including already-ready crops.", c => c.CompanionChance, (c, v) => c.CompanionChance = v);
            Percent(nameof(ModConfig.QualityUpgradeChance), "High Quality chance", "Chance per item to upgrade one quality tier. Uses the current value at the next harvest, including already-ready crops.", c => c.QualityUpgradeChance, (c, v) => c.QualityUpgradeChance = v);
            Percent(nameof(ModConfig.SeedSaverChance), "Seed Saver chance", "Chance to return one seed with the parent's traits. Uses the current value at the next harvest, including already-ready crops.", c => c.SeedSaverChance, (c, v) => c.SeedSaverChance = v);
            Percent(nameof(ModConfig.RootedChance), "Rooted chance", "Chance to restart a single-harvest crop. Uses the current value at the next harvest, including already-ready crops.", c => c.RootedChance, (c, v) => c.RootedChance = v);
            Percent(nameof(ModConfig.ResearcherMutationBonus), "Researcher mutation bonus", "Percentage points added to the next readiness roll. A ready plant keeps its saved outcome.", c => c.ResearcherMutationBonus, (c, v) => c.ResearcherMutationBonus = v);
            Percent(nameof(ModConfig.ResearcherGrowthPenalty), "Researcher growth penalty", "Extra initial growth and regrowth time per level. Applies on planting, Rooted restart, or a normal growth recalculation; regrowth uses it after the next harvest. Existing countdowns stay unchanged.", c => c.ResearcherGrowthPenalty, (c, v) => c.ResearcherGrowthPenalty = v);
            api.AddSectionTitle(mod.ModManifest, () => "Error reporting");
            api.AddBoolOption(mod.ModManifest, () => mod.Config.ShowErrorsInChat, value => mod.Config.ShowErrorsInChat = value,
                () => "Show errors in chat", () => "Takes effect immediately. Local chat notices are limited to once per action every 10 seconds. SMAPI logs each new error in full and summarizes identical repeats.", fieldId: nameof(ModConfig.ShowErrorsInChat));

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
