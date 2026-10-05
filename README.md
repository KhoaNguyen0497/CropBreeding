# Crop Breeding

A gameplay-focused SMAPI mod for Stardew Valley 1.6, including Stardew Valley Expanded crops. Development version; no release yet.

## Breeding loop

Grow crops on ordinary tilled ground. An eligible harvest has a configurable chance to add one new level-1 trait or increase one existing trait by one level. Existing traits stay intact. Put **one trait crop into the Breeding Machine, then 10 matching seeds**; it consumes the inputs and produces **one bred seed**. The ten seeds must have identical traits and quality. Manual insertion requires a stack of at least ten; Automate can combine matching split stacks.

Plain seeds copy the donor crop's traits. Seeds with **exactly one trait at level 1** merge that trait into the donor: an existing trait gains one level, or a different trait is added at level 1. Seeds with multiple traits or a trait above level 1 are rejected. A merge that would exceed the trait-count limit or level 5 is rejected without consuming the seeds or changing the waiting donor.

Examples: `X + Y` donor with `X` seeds gives `X2 + Y`; the same donor with `Z` seeds gives `X + Y + Z`. Each still costs ten seeds and one donor. Matching comes from the loaded `Data/Crops` seed ID and `HarvestItemId`, including Content Patcher/SVE changes; names are never used.

The machine unlocks at Farming level 5. Its provisional recipe uses 50 wood, 5 iron bars and 1 battery pack. It uses a static 16×32 incubator-inspired wooden machine sprite with a green sprout. Interact with empty hands to open its two-slot menu: put a trait crop on the left and matching seeds on the right, then select **Breed**. Collect the finished seed from the left slot. You can retrieve the donor before breeding. Unused seeds return to your inventory when closing (or drop beside you if full); the donor/completed output stays in the machine. Destroying the machine loses its contents. The menu uses the normal inventory and controller navigation. Automate can supply inputs and collect outputs, and pauses for that machine while its menu is open.

Coffee beans already act as both produce and seeds, so mutated beans can be replanted directly. They do not need the breeding machine.

## Provisional traits and balance

| Trait | Effect |
|---|---|
| Fast Growth | Adds 10 percentage points per level to initial-growth speed reduction alongside fertilizer/professions. Total trait speed reduction is capped at 90%. Does not shorten regrowth. |
| High Yield | 20% chance per level of one extra primary crop per harvest (capped at 100%); extra item preserves that output's quality/color. |
| Companion | 20% chance per level to produce one chosen companion crop per plant harvest. Adds half the companion's base growth time to initial growth and every regrowth cycle. |
| High Quality | 5% chance per level (25% at level 5) to upgrade each harvested primary item by one tier after normal quality calculation: normal → silver → gold → iridium. Iridium stays iridium. |
| Fast Regrowth | Reduces the post-harvest regrowth countdown by 10% per level. Rounded up to whole days, minimum 1 day. Can only appear or upgrade through mutation on crops whose loaded crop data supports regrowth, including SVE crops. Has no effect on single-harvest crops. |

Premium and Hardy have been removed; old metadata for them is ignored. Existing config files can retain obsolete keys, but those keys have no effect. `ExtraYieldChance` now defaults to 0.20 and `FastRegrowthReduction` to 0.10 per level.

Every primary crop harvested from a trait plant inherits its traits and levels, including every regrowing harvest. These crops can be used as donors to copy the traits into more seeds. A new mutation changes harvested produce, not the standing plant. For example, Fast Regrowth level 5 changes 7 days to 4, or 2 days to 1.

Default mutation chance is 5%; default cap is 3 unique traits, each with levels 1–5. Successful mutations choose uniformly among eligible trait types: add a missing type if there is room, or upgrade an existing type below level 5. Upgrades remain possible when the trait-count cap is reached. One mutation roll applies to all primary produce from a harvest. High Quality uses separate rolls for each individual item, including the High Yield bonus, based on the plant's inherited level. A new High Quality mutation does not improve the same harvest; breed and replant it first. Quality rolls run after fertilizer/farming-level quality calculations, without changing those calculations. Mixed-quality outputs are separated into stacks. Hand-harvest extra items drop as ordinary harvest debris; Junimos receive them in the hut. Auto Harvester checks capacity against the full split output before harvesting. Byproducts such as sunflower seeds and wheat hay do not receive quality upgrades. The new mutation is stored on produce; growth, regrowth, yield and quality effects require replanting. Lowering the cap never deletes existing traits; plain-seed copying preserves them, but merging is blocked while the donor exceeds the count cap. Existing saves with unlevelled traits read as level 1. Traits are stored in save-compatible `modData`; different trait sets or levels cannot stack, and vanilla quality/color distinctions still apply.

## Special cases

- Excluded: Mixed Seeds, Mixed Flower Seeds, spring/summer/fall/winter forage seeds, vanilla Fiber Seeds, Qi Beans, tea saplings, trees and grass.
- Normal ground only, including greenhouse/Ginger Island tilled ground. Garden Pots and modded planters using pot soil are excluded; trait seeds are refused there so traits aren't silently discarded. A custom planter implementing actual terrain soil needs an explicit compatibility rule.
- SVE crops are discovered from their real loaded data. **Ancient Fiber is eligible**; only vanilla Fiber Seeds are excluded.
- Sunflower bonus seeds copy the original plant traits; only harvested flowers receive the new mutation. Wheat hay remains ordinary.
- Rice/taro retain normal paddy rules. Hand, scythe, Junimo and the current `KhoaNguyen0497.AutoHarvester` harvest planner have hooks.
- Giant crop formation follows vanilla rules, even when constituent plants have different traits. Breaking the giant crop gives ordinary vanilla output with no traits.
- Vanilla machine/crafting outputs have no traits. Input quality remains available to their ordinary rules. Other mods that create outputs by copying custom metadata may need separate compatibility patches.
- Old saves start with ordinary crops. Existing crops from mixed seeds cannot reliably be identified retrospectively once the game has resolved the seed to a crop; newly planted mixed seeds are explicitly marked ineligible.

## Companion selection and timing

Companion starts unassigned when it first mutates. It takes one trait slot whether assigned or not. An unassigned trait produces no companion and adds no delay.

Empty both machine slots and select **Mode: Set Companion**. Insert **one seed with Companion** on the left and **one eligible crop** on the right. Select Set, then collect the updated seed from the left. This consumes one crop and updates one seed, preserving all existing traits, levels and seed quality. Any Companion level or number of other seed traits is allowed here. Existing selection can be replaced; choosing the same crop is rejected. The crop's own traits/quality are ignored. The main crop itself is allowed as its own companion. Ineligible crops, forage/fiber/Qi outputs with no eligible mapping, processed items and seeds without Companion are rejected. Switch back to Breeding mode for ordinary 10-seed breeding. Automate processes Breeding mode only; companion selection is manual to avoid choosing arbitrary crops from a chest.

In ordinary breeding, the donor's assigned companion wins. If the donor's Companion is unassigned or absent, use the seed's assigned companion instead. If both are unassigned, the result stays unassigned. Existing merge restrictions and count/level limits still apply. Different selections do not stack together.

The selected crop and level carry from seed to plant to every primary harvest (and original-trait sunflower bonus seeds). Each successful harvest rolls once using the plant's inherited Companion level: 20/40/60/80/100%. Success produces exactly **one normal-quality, trait-free companion crop**, irrespective of primary yield. High Yield and High Quality do not modify it. New Companion mutations take effect after breeding and replanting. No recursive companion production.

Both penalties use the companion's **initial base growth days** from loaded `Data/Crops`, including SVE, without fertilizer, profession or trait bonuses. They do not use the companion's regrowth interval. If multiple eligible seeds produce the same item, use the longest listed base growth time consistently. The lookup is cached and rebuilt on crop-data invalidation; there are no daily world scans.

- Initial growth: normal speed-adjusted main growth + half companion base growth, rounded up. Example: 5 + 7/2 = **9 days**.
- Regrowth: main regrowth after Fast Regrowth + half companion base growth, then round up once. Example: 4 + 7/2 = **8 days**. With Fast Regrowth 5: 4 × 0.5 + 7/2 = **6 days**.
- The companion portion is never reduced by speed bonuses. A single-harvest plant remains single-harvest.
- Reapplying fertilizer/speed calculations replaces the initial delay rather than accumulating it. Cleanup removes the recorded delay.

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
