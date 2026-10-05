# Crop Breeding

A gameplay-focused SMAPI mod for Stardew Valley 1.6, including Stardew Valley Expanded crops. Development version; no release yet.

## Breeding loop

Grow crops on ordinary tilled ground. An eligible harvest has a configurable chance to gain one new trait. Existing traits stay intact. Put **one trait crop into the Breeding Machine, then one matching seed**; it produces one seed with the donor crop's exact traits. Donor traits replace existing seed traits, never merge. Matching comes from the loaded `Data/Crops` seed ID and `HarvestItemId`, including Content Patcher/SVE changes; names are never used.

The machine unlocks at Farming level 5. Its provisional recipe uses 50 wood, 5 iron bars and 1 battery pack. It uses a static 16×32 incubator-inspired wooden machine sprite with a green sprout. Click/interact to retrieve a waiting donor or completed seed; destroy the machine to lose its contents. Automate can supply inputs and collect outputs.

Coffee beans already act as both produce and seeds, so mutated beans can be replanted directly. They do not need the breeding machine.

## Provisional traits and balance

| Trait | Effect |
|---|---|
| Fast Growth | Adds 10 percentage points to initial-growth speed reduction alongside fertilizer/professions. Does not shorten regrowth. |
| High Yield | 25% chance of one extra primary crop per harvest; extra item preserves that output's quality/color. |
| Premium | 20% higher direct crop/seed sale price. Processing uses the ordinary input price. |
| Hardy | 25% water-retention chance, combined with normal retention. |

Default mutation chance is 5%; default cap is 3 unique traits. One mutation roll applies to all primary produce from a harvest. The new mutation is stored on produce; growth, retention and yield effects require replanting. Premium affects the resulting item's sale price immediately. Lowering the cap never deletes existing traits. Traits are stored in save-compatible `modData`; different trait sets cannot stack, and vanilla quality/color distinctions still apply.

## Special cases

- Excluded: Mixed Seeds, Mixed Flower Seeds, spring/summer/fall/winter forage seeds, vanilla Fiber Seeds, Qi Beans, tea saplings, trees and grass.
- Normal ground only, including greenhouse/Ginger Island tilled ground. Garden Pots and modded planters using pot soil are excluded; trait seeds are refused there so traits aren't silently discarded. A custom planter implementing actual terrain soil needs an explicit compatibility rule.
- SVE crops are discovered from their real loaded data. **Ancient Fiber is eligible**; only vanilla Fiber Seeds are excluded.
- Sunflower bonus seeds copy the original plant traits; only harvested flowers receive the new mutation. Wheat hay remains ordinary.
- Rice/taro retain normal paddy rules. Hand, scythe, Junimo and the current `KhoaNguyen0497.AutoHarvester` harvest planner have hooks.
- Giant crop formation follows vanilla rules, even when constituent plants have different traits. Breaking the giant crop gives ordinary vanilla output with no traits.
- Vanilla machine/crafting outputs have no traits. Input quality remains available to their ordinary rules. Other mods that create outputs by copying custom metadata may need separate compatibility patches.
- Old saves start with ordinary crops. Existing crops from mixed seeds cannot reliably be identified retrospectively once the game has resolved the seed to a crop; newly planted mixed seeds are explicitly marked ineligible.

## Decisions still open

12. Regrowing crop mutation frequency: disabled by default (`EnableRegrowingCropMutations=false`). If enabled, each harvest can mutate its produce; the original plant does not permanently acquire the new mutation.

16. Foraged/shop/drop produce: no new mutations are granted to those sources. An otherwise matching item that already carries trait metadata can currently be used as a donor; no provenance restriction yet.

18. Seed Maker inheritance: disabled by default (`EnableSeedMakerInheritance=false`). If enabled, only a genuinely matching seed output copies traits; mixed seeds/Ancient Seeds bonus results do not get unrelated traits.

## Build and test

Install .NET SDK 8 or newer and SMAPI 4 in Stardew Valley 1.6. Build using your game directory:

```sh
dotnet build src/CropBreeding.csproj -c Release -p:GamePath="/path/to/Stardew Valley"
dotnet run --project tests/TraitRules.Tests.csproj
```

Copy `CropBreeding.dll`, `manifest.json` and the `assets` directory from the build output into `Mods/CropBreeding`. Install on all multiplayer clients. No game binaries are included in this repository.

Compile and pure trait-rule checks are automated locally; interactive game validation is still required. See [manual checks](docs/TESTING.md), particularly harvest integrations and controller interactions.

## Commands and uninstalling

- `cropbreeding_give`: give a machine for testing.
- `cropbreeding_catalog`: list the exact eligible seed → harvest mappings from your installed mods.
- `cropbreeding_cleanup`: as the host, remove traits, placed/inventory breeding machines, their contents and recipe unlocks. **Back up your save first, run this while the mod is installed, save, quit, then remove the mod.** Ordinary crops remain. This command intentionally destroys machine contents.
