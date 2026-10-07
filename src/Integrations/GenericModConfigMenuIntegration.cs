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
            api.AddNumberOption(mod.ModManifest, () => mod.Config.BreedingCost, value => mod.Config.BreedingCost = Math.Clamp(value, 1, 999),
                () => "Breeding cost", () => "Each breeding or merge consumes this many matching seeds AND donor crops and produces one seed. Default: 1 each. Each input must be one matching stack. If changed with crops stored in a station, retrieve and reinsert them. Companion assignment and trait removal are unchanged.",
                1, 999, 1, fieldId: nameof(ModConfig.BreedingCost));
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
