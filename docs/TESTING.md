# Validation status and in-game checklist

Updated 2026-10-07. Gameplay rules are described in [README](../README.md); final review choices are in [REVIEW-DECISIONS](REVIEW-DECISIONS.md). [BETTER-JUNIMOS](BETTER-JUNIMOS.md) records the inspected upstream source and integration details.

## What has been checked

The implementation compiled against Stardew Valley/SMAPI reference assemblies. The pure trait-rule suite and linked-code safety suites passed during development. Version 1.0.1 also checks fixed trait rates, five GMCM settings and configurable breeding batches.

| Check | Coverage | Limit |
| --- | --- | --- |
| Game-reference compilation | Production types, method calls and signatures compile. | Does not execute the game or install live Harmony patches. |
| `tests/TraitRules.Tests.csproj` | Trait parsing/levels, merges and rejections, full-pool mutation selection, caps, growth/quality/yield formulas, Companion timing, material availability/overflow and Nurse Crop limits. | Pure rules; formula tests can intentionally pass non-default arguments. |
| `safety-tests/Safety.Tests.csproj` — general | Error reporting/throttling, crop rollback, GMCM fields/save/reset, patch rollback isolation, menu geometry and deferred station-lock cleanup. | Uses test doubles; no controller input, rendering or actual game locks. |
| Breeding costs | Linked station logic at cost 1/5/999, insufficient inputs, probes, stored-cost changes, merging and secondary modes. | Uses test doubles; inventory/controller UI needs live validation. |
| Mutation lifecycle | Stored success/failure, waiting/reload state, blocked picks, readiness/fallback, regrowth/Rooted cycle clearing and preparation failures. | Simulates state; no real save serialization, overnight event order or game RNG execution. |
| Harvest preparation | Independent bonus failures preserve inherited/mutated output and unrelated effects. | No end-to-end vanilla harvest or real inventory/chest behavior. |
| Lookup descriptions | All 15 trait descriptions, levels/settings, field construction, source precedence and failure fallback. | No live Lookup Anything UI or Harmony detours. |
| Junimo raisins | Primary-crop selection, copied traits/Companion/quality, seed/hay/same-ID Companion exclusion, delivery failures and separation from annual effects. | No live Junimo AI, color rendering or chest overflow simulation. |
| Better Junimos planting | Exact selected seed stack, inherited metadata, growth-call routing, nested/error cleanup, boundaries, instant-ready ordering and registration rollback. | Uses a mock ability contract, not Better Junimos itself. |
| Better Junimos fertilizing | Empty/phase-0 eligibility, later-phase rejection before execution, phase-change recheck, normal growth-call routing and snapshot recovery. | Does not run the game's fertilizer calculation. |
| Rooted walnuts | Scoped single roll, strict 5% boundary, `IslandFarming` request key/coordinates/limit, nested calls and error recovery. | Does not execute the game's shared drop counter, networking or item spawning. |

The shared RNG now includes save ID, location, tile, effect salt and `DaysPlayed * 10000d + Game1.timeOfDay`. Compilation and existing lifecycle checks cover the change's integration and stored-outcome invariants; there is no claim of a live statistical RNG test.

**No live Stardew Valley, Steam Deck/controller, rendered UI or end-to-end installed-mod test has been completed.** The checklists below are pending work, not passed results. Multiplayer and migration from older development saves/configs are outside scope.

## Running the automated checks

Use .NET SDK 8 or newer. Supply a Stardew Valley 1.6/SMAPI 4 game directory or compatible reference assemblies for the production build:

```sh
dotnet build src/CropBreeding.csproj -c Release -p:GamePath="/path/to/Stardew Valley"
dotnet run --project tests/TraitRules.Tests.csproj
dotnet run --project safety-tests/Safety.Tests.csproj
```

No game binaries are included. A passed safety suite must not be reported as an in-game compatibility test.

## Pending single-player checks

Use a copied save with the actual installed SVE, Better Junimos, updated Auto Harvester, Automate, Lookup Anything and Generic Mod Config Menu versions. Test optional mods absent as well as present where relevant. Record versions, config, input items, expected result and observed result when completing a check.

### 1. Station modes and controller

- [ ] Reach every inventory row, input slot, mode/trait selector, action and close button with stick/D-pad; A activates the selected control. Test PC and Steam Deck, UI scales, resize and long messages.
- [ ] Press/hold B with an empty cursor, held item, staged inputs and completed output. It closes once without breeding, inventory reopening or a world action. A fresh B press after release works normally.
- [ ] Holding an ordinary inventory item opens the UI without depositing or consuming it. Direct machine-input probes reject deposits. Axe/pickaxe use follows the normal removal path.
- [ ] Breeding consumes the configured x crops and x seeds from matching stacks, producing one seed. Default x is 1. Test cost 1/5/999, insufficient/surplus inputs, full inventory, wrong crop/seed IDs, and trait/level/quality/Companion stack differences. Changing cost with stored donors must require retrieval/reinsertion when counts differ.
- [ ] Plain seeds copy donor traits. A single level-1 seed trait merges into the donor: X,Y + X gives X2,Y; X,Y + Z gives X,Y,Z. Multi-trait seeds, level-2-or-higher seeds and over-cap merges reject without consuming the staged inputs. Lowering the cap does not erase traits or prevent plain-seed copying.
- [ ] Set Companion uses one seed with Companion and one eligible crop. Same choice rejects, different choice replaces, and choosing its own crop is allowed. Donor crop traits/quality do not transfer. In breeding, an assigned donor choice wins; otherwise use the seed's choice.
- [ ] Remove Trait consumes/stages one eligible trait seed, removes only the selected trait and requires no extra ingredient. Removing Companion clears its choice; removing the final trait leaves a plain seed. Coffee is accepted here; ordinary produce is not. Completed output cannot be processed twice.
- [ ] Mode changes require empty slots/cursor. Closing returns unused right-slot/cursor items through inventory or overflow; donor/completed output remains staged. Reopen, save/reload, move the station, and verify lock cleanup. Confirm normal item rendering resumes after menu closure or a drawing error.

Station destruction has accepted exceptions: contents may clear on an axe/pickaxe action before confirmed removal, and closing immediately after removal can return staged seeds before the next update. Do not turn those cases into mandatory behavior fixes without a new decision. They still must not produce an uncaught mod error.

### 2. Mutation lifecycle and timing

- [ ] Set mutation chance to 1 before readiness; an eligible free-slot crop gains one trait shared by every primary output. At cap, the full eligible pool is still selected: blocked/maxed picks produce a stored failure, not a retry. Researcher increases the chance roll only.
- [ ] Set chance to 0 before another readiness event. Its no-change marker remains fixed through waiting, repeated growth calls, config edits and save/reload. Stored successful outcomes also remain fixed. Changes to mutation settings affect the next unprepared cycle only.
- [ ] Check overnight growth before Auto Harvester and daytime Junimos, crop fairy/`growCompletely`, zero-day plants and other instant-growth effects. A mod that directly changes phases without a supported growth hook may show no icon until harvest, but the fallback must prepare the outcome before output decoration.
- [ ] The static vanilla purple star appears only over a ready plant with an actual mutation. Test zoom, trellises, colored flowers, dense rows and screen edges. It indicates mutation, not quality. Dead plants and failed mutations show no icon.
- [ ] Failed harvest/full-inventory attempts retain the result. Successful regrowing/Rooted harvests clear it; the next readiness cycle gets one new outcome. Pending mutation data stays on the plant, not harvested items or Seed Saver outputs.
- [ ] New cycles rolled at different 10-minute clock times use different inputs. Identical inputs can repeat, and different inputs can coincidentally yield the same outcome. Waiting after a stored roll never changes that mutation; later harvest time may change bonus rolls.
- [ ] Regrowing mutations are enabled by default; disabling them prevents future mutations without stripping inherited traits or changing a stored result. The standing plant never acquires its produce's new mutation automatically.

Influencing the initial roll by replaying the day with different maturation timing is accepted. Same-tile/same-interval repeat rolls are also accepted; there is no cycle counter.

### 3. Growth, soil and crop exceptions

- [ ] Compare planting on ordinary ground, greenhouse/Ginger Island ground, already-fertilized soil, and immediate harvest/replant on the same soil. Test quality fertilizer, Speed-Gro, Agriculturist and rice/taro adjacency alongside Fast Growth, Companion and Researcher.
- [ ] Recalculation removes old trait adjustments before applying new ones; repeated recalculation or Rooted cycles must not accumulate speed or delays. Initial days use `(ordinary adjusted days + half Companion base days) × Researcher multiplier × Fast Growth multiplier`, rounded up once. Regrowth uses the same trait formula on the normal regrowth interval, without inventing vanilla fertilizer effects on regrowth.
- [ ] Initial 5 days plus a 7-day Companion gives 9 days; at Fast Growth 5 it gives 7. Regrowth 4 plus that Companion gives 8 days, or 6 with Fast Growth 5. Fast Growth never creates regrowth.
- [ ] Trait seeds are refused in Garden Pots/modded planters using pot soil. Plain seeds there behave normally and do not gain crop-breeding mutations. Custom planters using actual terrain soil remain a separate compatibility boundary.
- [ ] Mixed/Mixed Flower seed packets are not breeding inputs. After vanilla resolves them to an eligible known crop, test that crop using its actual mapping; do not require a blanket ban based on mixed-seed origin. Seasonal forage, vanilla Fiber and Qi crops remain excluded. Tea/tree/grass inputs are not crop-breeding seeds.
- [ ] Run `cropbreeding_catalog` against SVE and inspect seed-to-harvest mappings, including eligible SVE Ancient Fiber. No display-name matching or general resource-category exclusion is intended.
- [ ] Sunflowers give potentially mutated flowers and original-trait bonus seeds; wheat hay remains plain. Coffee beans inherit/mutate and replant directly, remain blocked as breeding/merging donors, and can use Set Companion/Remove Trait.
- [ ] Differently traited plants may form giant crops under vanilla rules. Breaking a giant crop gives ordinary output without traits.
- [ ] Evergreen 1–4 retains normal seasons; level 5 supports manual out-of-season planting/growth and seasonal survival, including winter. Normal watering/location/trellis restrictions remain; dead plants do not revive. A new level-5 mutation does not protect the original level-4 plant. Better Junimos' season-selection limitation is accepted.

### 4. Harvest effects and storage

Run applicable cases through hand, scythe/Iridium Scythe, vanilla Junimo, Better Junimos and updated Auto Harvester paths, including offscreen locations. Custom harvesters bypassing `Crop.harvest` are not covered.

- [ ] High Yield adds proportional primary output: 4 base items at level 1 give 4 plus 80% for a fifth; level 2 gives 5 plus 60% for a sixth; level 5 gives 8. It excludes sunflower seeds, hay, Companion, materials and Seed Saver. All primary extras share the mutation.
- [ ] High Quality upgrades each primary unit by at most one tier, normal → silver → gold → iridium, after vanilla quality. Test mixed-quality stacks, colored flowers and High Yield extras. It uses inherited levels, not a new mutation, and does not upgrade byproducts.
- [ ] Companion rolls once per plant harvest for one normal-quality, trait-free crop. Check unassigned/assigned and same-ID choices. Its delay uses initial base data, never the companion's regrowth or modified days. No recursive effects or High Yield multiplication.
- [ ] Copper/Iron/Gold Bearing output IDs are 334/335/336; Maple/Resin/Tar output IDs are 724/725/726. At 28 base days, levels 1/4/5 give 25%, exactly one, and one plus 25% for another. Under five base days gives zero. Each trait rolls independently, using initial base growth even on regrowers; buffs, primary yield and regrowth interval do not affect these odds.
- [ ] Seed Saver returns at most one matching seed per successful harvest with original inherited traits/Companion, not the new mutation. Test coffee and sunflowers. Researcher level 1/5 gives default total mutation chances of 10%/30% and growth penalties of 10%/50%.
- [ ] Rooted restarts a successful annual crop at phase/day zero with original traits/color/Companion and current soil fertilizer/water. Sunflowers become flowers again. Natural regrowers, newly mutated Rooted, failed harvests and tool/giant-crop destruction cannot trigger it. Harvest bonuses occur once before restart.
- [ ] Nurse Crop advances living non-fruit trees in the eight neighboring tiles by the shared rolled stage count, capped at stage 4. Test stages 0–5, diagonals, stumps, fruit trees and trees two tiles away. Rooted may trigger Nurse Crop on each successful cycle; natural regrowers and new Nurse Crop mutations do not get the effect.
- [ ] Failed harvest attempts award no pending bonuses. Full Auto Harvester storage does not start harvesting; successful overflow uses its normal drop behavior. Vanilla Junimo hut overflow retains its accepted object-debris/metadata limitation.
- [ ] Same item ID with different traits/levels/Companion choices does not stack; normal quality/color rules still apply. Automate does not feed/collect from the breeding station, but can use trait items in other supported machines. Processing/Seed Maker/crafting outputs have no breeding traits or Companion assignment; input quality retains its ordinary meaning. Building/crafting consumption of valuable trait items is allowed.

### 5. Better Junimos, raisins and walnuts

- [ ] Put two differently traited stacks of the same seed in the hut. Each planted crop must inherit from the exact stack consumed, including Companion and timing. Compare against hand planting on prefertilized ground with SVE crops, paddy adjacency and Agriculturist. Normal one-seed consumption and Better Junimos' infinite-inventory setting remain its responsibility.
- [ ] Better Junimos may fertilize empty soil and internal phase 0 only, retaining its other rejection rules. Phase 1 and later must be skipped before consumption, including a crop that advances after job selection. Compare resulting timing with manual fertilizing and confirm trait metadata remains intact. Test Rooted returning to phase 0.
- [ ] Better Junimos still may reject Evergreen 5 seeds out of season. This is accepted, not a pending integration fix. Test optional-mod absence and log/fallback behavior for unsupported contracts.
- [ ] With raisins active, a successful vanilla 20% roll copies one last primary crop with its traits/Companion/quality/color, never seeds, hay, bars or Companion output—even when Companion has the same ID. It does not duplicate the whole multi-yield batch or rerun mutation, Rooted, Nurse Crop or other bonuses. Auto Harvester does not gain a raisin roll merely by using a Junimo collector.
- [ ] Hand/scythe harvesting an annual Rooted crop on Ginger Island preserves the skipped 5% walnut chance through the normal shared five-`IslandFarming`-walnut limit. No additional roll for non-Rooted, failed, unready, naturally regrowing, destructive or non-island actions. Direct Junimo/Auto Harvester walnut behavior stays unchanged; bonus items do not add rolls.

### 6. Lookup Anything, settings and recovery

- [ ] Lookup Anything shows all 15 inherited traits with fixed trait percentages and current base mutation chance. A planted crop describes its inherited traits, not its pending mutation. Check Companion assignment, Evergreen 4/5, overflow odds and long controller-visible text. Plain items remain ordinary.
- [ ] Seed previews show Fast Growth/Companion/Researcher and current Agriculturist without assumed fertilizer/paddy adjacency. Actual crop views use stored phase durations/countdowns. Compare regrowth summaries and next-harvest dates immediately after harvest and later. Yield/quality forecasts remain Lookup Anything's base calculations.
- [ ] Opening lookup repeatedly does not change crops or roll mutations. Missing/changed Lookup Anything contracts or failed field writes preserve ordinary lookup behavior. Test without the optional mod.
- [ ] All five GMCM settings support save/reset and bounds. Mutation settings affect only future rolls. Breeding cost defaults to 1 and consumes equal seed/crop counts; changing it refreshes menu eligibility. Chat errors default to on. Trait effect percentages cannot be edited in config or GMCM.
- [ ] Inject errors in our preparation, growth, output decoration and menu transactions. Restore the original action/result/state where possible, keep unrelated bonuses operational, do not replay harvests or duplicate delivered items, and let later actions work. Do not suppress arbitrary original-game/other-mod exceptions.
- [ ] Check SMAPI first-error logs/repeat summaries and optional local chat notices, with no per-frame spam or reporting timer. Chat defaults off. Test missing texture/patch target, malformed config and unavailable optional APIs.
- [ ] Menu transaction errors close safely, release the lock and return undelivered items; test full inventory/overflow and Lost and Found fallback. Failed patch registration must preserve previously installed unrelated patches.
- [ ] On a backup, run `cropbreeding_cleanup` with crops and placed/inventory/nested machines. Save, exit and remove the mod; verify intended metadata/machine removal and no missing-item remnants. Machine contents are intentionally destroyed. This is an uninstall check, not old-version migration support.

## Scope and remaining work

The review's gameplay decisions are settled. In-game checks above, especially Steam Deck input, rendered UI, actual event order and installed-mod interaction, remain pending. No multiplayer acceptance checklist or backward-compatibility migration work is currently required. Accepted behavior is not a promise that crashes or lag are impossible; report reproducible failures with SMAPI logs before choosing further changes.
