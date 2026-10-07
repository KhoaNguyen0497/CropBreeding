# Crop Breeding

A gameplay-focused SMAPI mod for Stardew Valley 1.6, including Stardew Valley Expanded crops. Development version; no release yet. This mod has never been installed: development targets fresh installs, with no migrations or support for earlier development save/config formats.

## Breeding loop

Grow crops on ordinary tilled ground. When an eligible crop becomes harvest-ready, it rolls a configurable chance to add one new level-1 trait or increase one existing trait by one level on its produce. The saved outcome stays fixed until harvested. Existing traits stay intact. Open the Breeding Machine UI and place **5 trait crops and 5 matching seeds** in its slots; it consumes the inputs and produces **one bred seed**. Each input comes from a single stack of at least five: the seeds must match each other, and the donor crops must match each other (traits, levels, Companion choice, quality and normal stacking rules). Exactly five of each are consumed.

Plain seeds copy the donor crop's traits. Seeds with **exactly one trait at level 1** merge that trait into the donor: an existing trait gains one level, or a different trait is added at level 1. Seeds with multiple traits or a trait above level 1 are rejected. A merge that would exceed the trait-count limit or level 5 is rejected without consuming the seeds or changing the waiting donor.

Examples: `X + Y` donor with `X` seeds gives `X2 + Y`; the same donor with `Z` seeds gives `X + Y + Z`. Each still costs five seeds and five donor crops. Matching comes from the loaded `Data/Crops` seed ID and `HarvestItemId`, including Content Patcher/SVE changes; names are never used.

The machine unlocks at Farming level 5. Its provisional recipe uses 50 wood, 5 iron bars and 1 battery pack. It uses a static 16×32 incubator-inspired wooden machine sprite with a green sprout. Interact to open its two-slot menu (holding an inventory item still opens the UI without depositing or consuming it): put a trait crop on the left and matching seeds on the right, then select **Breed**. Collect the finished seed from the left slot. You can retrieve the donor before breeding. Unused seeds return to your inventory when closing (or drop beside you if full); the donor/completed output stays in the machine. Destroying the machine loses its contents. The menu uses normal inventory input handling and controller navigation. Its controls, item icons, and hitboxes scale together to fit the UI viewport; status text wraps, and controller focus survives resizing. Breeding eligibility and removal labels refresh when inputs/settings change instead of being reparsed during drawing. Station locks are removed on a single deferred update after closing, rather than accumulating at old tile positions. The Breeding Machine acts as an interactive crafting station: it accepts inputs only through its UI and has no Automate integration. Trait crops can still be used by Automate with other machines through their normal input rules.

Coffee beans already act as both produce and seeds, so mutated beans can be replanted directly. They do not need the breeding machine.

## Provisional traits and balance

| Trait | Effect |
|---|---|
| Nurse Crop | On a successful single-harvest crop harvest, advances non-fruit trees in the eight surrounding tiles. 30% chance per level for a stage, overflowing into additional stages: levels 1–3 give 30/60/90% for one; level 4 gives one plus 20% for a second; level 5 gives one plus 50% for a second. Never advances a tree beyond stage 4 (one step short of maturity). |
| Rooted | 10% chance per level (50% at level 5) for a successfully harvested single-harvest crop to restart from the seed stage, without consuming a seed. Rolls once per plant harvest and can repeat across cycles. Only mutates on non-regrowing crops. |
| Copper Bearing | Produces Copper Bars using the material-drop rule below. |
| Iron Bearing | Produces Iron Bars using the material-drop rule below. |
| Gold Bearing | Produces Gold Bars using the material-drop rule below. |
| Maple Bearing | Produces Maple Syrup using the material-drop rule. |
| Resin Bearing | Produces Oak Resin using the material-drop rule. |
| Tar Bearing | Produces Pine Tar using the material-drop rule. |
| Researcher | Adds 5 percentage points of mutation chance per level: default total 10/15/20/25/30%. Adds 10% initial growth and regrowth time per level. Timing: (ordinary adjusted growth + Companion delay) × Researcher multiplier × Fast Growth multiplier, rounded up once. Uses the parent's inherited level. |
| Seed Saver | 10% chance per level (up to 50%) to return one matching seed per successful plant harvest. Copies the parent's original traits, levels and Companion choice, never the new harvest mutation. No High Yield multiplication or High Quality upgrade. Works on regrowing harvests and directly plantable coffee too. |
| Evergreen | Levels 1–4 are dormant and still occupy a trait slot. Level 5 allows planting and growth in all seasons, including winter, and prevents seasonal death on eligible tilled ground. Regrowing crops keep regrowing; single-harvest crops remain single-harvest. Normal watering and location restrictions still apply. |
| Fast Growth | Reduces initial growth and supported regrowth by 5% per level (25% at level 5), after ordinary bonuses and Companion delay. Round the final result up, minimum one day. Never creates regrowth for single-harvest crops. |
| High Yield | Adds 20% primary output per level (up to +100%). Guaranteed whole extra items plus one roll for the fractional remainder, calculated across the whole plant harvest. For 4 base items, level 1 gives 4 plus an 80% chance of a fifth; level 2 gives 5 plus a 60% chance of a sixth. Includes vanilla bonus produce; excludes Companion output and byproducts. Extras inherit the same mutation and sample original output quality/color; High Quality rolls independently per item. |
| Companion | 20% chance per level to produce one chosen companion crop per plant harvest. Adds half the companion's base growth time to initial growth and every regrowth cycle. |
| High Quality | 5% chance per level (25% at level 5) to upgrade each harvested primary item by one tier after normal quality calculation: normal → silver → gold → iridium. Iridium stays iridium. |

`GrowthReductionPerLevel` controls initial growth and regrowth reduction, default 0.05. `ExtraYieldPerLevel` controls proportional extra yield per level, default 0.20.

Every primary crop harvested from a trait plant inherits its traits and levels, including every regrowing harvest. These crops can be used as donors to copy the traits into more seeds. A new mutation changes harvested produce, not the standing plant. For example, Fast Growth level 5 changes 7 days to 6; 2 days still rounds up to 2.

Default mutation chance is 5%; default cap is 3 unique traits, each with levels 1–5. A successful chance roll chooses uniformly from ALL crop-eligible trait types, before checking owned traits, levels or free slots. Only intrinsic eligibility filters the pool (for example, natural regrowers cannot roll Rooted or Nurse Crop, and unavailable material outputs are excluded). The selected trait upgrades if below level 5, or is added at level 1 if a slot is free. Selecting a maxed trait or a missing trait with no free slot does nothing: no reroll. Thus X5/Y5/Z4 only upgrades when Z itself is selected; with N eligible traits its base per-harvest upgrade chance is 5% / N. Researcher increases the initial chance, not the odds of selecting any particular trait. One mutation roll applies to all primary produce from a harvest. High Quality uses separate rolls for each individual item, including the High Yield bonus, based on the plant's inherited level. A new High Quality mutation does not improve the same harvest; breed and replant it first. Quality rolls run after fertilizer/farming-level quality calculations, without changing those calculations. Mixed-quality outputs are separated into stacks. Hand-harvest extra items drop as ordinary harvest debris; Junimos receive them in the hut. The updated Auto Harvester uses this same vanilla Junimo harvest path: outputs go to its storage, with overflow dropped on the ground. Byproducts such as sunflower seeds and wheat hay do not receive quality upgrades. The new mutation is stored on produce; growth, regrowth, yield and quality effects require replanting. Lowering the cap never deletes existing traits; plain-seed copying preserves them, but merging is blocked while the donor exceeds the count cap. Traits use one explicit `trait:level` format in `modData`, including level 1; different trait sets or levels cannot stack, and vanilla quality/color distinctions still apply.

### Mutation readiness and icon

Mutation is decided once when an eligible plant becomes harvest-ready, not rerolled when picked. A separate `PendingMutation` crop `modData` entry stores either the complete resulting harvest traits or `-` for no change (including failed chance rolls and blocked picks). The parent's inherited traits and bonuses remain unchanged. Waiting extra days, reloading a saved outcome, or changing config does not reroll it. The deterministic readiness roll also reproduces the result if the same day/state is replayed before it was saved.

A small static **vanilla purple star** appears above a ready plant with an actual new/upgraded trait. This reuses the iridium-quality star in `Game1.mouseCursors`, but indicates mutation, **not harvest quality**. Drawing only reads the outcome/readiness and draws a visible icon; it does not roll, parse traits, spawn particles or scan the farm. No new texture or animation is needed.

The host checks readiness after vanilla's existing `Crop.newDay` and `Crop.growCompletely` calls (including the crop fairy). Normal overnight preparation precedes our Auto Harvester's `DayStarted` harvest and daytime Junimos. Already stored results are skipped. Newly planted instant-ready crops are handled after seed traits are assigned. The vanilla harvest hook also prepares a missing result, including locally performed farmhand harvests, so mods that directly advance phases without either growth method still receive mutation results; their icon may not appear before harvest. Future in-mod crop-growth effects should call `MutationState.EnsurePrepared` after changing growth. Harvesters bypassing vanilla `Crop.harvest` are not covered.

Failed/full-inventory harvests retain the outcome. Successful harvests clear it; regrowers and Rooted plants roll once again when their next harvest becomes ready. Rooted still resets the existing crop to seed stage rather than constructing a replacement. Save/network crop metadata carries the outcome; items receive only the ordinary harvested traits, not pending state. High Quality, High Yield's fractional bonus, Companion, material drops, Seed Saver, Nurse Crop and Rooted rolls remain at harvest time using inherited levels. Mutation-related config changes apply to future readiness rolls, including re-enabling mutations after a disabled cycle was already recorded.

## Special cases

Nurse Crop rolls once per plant harvest using the inherited level and applies the same stage bonus to each adjacent living non-fruit tree, including diagonals. Each tree is capped independently at stage 4; trees already at stage 4 or mature are unchanged. Fruit trees, stumps and destroyed trees are ignored. It only mutates/upgrades on single-harvest crops and has no effect on natural regrowers, even if their metadata already contains it. Newly mutated produce needs breeding/replanting before the effect works. High Yield, bonus produce and Companion do not add rolls. Rooted crops can trigger it on each successful harvest before restarting. Failed harvests, tool destruction and giant crop chopping do not trigger it. It directly advances the tree's stage (even in winter) without calling tree day-update logic; the final maturity step still follows ordinary seasonal/spacing/growth rules. The effect uses at most eight terrain lookups per successful roll and adds no continuous or daily scans.

Rooted uses the parent's inherited level (`RootedChance=0.10` per level), retains its traits/Companion/color, and resets initial growth using current fertilizer, the harvesting player's Agriculturist profession (local game player for automated harvests), paddy adjacency, Companion, Researcher and Fast Growth. It keeps the soil's current water and fertilizer state. Harvest mutations affect produce only; failed harvests, natural regrowers, dead plants, tool destruction, giant crops and excluded crops never restart. Seasonal restrictions still apply unless Evergreen 5 protects the plant. Sunflower harvest IDs are restored before restarting. All harvest extras are committed first; then the vanilla harvest removal result is changed so hand/scythe/Junimo and the vanilla-based Auto Harvester retain the reset plant. No separate Auto Harvester integration or continuous scan is required.

Material traits each roll independently once per successful plant harvest, using the inherited level. Chance is `floor(base initial growth days / 5) × 5% × level`. Every complete 100% guarantees one bar; the remaining chance can add one more. A 28-day crop gets 25% at level 1, 100% at level 4, and 125% at level 5 (one guaranteed plus 25% for a second). Under five base days gives zero. Every regrowing harvest uses the same initial base growth, never the regrowth interval. Base growth comes from loaded crop data (including SVE); fertilizer, professions, Fast Growth, Researcher and Companion do not change these odds. Bars are plain items, unaffected by primary crop quantity, High Yield, High Quality or Companion. Failed harvests deliver no bars. These are ordinary inheritable/mutable/removable traits and each occupies one trait slot. No continuous scans are added.

Tapper-product traits cover only Maple Syrup, Oak Resin and Pine Tar. They use exactly the same material-drop formula and harvest path as Copper/Iron/Gold Bearing, including regrowing crops. Each successful drop adds one item; chances above 100% guarantee whole items plus a fractional chance of one more. Each product has a separate trait/roll, takes one normal trait slot, and produces plain normal-quality items. Material checks iterate only the parent's actual traits. No extra world scans are added.

### Lookup Anything

Lookup pages for trait seeds, harvested produce and planted crops include a separate row for each inherited trait, labelled with its level. Descriptions cover all 15 traits and use current configured percentages, including Companion selection/unassigned state, Evergreen's level-5 activation, Researcher's penalty, and material/Nurse Crop overflow rules. Planted crop descriptions use the actual plant's inherited traits, not its pending harvest mutation or a generic sample item. These are effect explanations, not guaranteed yield/quality forecasts. Normal inventory tooltips remain compact. Trait field integration is optional, validated separately from timing integration, and only runs when lookup fields are requested; it does not roll mutations or scan crops. If the field contract changes or description generation fails, normal Lookup Anything fields remain available.

Optional display integration adjusts trait seed previews, planted crop growth/regrowth summaries and next-harvest countdowns. Evergreen 5 displays all four seasons and avoids false out-of-season harvest warnings. Seed previews include Fast Growth, Companion and the current player's Agriculturist profession, without assuming fertilizer or paddy adjacency. Planted crops use their existing phase durations and countdowns. This does not change shared crop data or actual plants, and runs only during lookups. Harvest yield/quality probability fields remain Lookup Anything's base calculations, not breeding-trait forecasts. If Lookup Anything's internal API changes, the integration logs a warning and disables itself; gameplay does not depend on it.

- Excluded: Mixed Seeds, Mixed Flower Seeds, spring/summer/fall/winter forage seeds, vanilla Fiber Seeds, Qi Beans, tea saplings, trees and grass.
- Normal ground only, including greenhouse/Ginger Island tilled ground. Garden Pots and modded planters using pot soil are excluded; trait seeds are refused there so traits aren't silently discarded. A custom planter implementing actual terrain soil needs an explicit compatibility rule.
- SVE crops are discovered from their real loaded data. **Ancient Fiber is eligible**; only vanilla Fiber Seeds are excluded.
- Sunflower bonus seeds copy the original plant traits; only harvested flowers receive the new mutation. Wheat hay remains ordinary.
- Rice/taro retain normal paddy rules. Hand, scythe and Junimo harvesting use the shared vanilla `Crop.harvest` hooks. The updated `KhoaNguyen0497.AutoHarvester` calls that same method and needs no dedicated integration or dependency. Harvesters must call vanilla `Crop.harvest` for these hooks to run.
- Junimo raisins keep vanilla's 20% chance of **one extra main-crop item**, with that item's traits, Companion selection, quality and color. The last primary output is retained across delivery of material/Companion/Seed Saver/High Yield bonuses and vanilla seed/hay byproducts. The copy does not rerun mutation, Rooted, Nurse Crop or other harvest effects. No Better Junimos-specific harvesting patch or dependency is needed.
- Better Junimos harvesting uses this vanilla path. An optional planting integration transfers traits and Companion choice from the exact seed stack it selects, then reapplies initial trait growth timing; Better Junimos still owns seed selection/consumption. An optional fertilizer integration limits Better Junimos to empty soil or phase-0 crops and uses normal growth recalculation, preserving trait timing. Its out-of-season filter still does not recognize Evergreen; that limitation is accepted for now. See [the compatibility notes](docs/BETTER-JUNIMOS.md).
- Giant crop formation follows vanilla rules, even when constituent plants have different traits. Breaking the giant crop gives ordinary vanilla output with no traits.
- Vanilla machine/crafting outputs have no traits. Input quality remains available to their ordinary rules. Other mods that create outputs by copying custom metadata may need separate compatibility patches.
- Crops planted before first installing the mod have no traits. Their mixed-seed origin cannot reliably be identified once the game has resolved the seed to a crop; mixed seeds planted with the mod installed are explicitly marked ineligible.

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

Regrowing crop mutations are enabled by default (`EnableRegrowingCropMutations=true`). Every harvest cycle gets one readiness roll at the configured mutation chance; the original plant does not permanently acquire the new mutation. Set it to `false` to disable future regrowing readiness rolls. Already stored outcomes remain unchanged.

## Decisions still open

Researcher and Seed Saver settings: `ResearcherMutationBonus=0.05`, `ResearcherGrowthPenalty=0.10`, `SeedSaverChance=0.10`, all per level. Researcher does not bypass disabled regrowing mutations or trait/level caps. Seed Saver returns one seed at most per plant harvest, regardless of primary yield. Neither trait scans the world. Remove Trait mode can remove Researcher or any other selected trait from a seed.

16. Foraged/shop/drop produce: no new mutations are granted to those sources. An otherwise matching item that already carries trait metadata can currently be used as a donor; no provenance restriction yet.

Seed Maker outputs always have no breeding traits or Companion assignment. There is no inheritance setting.

## Error recovery and settings

Each gameplay patch catches its own breeding errors. New errors are reported in full to SMAPI; identical repeats for an action are summarized at most once every ten seconds while the error continues. A failed prefix lets the original action run; a failed result/tooltip patch preserves the original result. Crop growth/planting/Rooted and lookup changes restore captured state where possible. Harvest output decoration restores the original clone if it fails. Material item creation, Seed Saver, Companion output, and quality/yield preparation are isolated so a failed bonus does not discard the harvest context or its other prepared effects. Shared core-state failures still fall back to vanilla. An error does not permanently disable later actions or the whole mod. A harvest is never replayed, and an already-delivered item is never deliberately awarded again. Exceptions raised by vanilla or another mod's original code are not globally suppressed.

Breeding-menu slot/button transactions restore their inputs on failure, then close safely and release the multiplayer lock. Items return through normal inventory/overflow handling; if that return fails, an undelivered remainder is sent to the Lost and Found when possible. A failed patch installation removes only that attempt's additions, preserving earlier successful patches on the same target, and continues installing unrelated patches. This is best-effort recovery from mod errors, not a guarantee against process-level failures, broken game state or another mod failing after irreversible side effects.

Optional **Generic Mod Config Menu** support exposes all 12 settings with save/reset controls and percentage labels. `ShowErrorsInChat` defaults to `false`; enabling it adds local chat notices as well as full SMAPI errors. It does not send network chat or execute chat commands. Identical-action chat notices are limited to one per ten seconds. SMAPI logs new errors in full and summarizes identical repeats at most once per ten seconds while failures continue; it does not format/log every repeated exception or run a reporting timer. Malformed config loading falls back to defaults; numeric settings are bounded and non-finite rates are replaced with defaults.

No restart is needed. Saving settings does not scan/rewrite existing crops. GMCM help and planted-crop Lookup Anything pages explain the timing:

| Setting | When a changed value takes effect |
| --- | --- |
| Mutation chance, Researcher mutation bonus, regrowing mutation toggle | Next readiness roll. Success and failure outcomes already stored on ready crops stay fixed. |
| Maximum traits | Next breeding operation or new mutation roll. Existing traits are not removed. |
| Fast Growth / Researcher growth penalty | Initial growth on planting, Rooted restart, or a normal growth recalculation. Regrowth after the next successful harvest; an already-running countdown is unchanged. |
| High Yield, High Quality, Companion chance, Seed Saver, Rooted chance | Next harvest, including a crop that is already ready. Effects still use the plant's inherited trait levels. |
| Show errors in chat | Immediately. |

Lookup trait percentages describe current settings; a planted crop's stored phase durations/countdown can still reflect older growth settings. Material drop odds, Nurse Crop odds and the level cap remain fixed gameplay rules, not config settings.

## Build and test

Install .NET SDK 8 or newer and SMAPI 4 in Stardew Valley 1.6. Build using your game directory:

```sh
dotnet build src/CropBreeding.csproj -c Release -p:GamePath="/path/to/Stardew Valley"
dotnet run --project tests/TraitRules.Tests.csproj
dotnet run --project safety-tests/Safety.Tests.csproj
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
