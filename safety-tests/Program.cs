using CropBreeding;
using CropBreeding.Integrations;
using StardewValley;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
int calls = 0;
Check(!ErrorHandler.Try("Injected failure", () => { calls++; throw new InvalidOperationException("test exception"); }), "failure caught");
Check(calls == 1 && ModEntry.Instance.Monitor.Messages.Count == 1, "action executed once and full error logged");
Check(ErrorHandler.Try("Next independent action", () => calls++), "next action still runs");
Check(calls == 2 && Game1.chatBox!.Messages.Count == 0, "chat disabled by default");
ModEntry.Instance.Config.ShowErrorsInChat = true;
ErrorHandler.Report("Repeat", new Exception("first"));
ErrorHandler.Report("Repeat", new Exception("first"));
Check(Game1.chatBox!.Messages.Count == 1 && ModEntry.Instance.Monitor.Messages.Count == 2, "identical errors and chat both throttled");
ModEntry.Instance.Monitor.Throw = true;
Game1.chatBox.Throw = true;
ErrorHandler.Report("Broken reporters", new Exception("must not escape"));
Check(ErrorHandler.Try("Still running", () => calls++), "reporter failure does not disable actions");
ModEntry.Instance.Monitor.Throw = false;
Game1.chatBox.Throw = false;

var crop = new Crop { Dirt = new HoeDirt() };
crop.modData["test"] = "original";
var snapshot = new CropSnapshot(crop);
crop.phaseDays.Clear(); crop.phaseDays.Add(42);
crop.modData.Clear(); crop.modData["partial"] = "mutation";
crop.currentPhase.Value = 0; crop.dayOfCurrentPhase.Value = 8;
crop.phaseToShow.Value = 7; crop.fullyGrown.Value = true;
crop.raisedSeeds.Value = false; crop.indexOfHarvest.Value = "431";
crop.Dirt.state.Value = 0; crop.Dirt.nearWaterForPaddy.Value = -1;
snapshot.Restore();
Check(crop.phaseDays.SequenceEqual(new[] { 1, 2, 3, 99999 }), "growth phases restored");
Check(crop.modData.Count == 1 && crop.modData["test"] == "original", "metadata restored");
Check(crop.currentPhase.Value == 2 && crop.dayOfCurrentPhase.Value == 1 && crop.phaseToShow.Value == -1, "phase state restored");
Check(!crop.fullyGrown.Value && crop.raisedSeeds.Value && crop.indexOfHarvest.Value == "24", "crop flags and output restored");
Check(crop.Dirt.state.Value == 1 && crop.Dirt.nearWaterForPaddy.Value == 0 && crop.DrawUpdates == 1, "soil and drawing refreshed");

var config = new ModConfig { MutationChance = double.NaN, GrowthReductionPerLevel = double.PositiveInfinity,
    ExtraYieldPerLevel = -100, RootedChance = 50, MaximumTraits = int.MaxValue };
config.Normalize();
Check(config.MutationChance == .05 && config.GrowthReductionPerLevel == .05, "nonfinite settings replaced");
Check(config.ExtraYieldPerLevel == 0 && config.RootedChance == 1 && config.MaximumTraits == CropBreeding.Core.TraitRules.Known.Length, "settings bounded");

var api = new ConfigApi();
ModEntry.Instance.Helper.ModRegistry.Api = api;
GenericModConfigMenuIntegration.Register();
var fields = api.Numbers.Keys.Concat(api.Booleans.Keys).ToHashSet();
Check(typeof(ModConfig).GetProperties().All(p => fields.Contains(p.Name)) && fields.Count == 12, "every config setting appears in GMCM");
api.Numbers[nameof(ModConfig.MutationChance)].Set(25);
Check(ModEntry.Instance.Config.MutationChance == .25, "GMCM percentages convert correctly");
api.Booleans[nameof(ModConfig.ShowErrorsInChat)].Set(true);
api.Save();
Check(ModEntry.Instance.Helper.Saves == 1 && ModEntry.Instance.Config.ShowErrorsInChat, "GMCM save persists settings");
api.Reset();
Check(api.Numbers[nameof(ModConfig.MutationChance)].Get() == 5 && !api.Booleans[nameof(ModConfig.ShowErrorsInChat)].Get(), "callbacks use reset config");
ModEntry.Instance.Helper.Throw = true;
api.Save(); // file-write error must not escape into GMCM.
ModEntry.Instance.Helper.Throw = false;
api.Save();
Check(ModEntry.Instance.Helper.Saves == 2, "save can succeed after a failed save");
var brokenApi = new ConfigApi { FailOptions = true };
ModEntry.Instance.Helper.ModRegistry.Api = brokenApi;
GenericModConfigMenuIntegration.Register();
Check(brokenApi.Unregistered, "partial GMCM registration removed");
Console.WriteLine("Passed injected failures, reporter isolation, crop rollback, config validation and all GMCM fields/save/reset checks. Uses test doubles, not an in-game test.");

MutationLifecycleTests.Run();
LookupDescriptionTests.Run();

ReviewFixTests.Run();
