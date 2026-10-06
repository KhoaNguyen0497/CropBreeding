# Crop Breeding

A gameplay-focused SMAPI mod for Stardew Valley 1.6, including Stardew Valley Expanded crops. Development version; no release yet.

## Breeding loop

Grow crops on ordinary tilled ground. An eligible harvest has a configurable chance to add one new level-1 trait or increase one existing trait by one level. Existing traits stay intact. Open the Breeding Machine UI and place **5 trait crops and 5 matching seeds** in its slots; it consumes the inputs and produces **one bred seed**. Each input comes from a single stack of at least five: the seeds must match each other, and the donor crops must match each other (traits, levels, Companion choice, quality and normal stacking rules). Exactly five of each are consumed.

Plain seeds copy the donor crop's traits. Seeds with **exactly one trait at level 1** merge that trait into the donor: an existing trait gains one level, or a different trait is added at level 1. Seeds with multiple traits or a trait above level 1 are rejected. A merge that would exceed the trait-count limit or level 5 is rejected without consuming the seeds or changing the waiting donor.

Examples: `X + Y` donor with `X` seeds gives `X2 + Y`; the same donor with `Z` seeds gives `X + Y + Z`. Each still costs ten seeds and one donor. Matching comes from the loaded `Data/Crops` seed ID and `HarvestItemId`, including Content Patcher/SVE changes; names are never used.

The machine unlocks at Farming level 5. Its provisional recipe uses 50 wood, 5 iron bars and 1 battery pack. It uses a static 16×32 incubator-inspired wooden machine sprite with a green sprout. Interact to open its two-slot menu (holding an inventory item still opens the UI without depositing or consuming it): put a trait crop on the left and matching seeds on the right, then select **Breed**. Collect the finished seed from the left slot. You can retrieve the donor before breeding. Unused seeds return to your inventory when closing (or drop beside you if full); the donor/completed output stays in the machine. Destroying the machine loses its contents. The menu uses the normal inventory and controller navigation. The Breeding Machine acts as an interactive crafting station: it accepts inputs only through its UI and has no Automate integration. Trait crops can still be used by Automate with other machines through their normal input rules.

Coffee beans already act as both produce and seeds, so mutated beans can be replanted directly. They do not need the breeding machine.

## Provisional traits and balance

| Trait | Effect |
|---|---|
| Researcher | Adds 5 percentage points of mutation chance per level: default total 10/15/20/25/30%. Adds 10% initial growth and regrowth time per level. Timing: (ordinary adjusted growth + Companion delay) × Researcher multiplier × Fast Growth multiplier, rounded up once. Uses the parent's inherited level. |
| Seed Saver | 10% chance per level (up to 50%) to return one matching seed per successful plant harvest. Copies the parent's original traits, levels and Companion choice, never the new harvest mutation. No High Yield multiplication or High Quality upgrade. Works on regrowing harvests and directly plantable coffee too. |
| Evergreen | Levels 1–4 are dormant and still occupy a trait slot. Level 5 allows planting and growth in all seasons, including winter, and prevents seasonal death on eligible tilled ground. Regrowing crops keep regrowing; single-harvest crops remain single-harvest. Normal watering and location restrictions still apply. |
| Fast Growth | Reduces initial growth and supported regrowth by 5% per level (25% at level 5), after ordinary bonuses and Companion delay. Round the final result up, minimum one day. Never creates regrowth for single-harvest crops. |
| High Yield | Adds 20% primary output per level (up to +100%). Guaranteed whole extra items plus one roll for the fractional remainder, calculated across the whole plant harvest. For 4 base items, level 1 gives 4 plus an 80% chance of a fifth; level 2 gives 5 plus a 60% chance of a sixth. Includes vanilla bonus produce; excludes Companion output and byproducts. Extras inherit the same mutation and sample original output quality/color; High Quality rolls independently per item. |
| Companion | 20% chance per level to produce one chosen companion crop per plant harvest. Adds half the companion's base growth time to initial growth and every regrowth cycle. |
| High Quality | 5% chance per level (25% at level 5) to upgrade each harvested primary item by one tier after normal quality calculation: normal → silver → gold → iridium. Iridium stays iridium. |

Premium and Hardy are ignored. Legacy Fast Regrowth metadata converts to Fast Growth, keeping the higher level if both were present. `GrowthReductionPerLevel` is the new shared config setting, default 0.05; obsolete `FastGrowthReduction` and `FastRegrowthReduction` keys are ignored. `ExtraYieldChance` remains proportional extra yield per level, default 0.20.

Every primary crop harvested from a trait plant inherits its traits and levels, including every regrowing harvest. These crops can be used as donors to copy the traits into more seeds. A new mutation changes harvested produce, not the standing plant. For example, Fast Growth level 5 changes 7 days to 6; 2 days still rounds up to 2.

Default mutation chance is 5%; default cap is 3 unique traits, each with levels 1–5. Successful mutations choose uniformly among eligible trait types: add a missing type if there is room, or upgrade an existing type below level 5. Upgrades remain possible when the trait-count cap is reached. One mutation roll applies to all primary produce from a harvest. High Quality uses separate rolls for each individual item, including the High Yield bonus, based on the plant's inherited level. A new High Quality mutation does not improve the same harvest; breed and replant it first. Quality rolls run after fertilizer/farming-level quality calculations, without changing those calculations. Mixed-quality outputs are separated into stacks. Hand-harvest extra items drop as ordinary harvest debris; Junimos receive them in the hut. The updated Auto Harvester uses this same vanilla Junimo harvest path: outputs go to its storage, with overflow dropped on the ground. Byproducts such as sunflower seeds and wheat hay do not receive quality upgrades. The new mutation is stored on produce; growth, regrowth, yield and quality effects require replanting. Lowering the cap never deletes existing traits; plain-seed copying preserves them, but merging is blocked while the donor exceeds the count cap. Existing saves with unlevelled traits read as level 1. Traits are stored in save-compatible `modData`; different trait sets or levels cannot stack, and vanilla quality/color distinctions still apply.

## Special cases

### Lookup Anything

Optional display integration adjusts trait seed previews, planted crop growth/regrowth summaries and next-harvest countdowns. Evergreen 5 displays all four seasons and avoids false out-of-season harvest warnings. Seed previews include Fast Growth, Companion and the current player's Agriculturist profession, without assuming fertilizer or paddy adjacency. Planted crops use their existing phase durations and countdowns. This does not change shared crop data or actual plants, and runs only during lookups. Harvest yield/quality probability fields remain Lookup Anything's base calculations, not breeding-trait forecasts. If Lookup Anything's internal API changes, the integration logs a warning and disables itself; gameplay does not depend on it.

- Excluded: Mixed Seeds, Mixed Flower Seeds, spring/summer/fall/winter forage seeds, vanilla Fiber Seeds, Qi Beans, tea saplings, trees and grass.
- Normal ground only, including greenhouse/Ginger Island tilled ground. Garden Pots and modded planters using pot soil are excluded; trait seeds are refused there so traits aren't silently discarded. A custom planter implementing actual terrain soil needs an explicit compatibility rule.
- SVE crops are discovered from their real loaded data. **Ancient Fiber is eligible**; only vanilla Fiber Seeds are excluded.
- Sunflower bonus seeds copy the original plant traits; only harvested flowers receive the new mutation. Wheat hay remains ordinary.
- Rice/taro retain normal paddy rules. Hand, scythe and Junimo harvesting use the shared vanilla `Crop.harvest` hooks. The updated `KhoaNguyen0497.AutoHarvester` calls that same method and needs no dedicated integration or dependency. Older versions that bypass vanilla harvesting are no longer supported.
- Giant crop formation follows vanilla rules, even when constituent plants have different traits. Breaking the giant crop gives ordinary vanilla output with no traits.
- Vanilla machine/crafting outputs have no traits. Input quality remains available to their ordinary rules. Other mods that create outputs by copying custom metadata may need separate compatibility patches.
- Old saves start with ordinary crops. Existing crops from mixed seeds cannot reliably be identified retrospectively once the game has resolved the seed to a crop; newly planted mixed seeds are explicitly marked ineligible.

## Companion selection and timing

Companion starts unassigned when it first mutates. It takes one trait slot whether assigned or not. An unassigned trait produces no companion and adds no delay.

Empty both machine slots and select **Mode: Set Companion**. Insert **one seed with Companion** on the left and **one eligible crop** on the right. Select Set, then collect the updated seed from the left. This consumes one crop and updates one seed, preserving all existing traits, levels and seed quality. Any Companion level or number of other seed traits is allowed here. Existing selection can be replaced; choosing the same crop is rejected. The crop's own traits/quality are ignored. The main crop itself is allowed as its own companion. Ineligible crops, forage/fiber/Qi outputs with no eligible mapping, processed items and seeds without Companion are rejected. Switch back to Breeding mode for ordinary 5-seed + 5-crop breeding. Both machine modes are operated manually.

In ordinary breeding, the donor's assigned companion wins. If the donor's Companion is unassigned or absent, use the seed's assigned companion instead. If both are unassigned, the result stays unassigned. Existing merge restrictions and count/level limits still apply. Different selections do not stack together.

The selected crop and level carry from seed to plant to every primary harvest (and original-trait sunflower bonus seeds). Each successful harvest rolls once using the plant's inherited Companion level: 20/40/60/80/100%. Success produces exactly **one normal-quality, trait-free companion crop**, irrespective of primary yield. High Yield and High Quality do not modify it. New Companion mutations take effect after breeding and replanting. No recursive companion production.

Both penalties use the companion's **initial base growth days** from loaded `Data/Crops`, including SVE, without fertilizer, profession or trait bonuses. They do not use the companion's regrowth interval. If multiple eligible seeds produce the same item, use the longest listed base growth time consistently. The lookup is cached and rebuilt on crop-data invalidation; there are no daily world scans.

- Initial growth: ceil((growth after ordinary bonuses + half companion base growth) × (1 − 0.05 × Fast Growth level)). Example: (5 + 7/2) × 0.75 = **7 days** at level 5.
- Regrowth: the same final multiplier applies after the existing harvest countdown and Companion delay. Example: (4 + 7/2) × 0.75 = **6 days** at level 5.
- Fast Growth reduces the Companion portion too. Single-harvest crops remain single-harvest.
- Recalculating growth first removes this mod's previous phase adjustments, preventing accumulated bonuses or delays.

Regrowing crop mutations are enabled by default (`EnableRegrowingCropMutations=true`). Every successful harvest can mutate its produce at the configured mutation chance; the original plant does not permanently acquire the new mutation. Existing config files explicitly set to `false` must be changed to `true` to enable this.

## Decisions still open

Researcher and Seed Saver settings: `ResearcherMutationBonus=0.05`, `ResearcherGrowthPenalty=0.10`, `SeedSaverChance=0.10`, all per level. Researcher does not bypass disabled regrowing mutations or trait/level caps. Seed Saver returns one seed at most per plant harvest, regardless of primary yield. Neither trait scans the world. Remove Trait mode can remove Researcher or any other selected trait from a seed.

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

## Removing traits

Controller navigation includes the mode button, both input/selection controls, action button, inventory and close button. Switching modes keeps focus on the mode button. B closes the UI and consumes that press to prevent opening the player inventory; cursor/right-slot items are returned normally, without triggering a machine action. Holding an axe or pickaxe bypasses station interaction so tool use can remove it through the usual tool path.

With both slots empty, cycle the mode button to **Remove Trait**. Insert one eligible seed with traits on the left. Select the right-hand trait button to cycle through its traits, then select **Remove**. Collect the seed from the left. Only the selected trait is removed; other levels, quality and metadata are retained. Removing Companion also clears its assigned crop. Removing the final trait returns a plain seed. No additional ingredient or fee is required. Crops are rejected unless the item itself is a supported plantable seed (e.g. coffee beans). The controller uses the same selectable buttons. Close/reopen preserves the staged seed; breaking the station destroys its contents as usual.
